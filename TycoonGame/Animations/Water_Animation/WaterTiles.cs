using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media.Imaging;

namespace TycoonGame.Animations
{
    internal class WaterTiles
    {
        public BitmapSource[] Frames { get; set; }
        public int CurrentFrame { get; set; } = 0;

        public WaterTiles(BitmapSource[] frames)
        {
            Frames = frames;
        }

        public BitmapSource GetCurrentFrame() => Frames[CurrentFrame];

        public void NextFrame() => CurrentFrame = (CurrentFrame + 1) % Frames.Length;
    }
}