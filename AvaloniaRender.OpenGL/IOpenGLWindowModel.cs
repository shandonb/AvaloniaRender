using AvaloniaRender.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaRender.OpenGL
{
    public interface IOpenGLWindowModel
    {
        /// <summary>
        /// The render instance for displaying opengl graphics.
        /// </summary>
        OpenGLRender Render { get; }

        /// <summary>
        /// The window handle for disposing
        /// </summary>
        IDisposableWindow WindowHandle { get; set; }
    }
}
