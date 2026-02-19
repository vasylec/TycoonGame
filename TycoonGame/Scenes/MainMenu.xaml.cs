using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TycoonGame.Scenes
{
    public partial class MainMenu : Window
    {
        private UIElement _originalContent;
        private UIElement _currentContent;
        private static MainMenu _instance;

        private FontFamily _pixelFont;

        public MainMenu()
        {
            InitializeComponent();

            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            _instance = this;
            _originalContent = this.Content as UIElement;

            // Font pixel art
            _pixelFont = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Daydream");

            // Setăm cursorul implicit global
            this.Cursor = App.NormalCursor;

            this.Loaded += MainMenu_Loaded;
        }

        private void MainMenu_Loaded(object sender, RoutedEventArgs e)
        {
            // Aplică font și setări vizuale pe tot conținutul ferestrei
            UIHelper.ApplyPixelFontAndSettings(this);

            // Aplicăm hover cursor pe butoane
            AttachCursorEvents(this);
        }


        private void AttachCursorEvents(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn)
                {
                    // Setăm cursor doar pe acest buton
                    btn.Cursor = App.NormalCursor; // inițial pointer
                    btn.MouseEnter += (s, e) =>
                    {
                        btn.Cursor = App.HoverCursor; // hand la hover
                    };
                    btn.MouseLeave += (s, e) =>
                    {
                        btn.Cursor = App.NormalCursor; // revine la pointer
                    };
                }

                // Recursiv pentru copii
                AttachCursorEvents(child);
            }
        }

        // Aplică font pixel art recursiv
        private void ApplyPixelFont(DependencyObject parent)
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn)
                    btn.FontFamily = _pixelFont;
                else if (child is Label lbl)
                    lbl.FontFamily = _pixelFont;
                else if (child is TextBlock tb)
                    tb.FontFamily = _pixelFont;

                if (child is Image img)
                {
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
                    img.SnapsToDevicePixels = true;
                    img.UseLayoutRounding = true;
                }

                ApplyPixelFont(child);
            }
        }

        private async void FadeTransition(UIElement oldContent, UIElement newContent)
        {
            if (oldContent != null)
            {
                DoubleAnimation fadeOut =
                    new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }

            if (newContent != null)
            {
                newContent.Opacity = 0;
                this.Content = newContent;
                _currentContent = newContent;

                await Task.Delay(50);

                DoubleAnimation fadeIn =
                    new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                newContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                // 🔥 Atașăm hover cursori la butoanele din pagina nouă
                AttachCursorEvents(newContent);
            }
        }


        public void GoBack()
        {
            FadeTransition(_currentContent, _originalContent);
            _currentContent = _originalContent;
            this.Content = _originalContent;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Page1 page = new Page1(this);
            FadeTransition(this.Content as UIElement, page);
            this.Content = page;
            _currentContent = page;

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Settings settings = new Settings(this);
            FadeTransition(this.Content as UIElement, settings);
            this.Content = settings;
            _currentContent = settings;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LoadSave loadsave = new LoadSave(this);
            FadeTransition(this.Content as UIElement, loadsave);
            this.Content = loadsave;
            _currentContent = loadsave;
        }

        private void exitButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnShowPopup_Click(object sender, RoutedEventArgs e)
        {
            popupContainer.Visibility = Visibility.Visible;
            newGameTextBlock.Text = "Enter your save name !";
            newGameTextBox.Text = "";
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            popupContainer.Visibility = Visibility.Collapsed;
        }

        private void btn_newGame(object sender, RoutedEventArgs e)
        {
            App.saveName = newGameTextBox.Text;

            if (string.IsNullOrWhiteSpace(App.saveName))
            {
                MessageBox.Show("Please enter a valid game title.");
                return;
            }

            newGameTextBlock.Text = $"Starting new game: {App.saveName}";
        }

        private void Button_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {

        }
    }
}
    