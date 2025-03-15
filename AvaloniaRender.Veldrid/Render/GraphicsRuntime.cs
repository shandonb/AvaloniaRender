using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace AvaloniaRender.Veldrid
{
    public class GraphicsRuntime
    {
        public static GraphicsBackend GraphicsBackend = GetPreferredBackend();

        public static GraphicsBackend GetPreferredBackend()
        {
            // OpenGL is last resort and the most complicated one to support
            // Metal for Mac OS
            // Direct3D11 for Windows
            // Vulkan for linux, otherwise OpenGL
            if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Metal))
                return GraphicsBackend.Metal;
            else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Direct3D11))
                return GraphicsBackend.Direct3D11;
            else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.Vulkan))
                return GraphicsBackend.Vulkan;
            else if (GraphicsDevice.IsBackendSupported(GraphicsBackend.OpenGL))
                return GraphicsBackend.OpenGL;

            return GraphicsBackend.OpenGLES;
        }
    }
}
