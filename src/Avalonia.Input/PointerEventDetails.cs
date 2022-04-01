namespace Avalonia.Input
{
    public class PointerEventDetails
    {
        public PointerEventDetails(
            ulong timestamp, Point position,
            PointerPointProperties properties, KeyModifiers inputModifiers)
        {
            Timestamp = timestamp;
            Position = position;
            Properties = properties;
            InputModifiers = inputModifiers;
        }

        /// <inheritdoc cref="PointerEventArgs.Timestamp" />
        public ulong Timestamp { get; }

        /// <inheritdoc cref="PointerEventArgs.GetPosition(VisualTree.IVisual?)" />
        public Point Position { get; }

        /// <inheritdoc cref="PointerEventArgs.Properties" />
        public PointerPointProperties Properties { get; }

        /// <inheritdoc cref="PointerEventArgs.InputModifiers" />
        public KeyModifiers InputModifiers { get; }
    }
}
