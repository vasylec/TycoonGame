using System;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TycoonGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly SoundPlayer? _clickPlayer;

        public MainWindow()
        {
            InitializeComponent();

            _clickPlayer = CreateClickPlayer();

            // Se aplica global pe toate butoanele din fereastra (inclusiv cele adaugate dinamic)
            AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(OnAnyButtonClick));
        }

        private SoundPlayer? CreateClickPlayer()
        {
            try
            {
                string soundPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Sounds", "select_005.wav");
                if (!File.Exists(soundPath))
                    return null;

                var player = new SoundPlayer(soundPath);
                player.Load();
                return player;
            }
            catch
            {
                return null;
            }
        }

        private void OnAnyButtonClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _clickPlayer?.Play();
            }
            catch
            {
                // Ignoram silent daca sunetul nu poate fi redat
            }
        }
    }
}
