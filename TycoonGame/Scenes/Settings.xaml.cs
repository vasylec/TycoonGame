using System.Windows;
using System.Windows.Controls;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class Settings : Page
    {
        private SoundManager sound => App.Sound;
        private MainMenu _parentWindow;

        public Settings(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            // Inițializează slider-ele cu valorile curente din SoundManager
            trackMaster.Value = sound.MasterVolume * 100;
            trackMusic.Value = sound.MusicVolume * 100;
            trackSFX.Value = sound.SFXVolume * 100;
        }

        private void trackMaster_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sound.MasterVolume = (float)(trackMaster.Value / 100.0);
            sound.UpdateMusicVolume();
            sound.UpdateSFXVolume();
        }

        private void trackMusic_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            sound.MusicVolume = (float)(trackMusic.Value / 100.0);
            sound.UpdateMusicVolume();
        }

        private void trackSFX_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
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
    }
}
