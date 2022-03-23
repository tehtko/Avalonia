#nullable enable
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace Avalonia.Storage
{
    public interface IStorageItem
    {
        /// <summary>
        /// Gets the name of the item including the file name extension if there is one.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the full file-system path of the item, if the item has a path.
        /// </summary>
        bool TryGetFullPath([NotNullWhen(true)] out string? path);

        /// <summary>
        /// Gets the basic properties of the current item.
        /// </summary>
        Task<StorageItemProperties> GetBasicPropertiesAsync();

        bool CanBookmark { get; }
        Task<string?> SaveBookmark();
    }
}
