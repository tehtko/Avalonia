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
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);

            Canvas canvas;
            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });

            SetHit(renderer, canvas);
            root.SetPointerOver(pointer.Object, pointerEvent);

            Assert.True(canvas.IsPointerOver);

            impl.Object.Closed!();

            Assert.False(canvas.IsPointerOver);
        }

        [Fact]
        public void MouseMove_Should_Update_IsPointerOver()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);

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

            SetHit(renderer, decorator);
            root.SetPointerOver(pointer.Object, pointerEvent);

            Assert.True(decorator.IsPointerOver);
            Assert.True(border.IsPointerOver);
            Assert.False(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            SetHit(renderer, canvas);
            root.SetPointerOver(pointer.Object, pointerEvent);

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);
        }

        [Fact]
        public void IsPointerOver_Should_Be_Updated_When_Child_Sets_Handled_True()
        {
            var renderer = new Mock<IRenderer>();
            var pointer = new Mock<IPointer>();
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);

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

            SetHit(renderer, canvas);
            root.SetPointerOver(pointer.Object, pointerEvent);

            Assert.False(decorator.IsPointerOver);
            Assert.False(border.IsPointerOver);
            Assert.True(canvas.IsPointerOver);
            Assert.True(root.IsPointerOver);

            // Ensure that e.Handled is reset between controls.
            decorator.PointerEnter += (s, e) => e.Handled = true;

            SetHit(renderer, decorator);
            root.SetPointerOver(pointer.Object, pointerEvent);

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
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);
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

            SetHit(renderer, canvas);
            root.SetPointerOver(pointer.Object, pointerEvent);

            AddEnterLeaveHandlers(HandleEvent, root, canvas, border, decorator);

            SetHit(renderer, decorator);
            root.SetPointerOver(pointer.Object, pointerEvent);

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
            var pointerEvent = new PointerEventDetails(default, expectedPosition,
                PointerPointProperties.None, KeyModifiers.None);
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

            SetHit(renderer, canvas);
            var pointerOver = root.SetPointerOver(pointer.Object, pointerEvent);
            Assert.Equal(canvas, pointerOver);

            root.ClearPointerOver(pointer.Object, pointerEvent);

            Assert.Equal(
                new[]
                {
                    ((object?)canvas, "PointerEnter", expectedPosition),
                    (root, "PointerEnter", expectedPosition),
                    (canvas, "PointerLeave", expectedPosition),
                    (root, "PointerLeave", expectedPosition),
                },
                result);
        }

        [Fact]
        public void Render_Invalidation_Should_Call_SceneInvalidated_On_Device()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var pointer = new Mock<IPointer>();
            var pointerDevice = new Mock<IPointerDevice>();
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);
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

            // Let input know about latest device
            SetHit(renderer, canvas);
            pointerDevice.Setup(d => d.ProcessRawEvent(rawArgs))
                .Callback(() => root.SetPointerOver(pointer.Object, pointerEvent));
            impl.Object.Input!(rawArgs);

            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            pointerDevice.Verify(d => d.SceneInvalidated(root, invalidateRect), Times.Once);

            // Raise another raw event, but this time clean pointerover.
            pointerDevice.Setup(d => d.ProcessRawEvent(rawArgs))
                .Callback(() => root.ClearPointerOver(pointer.Object, pointerEvent));
            impl.Object.Input!(rawArgs);

            // SceneInvalidated should not be called second time
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            pointerDevice.Verify(d => d.SceneInvalidated(root, invalidateRect), Times.Once);
        }

        [Fact]
        public void Render_Invalidation_Should_Call_SceneInvalidated_Only_On_Latest_Device()
        {
            using var app = UnitTestApplication.Start(new TestServices(inputManager: new InputManager()));

            var renderer = new Mock<IRenderer>();
            var impl = CreateTopLevelImplMock(renderer.Object);

            var pointer = new Mock<IPointer>();
            var pointerDevice1 = new Mock<IPointerDevice>();
            var pointerDevice2 = new Mock<IPointerDevice>();
            var pointerEvent = new PointerEventDetails(default, default, PointerPointProperties.None, KeyModifiers.None);
            var invalidateRect = new Rect(0, 0, 15, 15);

            Canvas canvas;
            var root = CreateInputRoot(impl.Object, new Panel
            {
                Children =
                {
                    (canvas = new Canvas())
                }
            });
            var rawArgs = new RawInputEventArgs(pointerDevice1.Object, 0, root);

            // Let input root to know about first device.
            SetHit(renderer, canvas);
            pointerDevice1.Setup(d => d.ProcessRawEvent(It.IsAny<RawInputEventArgs>()))
                .Callback(() => root.SetPointerOver(pointer.Object, pointerEvent));
            impl.Object.Input!(new RawInputEventArgs(pointerDevice1.Object, 0, root));

            // Let input root to know about second device.
            pointerDevice2.Setup(d => d.ProcessRawEvent(It.IsAny<RawInputEventArgs>()))
                .Callback(() => root.SetPointerOver(pointer.Object, pointerEvent));
            impl.Object.Input!(new RawInputEventArgs(pointerDevice2.Object, 0, root));

            // SceneInvalidated should be called only on latest pointer device.
            renderer.Raise(r => r.SceneInvalidated += null, new SceneInvalidatedEventArgs((IRenderRoot)root, invalidateRect));
            pointerDevice1.Verify(d => d.SceneInvalidated(root, invalidateRect), Times.Never);
            pointerDevice2.Verify(d => d.SceneInvalidated(root, invalidateRect), Times.Once);
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

        private static void SetHit(Mock<IRenderer> renderer, IControl hit)
        {
            renderer.Setup(x => x.HitTest(It.IsAny<Point>(), It.IsAny<IVisual>(), It.IsAny<Func<IVisual, bool>>()))
                .Returns(new[] { hit });

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
    }
}
