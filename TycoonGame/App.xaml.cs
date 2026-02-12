using System.Windows;
using TycoonGame.Scripts;

namespace TycoonGame
{
    public partial class App : Application
    {
        public static SoundManager Sound { get; private set; } = null!;
        public static String saveName;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            Sound = new SoundManager();

            Sound.PlayMusic("Sounds/music.mp3");

        }

        protected override void OnExit(ExitEventArgs e)
        {
            Sound.Dispose();
            base.OnExit(e);
        }
    }
}
