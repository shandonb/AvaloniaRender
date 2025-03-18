using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using AvaloniaRender.Core;
using SPB.Graphics.OpenGL;
using SPB.Graphics;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Veldrid.OpenGL;
using Veldrid;
using Avalonia.Rendering;
using AvaloniaRender.Veldrid.OpenGL;

namespace AvaloniaRender.Veldrid
{
    /// <summary>
    /// An embbed window for rendering veldrid graphics.
    /// </summary>
    public class EmbeddedWindowVeldrid : EmbeddedWindowBase
    {
        /// <summary>
        /// The graphics renderer.
        /// </summary>
        public VeldridRender Renderer = new VeldridRender();

        // Surface handle
        private NativeWindowBase _surface;

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            // Enable rendering if in visual
            if (Renderer != null)
                Renderer.IsRendering = true;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            // Disable rendering if detached from visual
            if (Renderer != null)
                Renderer.IsRendering = false;
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            // IVeldridWindowModel for view model render handle and disposing
            if (DataContext is IVeldridWindowModel windowModel)
            {
                windowModel.WindowHandle = this;
                Renderer = windowModel.Render;
            }
        }

        /// <summary>
        /// On window created. This should construct the render handle.
        /// </summary>
        protected override void OnWindowCreated()
        {
            if (GraphicsRuntime.GraphicsBackend == GraphicsBackend.OpenGL ||
                GraphicsRuntime.GraphicsBackend == GraphicsBackend.OpenGLES)
            {
                OnWindowCreatedOpenGL();
                return;
            }

            var handle = TopLevel.GetTopLevel(this).TryGetPlatformHandle();
            _surface = NativeWindow.CreateSurface(handle);

            var instance = Marshal.GetHINSTANCE(typeof(EmbeddedWindowVeldrid).Module);

            Renderer.Init(_surface.WindowHandle.RawHandle, instance);
            Renderer.Resize((uint)this.Bounds.Width, (uint)this.Bounds.Height);
            Renderer.IsRendering = true;

            Task.Run(() =>
            {
                Renderer.Loop();
            });
        }

        protected void OnWindowCreatedOpenGL()
        {
            base.OnWindowCreated();

            NativeWindow.IsOpenGL = true;

            var handle = TopLevel.GetTopLevel(this).TryGetPlatformHandle();
            var window = (SwappableNativeWindowBase)NativeWindow.CreateSurface(handle);
            var instance = Marshal.GetHINSTANCE(typeof(EmbeddedWindowVeldrid).Module);

            var sharedContext = OpenGLContextManager.GetContext();

            var flags = OpenGLContextFlags.Compat;
            var graphicsMode = Environment.OSVersion.Platform == PlatformID.Unix
             ? new FramebufferFormat(new ColorFormat(8, 8, 8, 0), 16, 0, ColorFormat.Zero, 0, 2, false)
             : FramebufferFormat.Default;

            var context = PlatformHelper.CreateOpenGLContext(graphicsMode, 3, 3, flags, true, sharedContext);

            context.Initialize(window);
            context.MakeCurrent(window);

            context.MakeCurrent(null);

            Task.Run(async () =>
            {
                SPBOpenGLContext bgContext = SPBOpenGLContext.CreateBackgroundContext(context);
                context.MakeCurrent(window);

                Renderer.OpenGLRenderContext = bgContext;
                Renderer.OpenGLWindowContext = context;
                Renderer.OpenGLWindow = window;

                Renderer.Init(window.WindowHandle.RawHandle, instance);
                Renderer.Resize((uint)this.Bounds.Width, (uint)this.Bounds.Height);
                Renderer.IsRendering = true;

                Renderer.Loop();
            });
        }

        /// <summary>
        /// When the control is resized. 
        /// </summary>
        /// <param name="size">The rectangle bounds of the window. </param>
        public override void OnResize(Size size)
        {
            if (Renderer != null)
                Renderer.Resize((uint)size.Width, (uint)size.Height);
        }

        /// <summary>
        /// On window destroyed. This should pause the render handle, but not dispose the resources
        /// as the window is not fully removed.
        /// </summary>
        protected override void OnWindowDestroying()
        {
            // Stop rendering if not attached    
            if (Renderer != null)
                Renderer.IsRendering = false;

            base.OnWindowDestroying();
        }

        /// <summary>
        /// Fully disposes the window handle and related render data.
        /// </summary>
        public override void DisposeGraphics()
        {
            Renderer?.Close();
            this.NativeWindow?.Dispose();
        }
    }
}
