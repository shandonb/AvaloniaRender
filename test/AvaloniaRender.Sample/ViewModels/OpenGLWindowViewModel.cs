using AvaloniaRender.Core;
using AvaloniaRender.OpenGL;
using Silk.NET.OpenGL;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaRender.ViewModels
{
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
}
