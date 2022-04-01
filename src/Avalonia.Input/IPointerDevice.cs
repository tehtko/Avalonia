using System;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public interface IPointerDevice : IInputDevice
    {
        /// <inheritdoc cref="IPointer.Captured" />
        [Obsolete("Use IPointer")]
        IInputElement? Captured { get; }

        /// <inheritdoc cref="IPointer.Capture(IInputElement?)" />
        [Obsolete("Use IPointer")]
        void Capture(IInputElement? control);

        /// <inheritdoc cref="PointerEventArgs.GetPosition(IVisual?)" />
        [Obsolete("Use PointerEventArgs.GetPosition")]
        Point GetPosition(IVisual relativeTo);

        void SceneInvalidated(IInputRoot root, Rect rect);
    }
}
