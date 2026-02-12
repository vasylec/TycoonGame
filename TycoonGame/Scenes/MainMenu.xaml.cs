using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TycoonGame.Scenes
{
    /// <summary>
    /// Interaction logic for MainMenu.xaml
    /// </summary>
    public partial class MainMenu : Window
    {
        private UIElement _originalContent;
        private UIElement _currentContent;
        private static MainMenu _instance;

        // Font pixel art Daydream
        private FontFamily _pixelFont;

        public MainMenu()
        {
            InitializeComponent();

            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            _instance = this;
            _originalContent = this.Content as UIElement;

            // Încarcă font pixel art Daydream
            _pixelFont = new FontFamily(
                new Uri("pack://application:,,,/"),
                        "./Assets/Fonts/#Daydream" 
            );

            this.Loaded += MainMenu_Loaded;
        }
        private void MainMenu_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyPixelFont(this);
        }
        // Recursiv aplică font pixel art la toate Button, Label și TextBlock
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



                ApplyPixelFont(child); // recursiv
            }
        }

        private async void FadeTransition(UIElement oldContent, UIElement newContent)
        {
            // Fade OUT pagina veche
            if (oldContent != null)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }

            // Setează pagina nouă INVIZIBILĂ și fade IN
            if (newContent != null)
            {
                newContent.Opacity = 0;
                this.Content = newContent;
                _currentContent = newContent;

                await Task.Delay(50);
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                newContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
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

        private void exitButtonClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            LoadSave loadsave = new LoadSave(this);
            FadeTransition(this.Content as UIElement, loadsave);
            this.Content = loadsave;
            _currentContent = loadsave;
        }
        private void btnShowPopup_Click(object sender, RoutedEventArgs e)
        {
            popupContainer.Visibility = Visibility.Visible; // arată popup-ul

            newGameTextBlock.Text = "Enter your save name !"; 
            newGameTextBox.Text = "";
        }

        private void ClosePopup_Click(object sender, RoutedEventArgs e)
        {
            popupContainer.Visibility = Visibility.Collapsed; // ascunde popup-ul
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
    }
}
