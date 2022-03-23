using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Avalonia.Storage;

using Foundation;

using UIKit;

#nullable enable

namespace Avalonia.iOS.Storage
{
    internal class IOSStorageFile : IStorageFile, IStorageBookmarkFile
    {
        private readonly NSUrl _url;
        private readonly string _filePath;

        public IOSStorageFile(NSUrl url)
        {
            _url = url;

            using (var doc = new UIDocument(url))
            {
                _filePath = doc.FileUrl?.Path ?? url.FilePathUrl.Path;
                Name = doc.LocalizedName ?? Path.GetFileName(_filePath) ?? url.FilePathUrl.LastPathComponent;
            }
        }

        public bool CanOpenRead => true;

        public bool CanOpenWrite => true;

        public string Name { get; }

        public bool CanBookmark => true;

        public Task<StorageItemProperties> GetBasicPropertiesAsync()
        {
            var attributes = NSFileManager.DefaultManager.GetAttributes(_filePath, out var error);
            return Task.FromResult(new StorageItemProperties
            {
                Size = attributes.Size,
                DateModified = (DateTime)attributes.ModificationDate,
                ItemDate = (DateTime)attributes.CreationDate
            });
        }

        public Task<Stream> OpenRead()
        {
            return Task.FromResult<Stream>(new IOSSecurityScopedStream(_url, FileAccess.Read));
        }

        public Task<Stream> OpenWrite()
        {
            return Task.FromResult<Stream>(new IOSSecurityScopedStream(_url, FileAccess.Write));
        }

        public Task Release()
        {
            // no-op
            return Task.CompletedTask;
        }

        public Task<bool> RequestPermissions()
        {
            throw new NotImplementedException();
        }

        public Task<string?> SaveBookmark()
        {
            try
            {
                _url.StartAccessingSecurityScopedResource();

                var newBookmark = _url.CreateBookmarkData(0, Array.Empty<string>(), null, out var bookmarkError);
                if (bookmarkError != null)
                {
                    throw new NSErrorException(bookmarkError);
                }

                return Task.FromResult<string?>(
                    newBookmark.GetBase64EncodedString(NSDataBase64EncodingOptions.None));
            }
            finally
            {
                _url.StopAccessingSecurityScopedResource();
            }
        }

        public bool TryGetFullPath([NotNullWhen(true)] out string path)
        {
            path = _filePath;
            return true;
        }
    }
}
