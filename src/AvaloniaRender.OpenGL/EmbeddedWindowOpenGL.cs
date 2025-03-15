using Avalonia.Logging;
using Avalonia.Rendering;
using Silk.NET.OpenGL;
using SPB.Graphics.Exceptions;
using SPB.Graphics;
using SPB.Graphics.OpenGL;
using SPB.Platform.WGL;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Formats.Asn1;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Silk.NET.Core.Native;
using System.Diagnostics;
using Avalonia.Platform;
using Avalonia;
using AvaloniaRender.Core;

namespace AvaloniaRender.OpenGL
{
    /// <summary>
    /// An embbed window for rendering OpenGL graphics.
    /// </summary>
    public class EmbeddedWindowOpenGL : EmbeddedWindowBase, IDisposableWindow
    {
        /// <summary>
        /// The opengl context attached to the window.
        /// </summary>
        public OpenGLContextBase Context { get; set; }

        /// <summary>
        /// The graphics render instance.
        /// </summary>
        public OpenGLRender Renderer = new OpenGLRender();

        /// <summary>
        /// Invokes when the render data has been loaded.
        /// </summary>
        public Action OnLoad;

        // The window base for swap buffers and context usage
        private SwappableNativeWindowBase _window;

        public EmbeddedWindowOpenGL()  { IsOpenGL = true; }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (DataContext is IOpenGLWindowModel windowModel)
            {
                windowModel.WindowHandle = this;
                Renderer = windowModel.Render;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            if (Renderer != null)
                Renderer.IsRendering = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            if (Renderer != null)
                Renderer.IsRendering = false;
        }

        /// <summary>
        /// On window created. This should construct the render handle.
        /// </summary>
        protected override void OnWindowCreated()
        {
            base.OnWindowCreated();

            var handle = TopLevel.GetTopLevel(this).TryGetPlatformHandle();
            _window = (SwappableNativeWindowBase)NativeWindow.CreateSurface(handle);

            var sharedContext = OpenGLContextManager.GetContext();

            var flags = OpenGLContextFlags.Compat;
            var graphicsMode = Environment.OSVersion.Platform == PlatformID.Unix ? new FramebufferFormat(new ColorFormat(8, 8, 8, 0), 16, 0, ColorFormat.Zero, 0, 2, false) : FramebufferFormat.Default;

            Context = PlatformHelper.CreateOpenGLContext(graphicsMode, 3, 3, flags, true, sharedContext);

            Context.Initialize(_window);
            Context.MakeCurrent(_window);

            var Gl = GL.GetApi(x => Context.GetProcAddress(x));

            unsafe
            {
                Debug.WriteLine($"OpenGL vendor: {SilkMarshal.PtrToString(
                    (nint)Gl.GetString(StringName.Vendor), NativeStringEncoding.UTF8)}");
                Debug.WriteLine($"OpenGL renderer: {SilkMarshal.PtrToString(
                    (nint)Gl.GetString(StringName.Renderer), NativeStringEncoding.UTF8)}");
            }

            Renderer.SetWindow(_window, Context);
            OnLoad?.Invoke();

            Context.MakeCurrent(null);

            Task.Run(async () =>
            {
                SPBOpenGLContext context = SPBOpenGLContext.CreateBackgroundContext(Context);

                MakeCurrent();
                Renderer.Init(context.Gl);

                Renderer.Loop();
            });
        }

        /// <summary>
        /// On window destroyed. This should pause the render handle, but not dispose the resources
        /// as the window is not fully removed.
        /// </summary>
        protected override void OnWindowDestroying()
        {
            base.OnWindowDestroying();
        }

        /// <summary>
        /// When the control is resized. 
        /// </summary>
        /// <param name="size">The rectangle bounds of the window. </param>
        public override void OnResize(Size size)
        {
            base.OnResize(size);
            Renderer?.Resize((uint)size.Width, (uint)size.Height);
        }

        /// <summary>
        /// Fully disposes the window handle and related render data.
        /// </summary>
        public override void DisposeGraphics()
        {
            Renderer?.Close();
            this.NativeWindow?.Dispose();
        }

        /// <summary>
        /// Sets the current context.
        /// </summary>
        /// <param name="unbind"> Unfinds the window. </param>
        /// <param name="shouldThrow"> Throws an exception if make current fails. </param>
        public void MakeCurrent(bool unbind = false, bool shouldThrow = true)
        {
            try
            {
                Context?.MakeCurrent(!unbind ? _window : null);
            }
            catch (ContextException e)
            {
                if (shouldThrow)
                {
                    throw;
                }
            }
        }
    }
}
