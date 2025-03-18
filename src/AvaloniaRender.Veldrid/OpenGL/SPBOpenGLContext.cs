using SPB.Graphics.OpenGL;
using SPB.Graphics;
using SPB.Platform;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace AvaloniaRender.Veldrid.OpenGL
{
    /// <summary>
    /// Opengl context for background rendering.
    /// </summary>
    public class SPBOpenGLContext
    {
        public readonly OpenGLContextBase _context;
        public readonly NativeWindowBase _window;

        private SPBOpenGLContext(OpenGLContextBase context, NativeWindowBase window)
        {
            _context = context;
            _window = window;
        }

        public void Dispose()
        {
            _context.Dispose();
            _window.Dispose();
        }

        public void MakeCurrent()
        {
            _context.MakeCurrent(_window);
        }

        public bool HasContext() => _context.IsCurrent;

        public static SPBOpenGLContext CreateBackgroundContext(OpenGLContextBase sharedContext)
        {
            OpenGLContextBase context = PlatformHelper.CreateOpenGLContext(FramebufferFormat.Default, 3, 3, OpenGLContextFlags.Compat, true, sharedContext);
            NativeWindowBase window = PlatformHelper.CreateOpenGLWindow(FramebufferFormat.Default, 0, 0, 100, 100);

            context.Initialize(window);
            context.MakeCurrent(window);

            context.MakeCurrent(null);

            return new SPBOpenGLContext(context, window);
        }
    }
}
