#nullable enable
using System;

namespace Avalonia.Storage
{
    public class StorageItemProperties
    {
        public ulong? Size { get; init; }

        public DateTimeOffset? ItemDate { get; init; }

        public DateTimeOffset? DateModified { get; init; }
    }
}
