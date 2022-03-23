using System;
using System.Linq;
using System.Threading.Tasks;

using Avalonia.Storage;

#nullable enable

namespace Avalonia.Controls.Platform
{
    /// <summary>
    /// Defines a platform-specific system dialog implementation.
    /// </summary>
    [Obsolete]
    internal class SystemDialogImpl : ISystemDialogImpl
    {
        /// <summary>
        /// Shows a file dialog.
        /// </summary>
        /// <param name="dialog">The details of the file dialog to show.</param>
        /// <param name="parent">The parent window.</param>
        /// <returns>A task returning the selected filenames.</returns>
        public async Task<string[]?> ShowFileDialogAsync(FileDialog dialog, Window parent)
        {
            var types = dialog.Filters.Select(f => new FilePickerFileType(f.Name!) { Extensions = f.Extensions }).ToArray();
            if (dialog is OpenFileDialog openDialog)
            {
                var filePicker = parent.StorageProvider;
                if (!filePicker.CanOpen)
                {
                    return null;
                }

                var options = new FilePickerOpenOptions
                {
                    AllowMultiple = openDialog.AllowMultiple,
                    FileTypes = types
                };

                var files = await filePicker.OpenFilePickerAsync(options);
                return files
                    .Select(file => file.TryGetFullPath(out var fullPath)
                        ? fullPath
                        : file.Name)
                    .ToArray();
            }
            else if (dialog is SaveFileDialog saveDialog)
            {
                var filePicker = parent.StorageProvider;
                if (!filePicker.CanSave)
                {
                    return null;
                }

                var options = new FilePickerSaveOptions
                {
                    DefaultFileName = saveDialog.InitialFileName,
                    FileTypes = types
                };

                var file = await filePicker.SaveFilePickerAsync(options);
                if (file is null)
                {
                    return null;
                }

                var filePath = file.TryGetFullPath(out var fullPath)
                    ? fullPath
                    : file.Name;
                return new[] { filePath };
            }
            return null;
        }

        public Task<string?> ShowFolderDialogAsync(OpenFolderDialog dialog, Window parent)
        {
            return Task.FromResult((string?)null);
        }
    }
}
