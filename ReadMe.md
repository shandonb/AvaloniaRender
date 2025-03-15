# AvaloniaRender

A simple library for loading both Silk.NET.OpenGL and Veldrid into Avalonia.
Other renders like webgpu can also work easily by creating surfaces from the window handle.

Currently I use embedded windows to display.
While there are methods like gpu interop, Veldrid was tricky to implement due to how swap chains need to be done.

## Usage Veldrid:

Make a view model that uses IVeldridWindowModel. Set Render with your own VeldridRender instance.
Override the render with your own code. Use Prepare to initialize resources, Dispose to dispose any.

Make sure to run Dispose() in the view model when you need to dispose both the native window and graphics. 
This is done manually.

```cs

    internal class VeldridWindowViewModel : IVeldridWindowModel
    {
        /// <summary>
        /// The render instance.
        /// </summary>
        public VeldridRender Render { get; set; } = new RenderTest();

        /// <summary>
        /// The window handle of the embedded window for disposing (set on data context assign automatically)
        /// </summary>
        public IDisposableWindow WindowHandle { get; set; }

        /// <summary>
        /// Disposes the window handle
        /// </summary>
        public void Dispose()
        {
            // Note this also disposes the render data used
            WindowHandle?.DisposeGraphics();
        }

        public class RenderTest : VeldridRender
        {
            public RgbaFloat ClearColor = RgbaFloat.CornflowerBlue;

            public override void Prepare(GraphicsDevice gd)
            {
                base.Prepare(gd);
            }

            public override void RenderFrame(GraphicsDevice gd, CommandList cmdList, Swapchain swapchain)
            {
                if (WindowWidth != swapchain.Framebuffer.Width || WindowHeight != swapchain.Framebuffer.Height)
                    swapchain.Resize(WindowWidth, WindowHeight);

                cmdList.Begin();
                cmdList.SetFramebuffer(swapchain.Framebuffer);
                cmdList.ClearColorTarget(0, ClearColor);
                cmdList.ClearDepthStencil(1f);

                cmdList.End();

                gd.SubmitCommands(cmdList);
                gd.SwapBuffers(swapchain);
            }

            public override void Dispose(GraphicsDevice gd)
            {
                base.Dispose(gd);
            }
        }
    }

```

Lastly set this into an EmbeddedWindowVeldrid.
You can put EmbeddedWindowVeldrid in your xaml (namespace AvaloniaRender.Veldrid) and bind from that or do it by code.

```cs

    Content = new EmbeddedWindowVeldrid()
    {
        DataContext = new VeldridWindowViewModel()
    }z
```

## Usage OpenGL:

Make a view model that uses IOpenGLWindowModel. Set Render with your own OpenGLRender instance.
Override the render with your own code. Use Prepare to initialize resources, Dispose to dispose any.
Ensure window.SwapBuffers() is ran at the end.

Make sure to run Dispose() in the view model when you need to dispose both the native window and graphics. 
This is done manually.

```cs

    public class OpenGLWindowViewModel : IOpenGLWindowModel
    {
        /// <summary>
        /// The render instance.
        /// </summary>
        public OpenGLRender Render { get; set; } = new RenderTest();

        /// <summary>
        /// The window handle of the embedded window for disposing (set on data context assign automatically)
        /// </summary>
        public IDisposableWindow WindowHandle { get; set; }

   
        /// <summary>
        /// Disposes the window handle
        /// </summary>
        public void Dispose()
        {
            WindowHandle?.DisposeGraphics();
        }

        public class RenderTest : OpenGLRender
        {
            public float ClearColorR = 1f;
            public float ClearColorG = 0.5f;
            public float ClearColorB = 0.5f;

            public override void Prepare(GL Gl)
            {
                base.Prepare(Gl);
            }

            public override void RenderFrame(GL Gl, SwappableNativeWindowBase window)
            {
                Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
                Gl.ClearColor(ClearColorR, ClearColorG, ClearColorB, 0);
                Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
                Gl.Enable(EnableCap.DepthTest);
                Gl.Viewport(0, 0, (uint)WindowWidth, (uint)WindowHeight);

                window.SwapBuffers();
            }

            public override void Dispose(GL Gl)
            {
                base.Dispose(Gl);
            }
        }
    }

```

Lastly set this into an EmbeddedWindowOpenGL.
You can put EmbeddedWindowOpenGL in your xaml (namespace AvaloniaRender.OpenGL) and bind from that or do it by code.

```cs

Content = new EmbeddedWindowOpenGL()
{
    DataContext = new OpenGLWindowViewModel()
}

```

### Credits:

Ryujinx for embedded code help and logic within NativeWindow. 