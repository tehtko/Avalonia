namespace Avalonia.Input
{
    /// <summary>
    /// Defines the interface for top-level input elements.
    /// </summary>
    public interface IInputRoot : IInputElement
    {
        /// <summary>
        /// Gets or sets the access key handler.
        /// </summary>
        IAccessKeyHandler AccessKeyHandler { get; }

        /// <summary>
        /// Gets or sets the keyboard navigation handler.
        /// </summary>
        IKeyboardNavigationHandler KeyboardNavigationHandler { get; }

        /// <summary>
        /// Gets or sets the input element that the pointer is currently over.
        /// </summary>
        IInputElement? PointerOverElement { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether access keys are shown in the window.
        /// </summary>
        bool ShowAccessKeys { get; set; }

        /// <summary>
        /// Gets associated mouse device
        /// </summary>
        IMouseDevice? MouseDevice { get; }

        /// <summary>
        /// Finds and sets new pointer over element by generating <see cref="InputElement.PointerEnterEvent"/> and <see cref="InputElement.PointerLeaveEvent"/> events.
        /// Clears previous event.
        /// </summary>
        /// <param name="pointer">Pointer used in generated event arguments.</param>
        /// <param name="pointerEvent">Events details used in generated event arguments.</param>
        /// <returns>New pointer over element if found or null.</returns>
        IInputElement? SetPointerOver(IPointer pointer, PointerEventDetails pointerEvent);

        /// <summary>
        /// Clears current pointer over element.
        /// Generates <see cref="InputElement.PointerLeaveEvent"/> event.
        /// </summary>
        /// <param name="pointer">Pointer used in generated event arguments.</param>
        /// <param name="pointerEvent">Events details used in generated event arguments.</param>
        void ClearPointerOver(IPointer pointer, PointerEventDetails pointerEvent);
    }
}
