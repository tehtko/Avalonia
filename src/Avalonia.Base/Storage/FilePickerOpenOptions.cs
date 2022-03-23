#nullable enable
using System.Collections.Generic;

namespace Avalonia.Storage
{
    public class FilePickerOpenOptions
    {
        public string? Title { get; init; }
        public bool AllowMultiple { get; init; }
        public IReadOnlyList<FilePickerFileType>? FileTypes { get; init; }
    }
}
