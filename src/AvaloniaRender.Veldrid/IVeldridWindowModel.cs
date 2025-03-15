using AvaloniaRender.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaRender.Veldrid
{
    public interface IVeldridWindowModel
    {
        /// <summary>
        /// The render instance for displaying veldrid graphics.
        /// </summary>
        VeldridRender Render { get; }

        /// <summary>
        /// The window handle of the embedded window for disposing (set on data context assign automatically)
        /// </summary>
        IDisposableWindow WindowHandle { get; set; }
    }
}
