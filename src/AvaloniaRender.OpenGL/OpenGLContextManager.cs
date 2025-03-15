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

namespace AvaloniaRender.OpenGL
{
    public class OpenGLContextManager
    {
        private static SPBOpenGLContext GlobalContext { get; set; }

        static OpenGLContextManager()
        {

        }

        public static OpenGLContextBase GetContext()
        {
            InitializeSharedResources();
            return GlobalContext._context;
        }

        public static void InitializeSharedResources()
        {
            // Initialize the shared OpenGL context
            if (GlobalContext != null) // Already setup
                return;

            GlobalContext = SPBOpenGLContext.CreateBackgroundContext(null);
            GlobalContext?.MakeCurrent();
        }
    }
}
