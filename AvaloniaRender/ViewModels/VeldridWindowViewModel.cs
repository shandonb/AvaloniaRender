using AvaloniaRender.Core;
using AvaloniaRender.OpenGL;
using AvaloniaRender.Veldrid;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Veldrid;

namespace AvaloniaRender.ViewModels
{
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
}
