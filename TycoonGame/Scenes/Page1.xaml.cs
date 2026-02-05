using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class Page1 : Page
    {
        private MainMenu _parentWindow;

        public Page1(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;
        }

        private async void FadeTransition(UIElement oldContent, UIElement newContent)
        {
            // Fade OUT pagina veche
            if (oldContent != null)
            {
                DoubleAnimation fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
                oldContent.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }

            // Fade IN pagina nouă (după 150ms delay)
            await Task.Delay(150);
            if (newContent != null)
            {
                newContent.Opacity = 0;
                DoubleAnimation fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.3));
                newContent.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }




        private void Button_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.GoBack(); // ← ÎNAPOI
        }
    }
}
