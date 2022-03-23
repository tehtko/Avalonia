#nullable enable
using System.Collections.Generic;

namespace Avalonia.Storage
{
    public class FilePickerSaveOptions
    {
        public string? Title { get; init; }
        public string? DefaultFileName { get; init; }
        public IReadOnlyList<FilePickerFileType>? FileTypes { get; init; }

        /// <summary>
        /// Gets or sets a value indicating whether to display a warning if the user specifies the name of a file that already exists.
        /// </summary>
        public bool? ShowOverwritePrompt { get; init; }
    }
}
