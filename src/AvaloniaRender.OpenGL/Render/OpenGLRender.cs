using Avalonia.Controls;
using AvaloniaRender.Core;
using Silk.NET.OpenGL;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System.Diagnostics;

namespace AvaloniaRender.OpenGL
{
    public class OpenGLRender
    {
        /// <summary>
        /// Platform specific window instance for attaching the surface to.
        /// </summary>
        public SwappableNativeWindowBase NativeWindow { get; private set; }

        /// <summary>
        /// The active window context.
        /// </summary>
        public OpenGLContextBase Context { get; private set; }

        /// <summary>
        /// Determines if the render is active and running. If false, all resources will be disposed.
        /// </summary>
        public bool IsRunning = true;

        /// <summary>
        /// Determines if the render is currently drawing a frame.
        /// </summary>
        public bool IsRendering;

        /// <summary>
        /// Invokes when the render has been created and loaded.
        /// </summary>
        public Action OnLoad;

        /// <summary>
        /// The window width.
        /// </summary>
        public uint WindowWidth { get; private set; }

        /// <summary>
        /// The window height.
        /// </summary>
        public uint WindowHeight { get; private set; }


        /// <summary>
        /// true if the render has been disposed.
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// The opengl instance to run GL functions.
        /// </summary>
        private GL _Gl;

        private bool drawing = false;
        private bool load = false;

        // FPS calculations
        const int targetFps = 60;
        const double targetFrameTime = 1000.0 / targetFps; // Milliseconds per frame
        Stopwatch stopwatch = new Stopwatch();

        internal void SetWindow(SwappableNativeWindowBase window, OpenGLContextBase context)
        {
            NativeWindow = window;
            Context = context;
        }

        public void Init(GL gl)
        {
            _Gl = gl;
        }

        public void Loop()
        {
            if (!load)
            {
                OnLoad?.Invoke();
                Prepare(_Gl);
                load = true;
            }

            while (IsRunning)
            {
                // Sleep if no window visible
                if (WindowWidth == 0 || WindowHeight == 0)
                {
                    Thread.Sleep(10);
                    continue;
                }
                if (!IsRendering)
                {
                    Thread.Sleep(1);
                    continue;
                }

                try
                {
                    if (!drawing && _Gl != null)
                    {
                        drawing = true;
                        RenderFrame(_Gl, NativeWindow);
                        drawing = false;
                    }
                }
                catch (Exception ex)
                {
                    Debug.Assert(false);
                    throw new Exception(ex.ToString());
                }

                // Calculate elapsed time
                stopwatch.Stop();
                double elapsedTime = stopwatch.Elapsed.TotalMilliseconds;

                // Sleep for the remaining time to maintain FPS
                double sleepTime = targetFrameTime - elapsedTime;
                if (sleepTime > 0)
                {
                   // Task.Delay((int)sleepTime);
                }
            }
            Dispose(_Gl);
            IsDisposed = true;
        }

        public virtual void Prepare(GL Gl)
        {

        }

        public virtual void RenderFrame(GL Gl, SwappableNativeWindowBase window)
        {
            Gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            Gl.ClearColor(0.5f, 0.5f, 0.5f, 0);
            Gl.Clear((uint)(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit));
            Gl.Enable(EnableCap.DepthTest);
            Gl.Viewport(0, 0, (uint)WindowWidth, (uint)WindowHeight);

            window.SwapBuffers();
        }

        public virtual void Resize(uint width, uint height)
        {
            WindowWidth = width;
            WindowHeight = height;
        }

        public virtual void Dispose(GL Gl)
        {
            Gl?.Dispose();
        }

        public void Close()
        {
            this.IsRunning = false;
        }
    }
}
