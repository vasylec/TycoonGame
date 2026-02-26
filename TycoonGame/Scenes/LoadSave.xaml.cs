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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Media.Animation;

namespace TycoonGame.Scenes
{
    /// <summary>
    /// Interaction logic for LoadSave.xaml
    /// </summary>
    public partial class LoadSave : Page
    {
        private MainMenu _parentWindow;

        public LoadSave(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            slot1.Content = App.saveName;


        }

        private void LoadSave_Loaded(object sender, RoutedEventArgs e)
        {
            UIHelper.ApplyPixelFontAndSettings(this);
            SetupAnimatedBackground();
        }

        private void btnMainMenu_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.GoBack();
        }

        private void btnLoad1_Click(object sender, RoutedEventArgs e)
        {
            App.saveName = "Slot 1";
            _parentWindow.NavigateTo(new Page1(_parentWindow));
        }

        private void btnLoad2_Click(object sender, RoutedEventArgs e)
        {
            App.saveName = "Slot 2";
            _parentWindow.NavigateTo(new Page1(_parentWindow));
        }

        private void btnLoad3_Click(object sender, RoutedEventArgs e)
        {
            App.saveName = "Slot 3";
            _parentWindow.NavigateTo(new Page1(_parentWindow));
        }

        private void SetupAnimatedBackground()
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MainMenuBackground.png");
                if (!System.IO.File.Exists(fullPath))
                    return;

                var img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(fullPath, UriKind.Absolute);
                img.DecodePixelWidth = 1920;
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();

                AnimatedBackgroundBrush.ImageSource = img;

                var animX = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(80), RepeatBehavior = RepeatBehavior.Forever };
                var animY = new DoubleAnimation { From = 0, To = 1, Duration = TimeSpan.FromSeconds(110), RepeatBehavior = RepeatBehavior.Forever };
                BackgroundTranslate.BeginAnimation(TranslateTransform.XProperty, animX);
                BackgroundTranslate.BeginAnimation(TranslateTransform.YProperty, animY);
            }
            catch
            {
            }
        }
    }
}
