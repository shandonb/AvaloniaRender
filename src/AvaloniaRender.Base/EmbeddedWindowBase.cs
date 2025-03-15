using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform;
using Avalonia.Reactive;
using SPB.Graphics;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.X11;
using SPB.Windowing;
using System;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading.Tasks;

namespace AvaloniaRender.Core
{
    /// <summary>
    /// An embedded window base for rendering a native window into an avalonia control.
    /// </summary>
    public class EmbeddedWindowBase : NativeControlHost, IDisposableWindow
    {
        /// <summary>
        /// Determines to make opengl window handles.
        /// </summary>
        public bool IsOpenGL = false;

        /// <summary>
        /// The native window instance.
        /// </summary>
        protected NativeWindow NativeWindow { get; private set; }

        public EmbeddedWindowBase()
        {
            this.GetObservable(BoundsProperty).Subscribe(
                new AnonymousObserver<Rect>(e => StateChanged(e)));
        }


        /// <summary>
        /// On window created. This should construct the render handle.
        /// </summary>
        protected virtual void OnWindowCreated() { }

        /// <summary>
        /// On window destroyed. This should pause the render handle, but not dispose the resources
        /// as the window is not fully removed.
        /// </summary>
        protected virtual void OnWindowDestroying() { }

        /// <summary>
        /// Resize event for when the bounds of the control changes.
        /// </summary>
        /// <param name="rect">The rectangle bounds of the window. </param>
        private void StateChanged(Avalonia.Rect rect)
        {
            NativeWindow?.OnResize(rect);
            OnResize(rect.Size);
        }

        /// <summary>
        /// When the control is resized. 
        /// </summary>
        /// <param name="size">The rectangle bounds of the window. </param>
        public virtual void OnResize(Size size)
        {

        }

        // Creates native controller. Attaches to native window
        protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle control)
        {
            if (NativeWindow != null)
            {
                // Skip recreation if already created
                return NativeWindow.GetExistingHandle(control);
            }

            // Create a window control that stores the window instance
            // This instance can be shared if the control gets destoryed from things like tab in/out
            NativeWindow = new NativeWindow() { IsOpenGL = IsOpenGL, };

            var platformHandle = NativeWindow.CreateNativeControlCore(control);
            OnWindowCreated();
            return platformHandle;
        }


        protected override void DestroyNativeControlCore(IPlatformHandle control)
        {
            OnWindowDestroying();
        }

        /// <summary>
        /// Fully disposes the window handle and related render data.
        /// </summary>
        public virtual void DisposeGraphics()
        {
            this.NativeWindow?.Dispose();
        }
    }
}