using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class Settings : Page
    {
        private SoundManager sound => App.Sound;
        private MainMenu _parentWindow;
        private bool _isInitializing;

        public Settings(MainMenu parent)
        {
            _isInitializing = true;
            InitializeComponent();
            _parentWindow = parent;

            // Inițializează slider-ele cu valorile curente din SoundManager
            trackMaster.Value = sound.MasterVolume * 100;
            trackMusic.Value = sound.MusicVolume * 100;
            trackSFX.Value = sound.SFXVolume * 100;
            _isInitializing = false;

            UIHelper.ApplyPixelFontAndSettings(this);

        }

        private void trackMaster_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            sound.MasterVolume = (float)(trackMaster.Value / 100.0);
            sound.UpdateMusicVolume();
            sound.UpdateSFXVolume();
        }

        private void trackMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            sound.MusicVolume = (float)(trackMusic.Value / 100.0);
            sound.UpdateMusicVolume();
        }

        private void trackSFX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_isInitializing) return;
            sound.SFXVolume = (float)(trackSFX.Value / 100.0);
            sound.UpdateSFXVolume();
        }

        private void btnBack_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.GoBack();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.GoBack();
        }

        private void Settings_Loaded(object sender, RoutedEventArgs e)
        {
            UIHelper.ApplyPixelFontAndSettings(this);
            SetupAnimatedBackground();
        }

        private void SetupAnimatedBackground()
        {
            try
            {
                string fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MainMenuBackground.png");
                if (!File.Exists(fullPath))
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
