using System.Windows;
using System.Windows.Controls;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class Settings : Page
    {
        private SoundManager soundManager = new SoundManager();

        public Settings()
        {
            InitializeComponent();

            // Pornește muzica automat
            soundManager.PlayMusic(@"C:\Users\Marius\Desktop\TycoonGame-golimaz\TycoonGame\Sounds\music.mp3");
            soundManager.PlaySFX(@"C:\Users\Marius\Desktop\TycoonGame-golimaz\TycoonGame\Sounds\sfx.wav");

            // Valori initiale slider
            trackMaster.Value = 100;
            trackMusic.Value = 0;
            trackSFX.Value = 0;


        }

        private void trackMaster_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (soundManager == null) return;
            soundManager.MasterVolume = (float)(trackMaster.Value / 100.0f);
            soundManager.UpdateMusicVolume();
            soundManager.UpdateSFXVolume();
        }

        private void trackSFX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (soundManager == null) return;
            soundManager.SFXVolume = (float)(trackSFX.Value / 100.0f);
            soundManager.UpdateSFXVolume();
        }

        private void trackMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (soundManager == null) return;
            soundManager.MusicVolume = (float)(trackMusic.Value / 100.0f);
            soundManager.UpdateMusicVolume();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            // Navighează înapoi în Frame-ul care găzduiește Page-ul
            if (this.NavigationService.CanGoBack)
                this.NavigationService.GoBack();
        }
    }
}
