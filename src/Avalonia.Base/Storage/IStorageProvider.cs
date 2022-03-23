#nullable enable
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avalonia.Storage
{
    public interface IStorageProvider
    {
        bool CanOpen { get; }
        Task<IReadOnlyList<IStorageFile>> OpenFilePickerAsync(FilePickerOpenOptions options);

        bool CanSave { get; }
        Task<IStorageFile?> SaveFilePickerAsync(FilePickerSaveOptions options);

        // bool CanPickDirectory { get; }
        // Task<IStorageDirectory> OpenDirectoryPickerAsync()
        Task<IStorageBookmarkFile?> OpenFileBookmarkAsync(string bookmark);

        // Task<IStorageBookmarkDirectory?> OpenDirectoryBookmarkAsync(string bookmark);
    }
}
