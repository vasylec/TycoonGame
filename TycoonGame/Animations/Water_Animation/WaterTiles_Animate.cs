using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TycoonGame.Animations
{
    internal class WaterTiles_Animate
    {
        public WaterTiles[,] Tiles { get; private set; }
        private int width, height;

        public WaterTiles_Animate(int width, int height)
        {
            this.width = width;
            this.height = height;
            Tiles = new WaterTiles[width, height];
        }

        public void UpdateFrames()
        {
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    Tiles[x, y]?.NextFrame();
        }
    }
}
