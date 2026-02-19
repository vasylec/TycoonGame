using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Resources;
using TycoonGame.Scripts;

namespace TycoonGame
{
    public partial class App : Application
    {
        public static SoundManager Sound { get; private set; } = null!;
        public static string saveName = "";

        // Cursori globali
        public static Cursor NormalCursor { get; private set; } = null!;
        public static Cursor HoverCursor { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Sunet
            Sound = new SoundManager();
            Sound.PlayMusic("Assets/Sounds/music.mp3");
            Sound.InitClickSound(); // 🔹 esențial

            // Încarcă cursori global
            NormalCursor = LoadCursor("Assets/Cursors/pointer.cur");
            HoverCursor = LoadCursor("Assets/Cursors/hand.cur");

            // Aplică click sound global după ce fereastra principală e gata
            this.Startup += (s, ev) =>
            {
                if (Current.MainWindow != null)
                    AttachClickSoundRecursive(Current.MainWindow);
            };
        }

        private static Cursor LoadCursor(string path)
        {
            StreamResourceInfo sri = GetResourceStream(new Uri($"pack://application:,,,/{path}"));
            if (sri == null)
                throw new Exception($"Cursor not found: {path}");

            return new Cursor(sri.Stream);
        }

        // 🔹 Click sound global pentru toate butoanele
        public static void AttachClickSoundRecursive(DependencyObject parent)
        {
            if (parent == null) return;

            int count = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is Button btn)
                {
                    // Evită dublarea handler-ului
                    btn.Click -= Button_ClickSound;
                    btn.Click += Button_ClickSound;
                }

                AttachClickSoundRecursive(child);
            }
        }

        private static void Button_ClickSound(object sender, RoutedEventArgs e)
        {
            Sound.PlayClick(); // 🔊 click instant
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Sound.Dispose();
            base.OnExit(e);
        }
    }
}
