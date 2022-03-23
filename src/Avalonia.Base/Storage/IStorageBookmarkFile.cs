#nullable enable
using System.Threading.Tasks;

namespace Avalonia.Storage
{
    public interface IStorageBookmarkFile : IStorageFile
    {
        Task Release();
        Task<bool> RequestPermissions();
    }
}
