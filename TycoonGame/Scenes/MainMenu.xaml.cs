using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        public MainMenu()
        {
            InitializeComponent();   

            this.WindowState = WindowState.Maximized;
            this.WindowStyle = WindowStyle.None;
            this.Topmost = true;

            _instance = this;
            _originalContent = this.Content as UIElement;
        }

        private async void FadeTransition(UIElement oldContent, UIElement newContent)
        {
            // Fade OUT pagina veche
            if (oldContent != null)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                fadeOut.Completed += (s, e) => { /* opțional */ };
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }

            // ✅ FIX: Setează pagina nouă INVIZIBILĂ de la început
            if (newContent != null)
            {
                newContent.Opacity = 0;  // ← CHEIA - fără flicker!
                this.Content = newContent;  // Schimbă conținutul ACUM

                // Apoi fade IN
                await Task.Delay(50);  // Delay minim
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
    }
}
