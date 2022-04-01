#nullable enable
using System;
using System.Collections.Generic;

using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Input.Raw;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;

using Moq;

using Xunit;

namespace Avalonia.Input.UnitTests
{
    public class PointerOverTests
    {
        // https://github.com/AvaloniaUI/Avalonia/issues/2821
        [Fact]
        public void Close_Should_Remove_PointerOver()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var impl = CreateTopLevelImplMock(renderer.Object);

            Canvas canvas;
            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            SetHit(renderer, canvas);
            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas));

            Assert.True(canvas.IsPointerOver);

            impl.Object.Closed!();

            Assert.False(canvas.IsPointerOver);
        }

        [Fact]
        public void MouseMove_Should_Update_IsPointerOver()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(renderer.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            decorator.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, decorator));

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas));

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }

        [Fact]
        public void TouchMove_Should_Not_Set_IsPointerOver()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            pointer.SetupGet(p => p.Type).Returns(PointerType.Touch);

            Canvas canvas;

            var root = CreateInputRoot(renderer.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas));

            Assert.False(canvas.IsPointerOver);
            Assert.False(root.IsPointerOver);
        }

        [Fact]
        public void IsPointerOver_Should_Be_Updated_When_Child_Sets_Handled_True()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(renderer.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas));

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            // Ensure that e.Handled is reset between controls.
            root.PointerMoved += (s, e) => e.Handled = true;
            decorator.PointerEnter += (s, e) => e.Handled = true;

            decorator.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, decorator));

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }

        [Fact]
        public void PointerEnter_Leave_Should_Be_Raised_In_Correct_Order()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var result = new List<(object?, string)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name));
            }

            Canvas canvas;
            Border border;
            Decorator decorator;

            var root = CreateInputRoot(renderer.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas()),
                    (border = new Border
                    {
                        Child = decorator = new Decorator(),
                    })
                }
            });

            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas));

            AddEnterLeaveHandlers(HandleEvent, root, canvas, border, decorator);

            decorator.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, decorator));

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, "PointerLeave"),
                    (decorator, "PointerEnter"),
                    (border, "PointerEnter"),
                },
                result);
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/7896
        [Fact]
        public void PointerEnter_Leave_Should_Set_Correct_Position()
        {
            var expectedPosition = new Point(15, 15);
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var result = new List<(object?, string, Point)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
            }

            Canvas canvas;

            var root = CreateInputRoot(renderer.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            AddEnterLeaveHandlers(HandleEvent, root, canvas);

            canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas, expectedPosition));

            // If source is not set, TopLevel will try to get element by position, so fake hit test too.
            SetHit(renderer, null);
            root.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, null, expectedPosition));

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, "PointerEnter", expectedPosition),
                    (root, "PointerEnter", expectedPosition),
                    (canvas, "PointerLeave", expectedPosition)
                },
                result);
        }

        [Fact]
        public void Render_Invalidation_Should_Affect_PointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var pointer = new Mock<IPointer>();
            var pointerDevice = new Mock<IPointerDevice>();
            var invalidateRect = new Rect(0, 0, 15, 15);

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            var rawArgs = new RawInputEventArgs(pointerDevice.Object, 0, root);

            // Let input know about latest device.
            pointerDevice.Setup(d => d.ProcessRawEvent(rawArgs))
                .Callback(() => canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas)));
            impl.Object.Input!(rawArgs);
            Assert.True(canvas.IsPointerOver);

            SetHit(renderer, canvas);
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            Assert.True(canvas.IsPointerOver);

            // Raise SceneInvalidated again, but now hide element from the hittest.
            SetHit(renderer, null);
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            Assert.False(canvas.IsPointerOver);
        }

        // https://github.com/AvaloniaUI/Avalonia/issues/7748
        [Fact]
        public void LeaveWindow_Should_Reset_PointerOver()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var pointer = new Mock<IPointer>();
            var pointerDevice = new Mock<IPointerDevice>();
            var lastClientPosition = new Point(1, 5);
            var invalidateRect = new Rect(0, 0, 15, 15);
            var result = new List<(object?, string, Point)>();

            void HandleEvent(object? sender, PointerEventArgs e)
            {
                result.Add((sender, e.RoutedEvent!.Name, e.GetPosition(null)));
            }

            Canvas canvas;

            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            AddEnterLeaveHandlers(HandleEvent, root, canvas);

            // Init pointer over.
            var firstInputArgs = new RawInputEventArgs(pointerDevice.Object, 0, root);
            pointerDevice.Setup(d => d.ProcessRawEvent(firstInputArgs))
                .Callback(() => canvas.RaiseEvent(CreatePointerMovedArgs(pointer.Object, root, canvas, lastClientPosition)));
            impl.Object.Input!(firstInputArgs);
            Assert.True(canvas.IsPointerOver);

            // Send LeaveWindow.
            impl.Object.Input!(new RawPointerEventArgs(pointerDevice.Object, 0, root, RawPointerEventType.LeaveWindow, new Point(), default));
            Assert.False(canvas.IsPointerOver);

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, "PointerEnter", lastClientPosition),
                    (root, "PointerEnter", lastClientPosition),
                    (canvas, "PointerLeave", lastClientPosition),
                    (root, "PointerLeave", lastClientPosition),
                },
                result);
        }

        private static void AddEnterLeaveHandlers(
            EventHandler<PointerEventArgs> handler,
            params IInputElement[] controls)
        {
            foreach (var c in controls)
            {
                c.PointerEnter += handler;
                c.PointerLeave += handler;
            }
        }

        private static void SetHit(Mock<IRenderer> renderer, IControl? hit)
        {
            renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(hit is null ? Array.Empty<IControl>() : new[] { hit });

            renderer.Setup(x => x.HitTestFirst(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(hit);
        }

        private static Mock<IWindowImpl> CreateTopLevelImplMock(IRenderer renderer)
        {
            var impl = new Mock<IWindowImpl>();
            impl.DefaultValue = DefaultValue.Mock;
            impl.SetupAllProperties();
            impl.SetupGet(r => r.RenderScaling).Returns(1);
            impl.Setup(r => r.CreateRenderer(It.IsAny<IRenderRoot>())).Returns(renderer);
            impl.Setup(r => r.PointToScreen(It.IsAny<Point>())).Returns<Point>(p => new PixelPoint((int)p.X, (int)p.Y));
            impl.Setup(r => r.PointToClient(It.IsAny<PixelPoint>())).Returns<PixelPoint>(p => new Point(p.X, p.Y));
            return impl;
        }

        private static IInputRoot CreateInputRoot(IWindowImpl impl, IControl child)
        {
            var root = new Window(impl)
            {
                Width = 100,
                Height = 100,
                Content = child,
                Template = new FuncControlTemplate<Window>((w, _) => new ContentPresenter
                {
                    Content = w.Content
                })
            };
            root.Show();
            return root;
        }

        private static IInputRoot CreateInputRoot(IRenderer renderer, IControl child)
        {
            return CreateInputRoot(CreateTopLevelImplMock(renderer).Object, child);
        }

        private static PointerEventArgs CreatePointerMovedArgs(
            IPointer pointer, IInputRoot root, IInputElement? source,
            Point? positition = null)
        {
            return new PointerEventArgs(InputElement.PointerMovedEvent, source, pointer, root,
                positition ?? default, default, PointerPointProperties.None, KeyModifiers.None);
        }
    }
}
