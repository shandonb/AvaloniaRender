using Avalonia.Controls;
using AvaloniaRender.OpenGL;
using AvaloniaRender.Veldrid;
using AvaloniaRender.ViewModels;
using Veldrid;

namespace AvaloniaRender.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LoadOpenGL();
            LoadVeldrid();
        }

        private void LoadVeldrid()
        {
            tabControl.Items.Add(new TabItem()
            {
                Header = $"Veldrid {GraphicsRuntime.GraphicsBackend}",
                Content = new EmbeddedWindowVeldrid()
                {
                    DataContext = new VeldridWindowViewModel()
                }
            });
        }

        private void LoadOpenGL()
        {
            tabControl.Items.Add(new TabItem()
            {
                Header = "Open GL",
                Content = new EmbeddedWindowOpenGL()
                {
                    DataContext = new OpenGLWindowViewModel()
                }
            });
        }
    }
}