using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using Avalonia.Storage;

using UIKit;
using MobileCoreServices;
using Foundation;

#nullable enable

namespace Avalonia.iOS.Storage
{
    internal class IOSStorageProvider : IStorageProvider
    {
        private readonly UIViewController _uiViewController;
        public IOSStorageProvider(UIViewController uiViewController)
        {
            _uiViewController = uiViewController;
        }

        public bool CanOpen => true;

        public bool CanSave => true;

        public async Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options)
        {
            var allowedUtis = options.FileTypes?.SelectMany(f => f.AppleUniformTypeIdentifiers ?? Array.Empty<string>())
                .ToArray() ?? new string[]
            {
                UTType.Content,
                UTType.Item,
                "public.data"
            };

            var tcs = new TaskCompletionSource<NSUrl[]>();

            // Use Open instead of Import so that we can attempt to use the original file.
            // If the file is from an external provider, then it will be downloaded.
            using var documentPicker = new UIDocumentPickerViewController(allowedUtis, UIDocumentPickerMode.Open);

#if NET6_0_OR_GREATER
            if (OperatingSystem.IsIOSVersionAtLeast(11, 0))
            {
                documentPicker.AllowsMultipleSelection = options.AllowMultiple;
            }
#endif

            var urls = await ShowPicker(documentPicker);
            return urls.Select(u => new IOSStorageFile(u)).ToArray();
        }

        public Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark)
        {
            var url = NSUrl.FromBookmarkData(new NSData(bookmark, NSDataBase64DecodingOptions.None),
                NSUrlBookmarkResolutionOptions.WithoutUI, null, out var isStale, out var error);
            if (error != null)
            {
                throw new NSErrorException(error);
            }

            return Task.FromResult<IStorageBookmarkFile?>(new IOSStorageFile(url));
        }

        public async Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options)
        {
            string? fileName;
            NSUrl? folderUrl;

            using var alert = UIAlertController.Create(options.Title, null, UIAlertControllerStyle.Alert);
            {
                var alertTcs = new TaskCompletionSource<string?>();
                alert.AddTextField(f => f.Text = options.DefaultFileName);
                alert.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, (action) => alertTcs.TrySetResult(alert.TextFields[0].Text)));
                alert.AddAction(UIAlertAction.Create("Cancel", UIAlertActionStyle.Cancel, (action) => alertTcs.TrySetResult(null)));
                _uiViewController.PresentViewController(alert, true, null);

                fileName = await alertTcs.Task;
            }

            if (fileName is null)
            {
                return null;
            }


            using (var documentPicker = new UIDocumentPickerViewController(new string[] { UTType.Folder }, UIDocumentPickerMode.Open))
            {
                var urls = await ShowPicker(documentPicker);
                folderUrl = urls.FirstOrDefault();
            }

            if (folderUrl is null)
            {
                return null;
            }

            folderUrl.StartAccessingSecurityScopedResource();
            try
            {
                var path = Path.Combine(folderUrl.RelativePath, fileName);
                if (!File.Exists(path))
                {
                    File.Create(path);
                }
                return new IOSStorageFile(new NSUrl(fileName, folderUrl));
            }
            finally
            {
                folderUrl.StopAccessingSecurityScopedResource();
            }
        }

        private Task<NSUrl[]> ShowPicker(UIDocumentPickerViewController documentPicker)
        {
            var tcs = new TaskCompletionSource<NSUrl[]>();
            documentPicker.Delegate = new PickerDelegate
            {
                PickHandler = urls => tcs.TrySetResult(urls)
            };

            if (documentPicker.PresentationController != null)
            {
                documentPicker.PresentationController.Delegate =
                    new UIPresentationControllerDelegate(() => tcs.TrySetResult(Array.Empty<NSUrl>()));
            }

            _uiViewController.PresentViewController(documentPicker, true, null);
            return tcs.Task;
        }

        class PickerDelegate : UIDocumentPickerDelegate
        {
            public Action<NSUrl[]>? PickHandler { get; set; }

            public override void WasCancelled(UIDocumentPickerViewController controller)
                => PickHandler?.Invoke(Array.Empty<NSUrl>());

            public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl[] urls)
                => PickHandler?.Invoke(urls);

            public override void DidPickDocument(UIDocumentPickerViewController controller, NSUrl url)
                => PickHandler?.Invoke(new NSUrl[] { url });
        }

        class AlertDelegate : UIAlertViewDelegate
        {
            public Action<string?>? Input { get; set; }
            public override void Canceled(UIAlertView alertView) => Input?.Invoke(null);
            public override void Clicked(UIAlertView alertView, nint button) => Input?.Invoke(alertView.GetTextField(0)?.Text);
        }


        internal class UIPresentationControllerDelegate : UIAdaptivePresentationControllerDelegate
        {
            private Action? dismissHandler;

            internal UIPresentationControllerDelegate(Action dismissHandler)
                => this.dismissHandler = dismissHandler;

            public override void DidDismiss(UIPresentationController presentationController)
            {
                dismissHandler?.Invoke();
                dismissHandler = null;
            }

            protected override void Dispose(bool disposing)
            {
                dismissHandler?.Invoke();
                base.Dispose(disposing);
            }
        }
    }
}
