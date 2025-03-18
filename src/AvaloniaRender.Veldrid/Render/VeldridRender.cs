using AvaloniaRender.Veldrid.OpenGL;
using SPB.Graphics.OpenGL;
using SPB.Windowing;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Veldrid;
using Veldrid.OpenGL;

namespace AvaloniaRender.Veldrid;

public class VeldridRender
{
    /// <summary>
    /// Determines if the render is active and running. If false, all resources will be disposed.
    /// </summary>
    public bool IsRunning = true;

    /// <summary>
    /// Invokes when the render has been created and loaded.
    /// </summary>
    public Action OnLoad;

    /// <summary>
    /// The window width.
    /// </summary>
    public uint WindowWidth { get; private set; }

    /// <summary>
    /// The window height.
    /// </summary>
    public uint WindowHeight { get; private set; }

    /// <summary>
    /// true if the render has been disposed.
    /// </summary>
    public bool IsDisposed { get; private set; }

    /// <summary>
    /// Determines if the render is currently drawing a frame.
    /// </summary>
    public bool IsRendering = true;

    /// <summary>
    /// The frame rate to run the render thread.
    /// </summary>
    public double TargetFrameRate = 60;

    // for opengl
    internal SwappableNativeWindowBase OpenGLWindow;
    /// <summary>
    /// OpenGL context from window creation.
    /// </summary>
    internal OpenGLContextBase OpenGLWindowContext;
    /// <summary>
    /// OpenGL context from render thread.
    /// </summary>
    internal SPBOpenGLContext OpenGLRenderContext;

    // Swap chain
    private Swapchain _swapchain = null;
    // Commands
    private CommandList _commandList = null;

    // Keep this static to keep contexts
    private GraphicsDevice _graphicsDevice = null;

    private bool drawing = false;
    private bool load = false;

    // FPS calculations
    double targetFrameTime => 1000.0 / TargetFrameRate; // Milliseconds per frame
    Stopwatch stopwatch = new Stopwatch();

    public void CreateDevice()
    {
        if (_graphicsDevice != null)
            return;

        GraphicsDeviceOptions options = new GraphicsDeviceOptions
        {
            ResourceBindingModel = ResourceBindingModel.Improved,
            SwapchainDepthFormat = PixelFormat.D32_Float_S8_UInt,
        };
#if DEBUG
        options.Debug = true;
#endif

        switch (GraphicsRuntime.GraphicsBackend)
        {
            case GraphicsBackend.Direct3D11:
                _graphicsDevice = GraphicsDevice.CreateD3D11(options);
                break;
            case GraphicsBackend.Vulkan:
                _graphicsDevice = GraphicsDevice.CreateVulkan(options);
                break;
            case GraphicsBackend.Metal:
                _graphicsDevice = GraphicsDevice.CreateMetal(options);
                break;
            case GraphicsBackend.OpenGL:
            case GraphicsBackend.OpenGLES:
                void SetVSync(bool vsync) { }

                var context = this.OpenGLWindowContext;
                var window = this.OpenGLWindow;
                if (window == null || context == null)
                    throw new Exception($"No OpenGL window or context setup!");

                var glPlatformInfo = new OpenGLPlatformInfo(
                   context.ContextHandle, // OpenGL context handle
                   OpenGLRenderContext._context.GetProcAddress,  // Function loader
                   ctx => context.MakeCurrent(window),  // Make current
                   () => context.ContextHandle, // Get current context
                   () => context.MakeCurrent(null), // Clear current context
                   ctx => { context.Dispose(); }, // Delete context
                   () => window.SwapBuffers(), // Swap buffers
                   vsync => SetVSync(vsync) // Set vertical sync
                );
                 _graphicsDevice = GraphicsDevice.CreateOpenGL(options, glPlatformInfo, 4, 4);
                break;
        }

    }

    public void Init(IntPtr? controlHandle, IntPtr? instanceHandle)
    {
        if (_graphicsDevice == null)
            CreateDevice();

        if (_graphicsDevice == null)
            throw new Exception($"Failed to create graphics device!");

        // OpenGL uses mainswapchain instead
        if (GraphicsRuntime.GraphicsBackend != GraphicsBackend.OpenGL &&
            GraphicsRuntime.GraphicsBackend != GraphicsBackend.OpenGLES)
        {
            _swapchain = CreateSwapchain(controlHandle, instanceHandle);
        }
        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        Debug.WriteLine($"Created resources");
    }

    private Swapchain CreateSwapchain(IntPtr? controlHandle, IntPtr? instanceHandle)
    {
        IntPtr handle = controlHandle ?? throw new Exception();
        IntPtr instance = instanceHandle ?? throw new Exception();
        IntPtr display = IntPtr.Zero;

        var depthhFormat = PixelFormat.R32_Float;
        bool vsync = false;

        // Note: this swapchain behavior cannot be used on opengl backend due to the behavior of opengl
        if (OperatingSystem.IsWindows())
        {
            var swapchainSource = SwapchainSource.CreateWin32(handle, IntPtr.Zero);
            var swapchainDescription = new SwapchainDescription(swapchainSource, 854, 480, depthhFormat, vsync);

            return _graphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
        }
        if (OperatingSystem.IsMacOS())
        {
            // UIView supports vulkan, otherwise default to NSView for metal
            var swapchainSource = _graphicsDevice.BackendType == GraphicsBackend.Vulkan ?
                    SwapchainSource.CreateUIView(handle) : SwapchainSource.CreateNSView(handle);
            var swapchainDescription = new SwapchainDescription(swapchainSource, 854, 480, depthhFormat, vsync);

            return _graphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
        }
        if (OperatingSystem.IsLinux())
        {
            bool IsWayland = false;

            var swapchainSource = IsWayland ? SwapchainSource.CreateWayland(display, handle)
                                            : SwapchainSource.CreateXlib(display, handle);

            var swapchainDescription = new SwapchainDescription(swapchainSource, 854, 480, depthhFormat, vsync);

            return _graphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
        }
        if (OperatingSystem.IsAndroid())
        {
            var swapchainSource = SwapchainSource.CreateAndroidSurface(handle, IntPtr.Zero);
            var swapchainDescription = new SwapchainDescription(swapchainSource, 854, 480, depthhFormat, vsync);

            return _graphicsDevice.ResourceFactory.CreateSwapchain(swapchainDescription);
        }
        throw new Exception($"Platform not supported!");
    }

    public async Task Loop()
    {
        if (!load)
        {
            OnLoad?.Invoke();
            Prepare(_graphicsDevice);
            load = true;
        }

        while (IsRunning)
        {
            // Sleep if no window visible
            if (WindowWidth == 0 || WindowHeight == 0)
            {
                await Task.Delay(10);
                continue;
            }
            if (!IsRendering)
            {
                await Task.Delay(1);
                continue;
            }

            try
            {
                if (!drawing)
                {
                    drawing = true;
                    // MainSwapchain  used by opengl, else use custom set swap chain
                    RenderFrame(_graphicsDevice, _commandList,
                        _swapchain == null ? _graphicsDevice.MainSwapchain : _swapchain);
                    drawing = false;
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);
                throw new Exception(ex.ToString());
            }

            // Calculate elapsed time
            stopwatch.Stop();
            double elapsedTime = stopwatch.Elapsed.TotalMilliseconds;

            // Sleep for the remaining time to maintain FPS
            double sleepTime = targetFrameTime - elapsedTime;
            if (sleepTime > 0)
            {
                await Task.Delay((int)sleepTime);
            }
        }
        Dispose(_graphicsDevice);
        IsDisposed = true;
    }
    
    public virtual void Prepare(GraphicsDevice gd)
    {

    }

    public virtual void RenderFrame(GraphicsDevice gd, CommandList cmdList, Swapchain swapchain)
    {
        if (WindowWidth != swapchain.Framebuffer.Width || WindowHeight != swapchain.Framebuffer.Height)
            swapchain.Resize(WindowWidth, WindowHeight);

        cmdList.Begin();
        cmdList.SetFramebuffer(swapchain.Framebuffer);
        cmdList.ClearColorTarget(0, RgbaFloat.Grey);
        cmdList.ClearDepthStencil(1f);

        cmdList.End();

        gd.SubmitCommands(cmdList);
        gd.SwapBuffers(swapchain);
    }

    public virtual void Resize(uint w, uint h)
    {
        this.WindowWidth = w;
        this.WindowHeight = h;
    }

    public void Close()
    {
        IsRunning = false; // Stop running, dispose data on thread
    }

    public virtual void Dispose(GraphicsDevice gd)
    {
        _swapchain?.Dispose();
        _commandList?.Dispose();
    }
}