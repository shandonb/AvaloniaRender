using Avalonia;
using Avalonia.Platform;
using AvaloniaRender.Core.Native;
using SPB.Graphics;
using SPB.Platform;
using SPB.Platform.GLX;
using SPB.Platform.Metal;
using SPB.Platform.WGL;
using SPB.Platform.Win32;
using SPB.Platform.X11;
using SPB.Windowing;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using static AvaloniaRender.Core.Native.Win32NativeInterop;

namespace AvaloniaRender.Core
{
    /// <summary>
    /// Represents a native window which can attach to an Avalonia IPlatformHandle.
    /// </summary>
    public class NativeWindow
    {
        /// <summary>
        /// Desktop scale factor used for mac os.
        /// </summary>
        public double DesktopScaleFactor = 1.0;

        /// <summary>
        /// Determines to make an OpenGL window handle or not.
        /// </summary>
        public bool IsOpenGL = false;

        /// <summary>
        /// The linux window instance.
        /// </summary>
        protected GLXWindow X11Window { get; set; }

        /// <summary>
        /// The handle for windows operating systems.
        /// </summary>
        protected IntPtr WindowHandle { get; set; }

        /// <summary>
        /// The handle for linux operating systems.
        /// </summary>
        protected IntPtr X11Display { get; set; }

        /// <summary>
        /// The handle for mac operating systems.
        /// </summary>
        protected IntPtr NsView { get; set; }

        /// <summary>
        /// The handle for mac metal layer.
        /// </summary>
        protected IntPtr MetalLayer { get; set; }

        /// <summary>
        /// The avalonia platform handle.
        /// </summary>
        private IPlatformHandle Control;

        // For window events
        private WindowProc _wndProcDelegate;
        //For window class
        private string _className;

        // For resize content scale for mac os
        public delegate void UpdateBoundsCallbackDelegate(Rect rect);
        private UpdateBoundsCallbackDelegate _updateBoundsCallback;

        public void OnResize(Avalonia.Rect rect)
        {
            _updateBoundsCallback?.Invoke(rect);
        }

        public IPlatformHandle CreateNativeControlCore(IPlatformHandle control)
        {
            Control = control;

            if (OperatingSystem.IsLinux())
                return CreateLinux(control);

            if (OperatingSystem.IsWindows())
                return CreateWin32(control);

            if (OperatingSystem.IsMacOS())
                return CreateMacOS();

            throw new Exception($"Platform not supported!");
        }

        public IPlatformHandle GetExistingHandle(IPlatformHandle control)
        {
            if (OperatingSystem.IsWindows() && WindowHandle != IntPtr.Zero)
            {
                var handle = new PlatformHandle(WindowHandle, "HWND");
                SetWindowLongPtrW(control.Handle, GWLP_WNDPROC, Marshal.GetFunctionPointerForDelegate(_wndProcDelegate));
                return handle;
            }
            if (OperatingSystem.IsLinux())
                return new PlatformHandle(WindowHandle, "X11");
            if (OperatingSystem.IsMacOS())
                return new PlatformHandle(NsView, "NSView");

            throw new Exception($"Platform not supported!");
        }

        public NativeWindowBase CreateSurface(IPlatformHandle handle)
        {
            NativeWindowBase nativeWindowBase;

            if (OperatingSystem.IsWindows())
            {
                if (IsOpenGL)
                    nativeWindowBase = new WGLWindow(new NativeHandle(WindowHandle));
                else
                    nativeWindowBase = new SimpleWin32Window(new NativeHandle(WindowHandle));
            }
            else if (OperatingSystem.IsLinux())
            {
                nativeWindowBase = new SimpleX11Window(new NativeHandle(X11Display), new NativeHandle(WindowHandle));
            }
            else if (OperatingSystem.IsMacOS())
            {
                nativeWindowBase = new SimpleMetalWindow(new NativeHandle(NsView), new NativeHandle(MetalLayer));
            }
            else
            {
                throw new PlatformNotSupportedException();
            }

            return nativeWindowBase;
        }

        public void Dispose()
        {
            WindowHandle = IntPtr.Zero;
            X11Display = IntPtr.Zero;
            NsView = IntPtr.Zero;
            MetalLayer = IntPtr.Zero;

            if (OperatingSystem.IsLinux())
            {
                DestroyLinux();
            }
            else if (OperatingSystem.IsWindows())
            {
                DestroyWin32(Control);
            }
            else if (OperatingSystem.IsMacOS())
            {
                DestroyMacOS();
            }
        }

        [SupportedOSPlatform("linux")]
        private IPlatformHandle CreateLinux(IPlatformHandle control)
        {
            if (IsOpenGL)
            {
                X11Window = PlatformHelper.CreateOpenGLWindow(new FramebufferFormat(new ColorFormat(8, 8, 8, 0), 16, 0, ColorFormat.Zero, 0, 2, false), 0, 0, 100, 100) as GLXWindow;
            }
            else
            {
                X11Window = new GLXWindow(new NativeHandle(X11.DefaultDisplay), new NativeHandle(control.Handle));
                X11Window.Hide();
            }

            WindowHandle = X11Window.WindowHandle.RawHandle;
            X11Display = X11Window.DisplayHandle.RawHandle;

            return new PlatformHandle(WindowHandle, "X11");
        }

        [SupportedOSPlatform("windows")]
        IPlatformHandle CreateWin32(IPlatformHandle control)
        {
            _className = "NativeWindow-" + Guid.NewGuid();

            _wndProcDelegate = delegate (IntPtr hWnd, WindowsMessages msg, IntPtr wParam, IntPtr lParam)
            {
                switch (msg)
                {
                    case WindowsMessages.NcHitTest:
                             return -1;

                }

                return DefWindowProc(hWnd, msg, wParam, lParam);
            };

            WndClassEx wndClassEx = new()
            {
                cbSize = Marshal.SizeOf<WndClassEx>(),
                hInstance = GetModuleHandle(null),
                lpfnWndProc = Marshal.GetFunctionPointerForDelegate(_wndProcDelegate),
                style = ClassStyles.CsOwndc,
                lpszClassName = Marshal.StringToHGlobalUni(_className),
                hCursor = CreateArrowCursor()
            };

            RegisterClassEx(ref wndClassEx);

            WindowHandle = CreateWindowEx(0, _className, "NativeWindow", WindowStyles.WsChild, 0, 0, 640, 480, control.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            SetWindowLongPtrW(control.Handle, GWLP_WNDPROC, wndClassEx.lpfnWndProc);

            Marshal.FreeHGlobal(wndClassEx.lpszClassName);

            return new PlatformHandle(WindowHandle, "HWND");
        }

        [SupportedOSPlatform("macos")]
        IPlatformHandle CreateMacOS()
        {
            // Create a new CAMetalLayer.
            ObjectiveC.Object layerObject = new("CAMetalLayer");
            ObjectiveC.Object metalLayer = layerObject.GetFromMessage("alloc");
            metalLayer.SendMessage("init");

            // Create a child NSView to render into.
            ObjectiveC.Object nsViewObject = new("NSView");
            ObjectiveC.Object child = nsViewObject.GetFromMessage("alloc");
            child.SendMessage("init", new ObjectiveC.NSRect(0, 0, 0, 0));

            // Make its renderer our metal layer.
            child.SendMessage("setWantsLayer:", 1);
            child.SendMessage("setLayer:", metalLayer);
            metalLayer.SendMessage("setContentsScale:", DesktopScaleFactor);

            // Ensure the scale factor is up to date.
            _updateBoundsCallback = rect =>
            {
                metalLayer.SendMessage("setContentsScale:", DesktopScaleFactor);
            };

            IntPtr nsView = child.ObjPtr;
            MetalLayer = metalLayer.ObjPtr;
            NsView = nsView;

            return new PlatformHandle(nsView, "NSView");
        }

        [SupportedOSPlatform("Linux")]
        void DestroyLinux()
        {
            X11Window?.Dispose();
        }

        [SupportedOSPlatform("windows")]
        void DestroyWin32(IPlatformHandle handle)
        {
            DestroyWindow(handle.Handle);
            UnregisterClass(_className, GetModuleHandle(null));
        }

        [SupportedOSPlatform("macos")]
#pragma warning disable CA1822 // Mark member as static
        void DestroyMacOS()
        {
            // TODO
        }
#pragma warning restore CA1822
    }
}
