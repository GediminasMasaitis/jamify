using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace Jamify
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private WindowState OldWindowState { get; set; }
        private WindowStyle OldWindowStyle { get; set; }
        private ResizeMode OldResizeMode { get; set; }
        private bool FullScreen { get; set; }
        private bool TopMost { get; set; }
        private Rect Bounds { get; set; }

        private GifManagerService GifManager { get; }

        public MainWindow()
        {
            InitializeComponent();

            HelpLabel.Content =
                "[F1]       Toggle Help\n" +
                "[F2]       Toggle Info\n" +
                "[Space]    Beat\n" +
                "[Escape]   Reset\n" +
                "[Up]       Increase speed\n" +
                "[Down]     Decrease speed\n" +
                "[Left]     Decrease offset\n" +
                "[Right]    Increase offset\n" +
                "[F4]       Toggle gif reverse\n" +
                "[F5]       Decrease start frame\n" +
                "[F6]       Increase start frame\n" +
                "[F7]       Decrease end frame\n" +
                "[F8]       Increase end frame\n" +
                "[PgDown]   Decrease render speed\n" +
                "[PgUp]     Increase render speed\n" +
                "[F9]       Load .gif\n" +
                "[F12]      Fullscreen";

            GifManager = new GifManagerService();
            GifManager.FrameChanged += GifManagerOnFrameChanged;
        }

        private void GifManagerOnFrameChanged(object sender, FrameChangedEventArgs args)
        {
            var timeout = new TimeSpan(0, 0, 0, 1);
            Dispatcher.Invoke(DispatcherPriority.Normal, timeout, new Action(() =>
            {
                if(StatsLabel.IsVisible)
                {
                    StatsLabel.Content = GifManager.GetStatusString();
                }
                if (args.FrameChanged)
                {
                    MainImage.Source = args.CurrentFrame;
                }
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var path = "default.gif";
            if (!File.Exists(path))
            {
                path = QueryFileLocation();
                if (path == null)
                {
                    Environment.Exit(0);
                    return;
                }
            }
            GifManager.Start(path);
        }

        private void EnterFullScreen()
        {
            var h = new WindowInteropHelper(this);
            OldWindowState = WindowState;
            OldWindowStyle = WindowStyle;
            OldResizeMode = ResizeMode;
            TopMost = Topmost;
            Bounds = new Rect(Left,Top,Width,Height);
            //Bounds = Screen.FromHandle(new WindowInteropHelper(this).Handle).Bounds;
            WindowState = WindowState.Maximized;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            //Topmost = true;
            FullScreen = true;
            User32.SetWinFullScreen(h.Handle);
        }

        private void ExitFullScreen()
        {
            WindowState = OldWindowState;
            WindowStyle = OldWindowStyle;
            ResizeMode = OldResizeMode;
            Topmost = TopMost;
            Left = Bounds.X;
            Top = Bounds.Y;
            Width = Bounds.Width;
            Height = Bounds.Height;
            FullScreen = false;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Space:
                    GifManager.Beat();
                    break;
                case Key.Escape:
                    GifManager.Clear();
                    break;
                case Key.Left:
                    if (GifManager.Offset > 0)
                    {
                        GifManager.Offset--;
                    }
                    else
                    {
                        GifManager.Offset = GifManager.Frames.Length - 1;
                    }
                    break;
                case Key.Right:
                    if (GifManager.Offset < GifManager.Frames.Length - 1)
                    {
                        GifManager.Offset++;
                    }
                    else
                    {
                        GifManager.Offset = 0;
                    }
                    break;
                case Key.Down:
                        GifManager.LoopDuration++;
                    break;
                case Key.Up:
                    if (GifManager.LoopDuration > 10)
                    {
                        GifManager.LoopDuration--;
                    }
                    break;
                case Key.F1:
                    HelpLabel.Visibility = HelpLabel.IsVisible ? Visibility.Hidden : Visibility.Visible;
                    break;
                case Key.F2:
                    StatsLabel.Visibility = StatsLabel.IsVisible ? Visibility.Hidden : Visibility.Visible;
                    break;
                case Key.F4:
                    GifManager.Reverse = !GifManager.Reverse;
                    break;
                case Key.F5:
                    if (GifManager.StartFrame > 0)
                    {
                        GifManager.StartFrame--;
                    }
                    break;
                case Key.F6:
                    if (GifManager.StartFrame < GifManager.EndFrame)
                    {
                        GifManager.StartFrame++;
                    }
                    break;
                case Key.F7:
                    if (GifManager.EndFrame > GifManager.StartFrame)
                    {
                        GifManager.EndFrame--;
                    }
                    break;
                case Key.F8:
                    if (GifManager.EndFrame < GifManager.Frames.Length - 1)
                    {
                        GifManager.EndFrame++;
                    }
                    break;
                case Key.PageUp:
                    if (GifManager.RenderDelay > 1)
                    {
                        GifManager.RenderDelay--;
                    }
                    break;
                case Key.PageDown:
                    GifManager.RenderDelay++;
                    break;
                case Key.F9:
                    var location = QueryFileLocation();
                    if (location != null)
                    {
                        GifManager.Start(location);
                    }
                    break;
                case Key.F12:
                    if (FullScreen)
                    {
                        ExitFullScreen();
                    }
                    else
                    {
                        EnterFullScreen();
                    }
                    break;
            }
        }

        private string QueryFileLocation()
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".gif",
                Filter = "GIF Files (*.gif)|*.gif"
            };

            var result = dlg.ShowDialog();

            if (result == true)
            {
                return dlg.FileName;
            }
            return null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            GifManager.Dispose();
        }
    }
}
