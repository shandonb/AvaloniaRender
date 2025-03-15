using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AvaloniaRender.Core
{
    /// <summary>
    /// Represents a disposable window. This should be attached to an embedded window base.
    /// </summary>
    public interface IDisposableWindow
    {
        /// <summary>
        /// Disposes all resources related to both graphics and the native window handle.
        /// </summary>
        void DisposeGraphics();
    }
}
