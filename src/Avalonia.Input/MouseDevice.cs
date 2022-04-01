using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Utilities;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Represents a mouse device.
    /// </summary>
    public class MouseDevice : IMouseDevice, IDisposable
    {
        private int _clickCount;
        private Rect _lastClickRect;
        private ulong _lastClickTime;

        private readonly Pointer _pointer;
        private bool _disposed;
        private PixelPoint? _position;
        private MouseButton _lastMouseDownButton;

        public MouseDevice(Pointer? pointer = null)
        {
            _pointer = pointer ?? new Pointer(Pointer.GetNextFreeId(), PointerType.Mouse, true);
        }

        [Obsolete("Use IPointer instead")]
        public IInputElement? Captured => _pointer.Captured;

        [Obsolete("Use events instead")]
        public PixelPoint Position
        {
            get => _position ?? new PixelPoint(-1, -1);
            protected set => _position = value;
        }

        [Obsolete("Use IPointer instead")]
        public void Capture(IInputElement? control)
        {
            _pointer.Capture(control);
        }

        /// <summary>
        /// Gets the mouse position relative to a control.
        /// </summary>
        /// <param name="relativeTo">The control.</param>
        /// <returns>The mouse position in the control's coordinates.</returns>
        public Point GetPosition(IVisual relativeTo)
        {
            relativeTo = relativeTo ?? throw new ArgumentNullException(nameof(relativeTo));

            if (relativeTo.VisualRoot == null)
            {
                throw new InvalidOperationException("Control is not attached to visual tree.");
            }

#pragma warning disable CS0618 // Type or member is obsolete
            var rootPoint = relativeTo.VisualRoot.PointToClient(Position);
#pragma warning restore CS0618 // Type or member is obsolete
            var transform = relativeTo.VisualRoot.TransformToVisual(relativeTo);
            return rootPoint * transform!.Value;
        }

        public void ProcessRawEvent(RawInputEventArgs e)
        {
            if (!e.Handled && e is RawPointerEventArgs margs)
                ProcessRawEvent(margs);
        }

        int ButtonCount(PointerPointProperties props)
        {
            var rv = 0;
            if (props.IsLeftButtonPressed)
                rv++;
            if (props.IsMiddleButtonPressed)
                rv++;
            if (props.IsRightButtonPressed)
                rv++;
            if (props.IsXButton1Pressed)
                rv++;
            if (props.IsXButton2Pressed)
                rv++;
            return rv;
        }

        private void ProcessRawEvent(RawPointerEventArgs e)
        {
            e = e ?? throw new ArgumentNullException(nameof(e));

            var mouse = (MouseDevice)e.Device;
            if(mouse._disposed)
                return;

            var oldPosition = _position;
            _position = e.Root.PointToScreen(e.Position);
            var props = CreateProperties(e);
            var keyModifiers = KeyModifiersUtils.ConvertToKey(e.InputModifiers);
            switch (e.Type)
            {
                case RawPointerEventType.LeaveWindow:
                case RawPointerEventType.NonClientLeftButtonDown:
                    LeaveWindow();
                    break;
                case RawPointerEventType.LeftButtonDown:
                case RawPointerEventType.RightButtonDown:
                case RawPointerEventType.MiddleButtonDown:
                case RawPointerEventType.XButton1Down:
                case RawPointerEventType.XButton2Down:
                    if (ButtonCount(props) > 1)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints);
                    else
                        e.Handled = MouseDown(mouse, e.Timestamp, e.Root, e.Position,
                            props, keyModifiers);
                    break;
                case RawPointerEventType.LeftButtonUp:
                case RawPointerEventType.RightButtonUp:
                case RawPointerEventType.MiddleButtonUp:
                case RawPointerEventType.XButton1Up:
                case RawPointerEventType.XButton2Up:
                    if (ButtonCount(props) != 0)
                        e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints);
                    else
                        e.Handled = MouseUp(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers);
                    break;
                case RawPointerEventType.Move:
                    e.Handled = MouseMove(mouse, e.Timestamp, e.Root, e.Position, props, keyModifiers, e.IntermediatePoints);
                    break;
                case RawPointerEventType.Wheel:
                    e.Handled = MouseWheel(mouse, e.Timestamp, e.Root, e.Position, props, ((RawMouseWheelEventArgs)e).Delta, keyModifiers);
                    break;
                case RawPointerEventType.Magnify:
                    e.Handled = GestureMagnify(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers);
                    break;
                case RawPointerEventType.Rotate:
                    e.Handled = GestureRotate(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers);
                    break;
                case RawPointerEventType.Swipe:
                    e.Handled = GestureSwipe(mouse, e.Timestamp, e.Root, e.Position, props, ((RawPointerGestureEventArgs)e).Delta, keyModifiers);
                    break;
            }
        }

        private void LeaveWindow()
        {
            _position = null;
        }

        PointerPointProperties CreateProperties(RawPointerEventArgs args)
        {

            var kind = PointerUpdateKind.Other;

            if (args.Type == RawPointerEventType.LeftButtonDown)
                kind = PointerUpdateKind.LeftButtonPressed;
            if (args.Type == RawPointerEventType.MiddleButtonDown)
                kind = PointerUpdateKind.MiddleButtonPressed;
            if (args.Type == RawPointerEventType.RightButtonDown)
                kind = PointerUpdateKind.RightButtonPressed;
            if (args.Type == RawPointerEventType.XButton1Down)
                kind = PointerUpdateKind.XButton1Pressed;
            if (args.Type == RawPointerEventType.XButton2Down)
                kind = PointerUpdateKind.XButton2Pressed;
            if (args.Type == RawPointerEventType.LeftButtonUp)
                kind = PointerUpdateKind.LeftButtonReleased;
            if (args.Type == RawPointerEventType.MiddleButtonUp)
                kind = PointerUpdateKind.MiddleButtonReleased;
            if (args.Type == RawPointerEventType.RightButtonUp)
                kind = PointerUpdateKind.RightButtonReleased;
            if (args.Type == RawPointerEventType.XButton1Up)
                kind = PointerUpdateKind.XButton1Released;
            if (args.Type == RawPointerEventType.XButton2Up)
                kind = PointerUpdateKind.XButton2Released;
            
            return new PointerPointProperties(args.InputModifiers, kind);
        }

        private bool MouseDown(IMouseDevice device, ulong timestamp, IInputElement root, Point p,
            PointerPointProperties properties,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);

            if (hit != null)
            {
                _pointer.Capture(hit);
                var source = GetSource(hit);
                if (source != null)
                {
                    var settings = AvaloniaLocator.Current.GetService<IPlatformSettings>();
                    var doubleClickTime = settings?.DoubleClickTime.TotalMilliseconds ?? 500;
                    var doubleClickSize = settings?.DoubleClickSize ?? new Size(4, 4);

                    if (!_lastClickRect.Contains(p) || timestamp - _lastClickTime > doubleClickTime)
                    {
                        _clickCount = 0;
                    }

                    ++_clickCount;
                    _lastClickTime = timestamp;
                    _lastClickRect = new Rect(p, new Size())
                        .Inflate(new Thickness(doubleClickSize.Width / 2, doubleClickSize.Height / 2));
                    _lastMouseDownButton = properties.PointerUpdateKind.GetMouseButton();
                    var e = new PointerPressedEventArgs(source, _pointer, root, p, timestamp, properties, inputModifiers, _clickCount);
                    source.RaiseEvent(e);
                    return e.Handled;
                }
            }

            return false;
        }

        private bool MouseMove(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties properties,
            KeyModifiers inputModifiers, Lazy<IReadOnlyList<RawPointerPoint>?>? intermediatePoints)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var source = _pointer.Captured ?? root.InputHitTest(p);

            if (source is object)
            {
                var e = new PointerEventArgs(InputElement.PointerMovedEvent, source, _pointer, root,
                    p, timestamp, properties, inputModifiers, intermediatePoints);

                source.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private bool MouseUp(IMouseDevice device, ulong timestamp, IInputRoot root, Point p, PointerPointProperties props,
            KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);
            var source = GetSource(hit);

            if (source is not null)
            {
                var e = new PointerReleasedEventArgs(source, _pointer, root, p, timestamp, props, inputModifiers,
                    _lastMouseDownButton);

                source?.RaiseEvent(e);
                _pointer.Capture(null);
                return e.Handled;
            }

            return false;
        }

        private bool MouseWheel(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props,
            Vector delta, KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);
            var source = GetSource(hit);

            // KeyModifiers.Shift should scroll in horizontal direction. This does not work on every platform. 
            // If Shift-Key is pressed and X is close to 0 we swap the Vector.
            if (inputModifiers == KeyModifiers.Shift && MathUtilities.IsZero(delta.X))
            {
                delta = new Vector(delta.Y, delta.X);
            }

            if (source is not null)
            {
                var e = new PointerWheelEventArgs(source, _pointer, root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureMagnify(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureMagnifyEvent, source,
                    _pointer, root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureRotate(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureRotateEvent, source,
                    _pointer, root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }
        
        private bool GestureSwipe(IMouseDevice device, ulong timestamp, IInputRoot root, Point p,
            PointerPointProperties props, Vector delta, KeyModifiers inputModifiers)
        {
            device = device ?? throw new ArgumentNullException(nameof(device));
            root = root ?? throw new ArgumentNullException(nameof(root));

            var hit = HitTest(root, p);

            if (hit != null)
            {
                var source = GetSource(hit);
                var e = new PointerDeltaEventArgs(Gestures.PointerTouchPadGestureSwipeEvent, source, 
                    _pointer, root, p, timestamp, props, inputModifiers, delta);

                source?.RaiseEvent(e);
                return e.Handled;
            }

            return false;
        }

        private IInteractive? GetSource(IVisual? hit)
        {
            if (hit is null)
                return null;

            return _pointer.Captured ??
                (hit as IInteractive) ??
                hit.GetSelfAndVisualAncestors().OfType<IInteractive>().FirstOrDefault();
        }

        private IInputElement? HitTest(IInputElement root, Point p)
        {
            root = root ?? throw new ArgumentNullException(nameof(root));

            return _pointer.Captured ?? root.InputHitTest(p);
        }

        public void Dispose()
        {
            _disposed = true;
            _pointer?.Dispose();
        }

        [Obsolete]
        public void TopLevelClosed(IInputRoot root)
        {
            // no-op
        }

        [Obsolete]
        public void SceneInvalidated(IInputRoot root, Rect rect)
        {
            // no-op
        }
    }
}
