using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace TycoonGame.Helpers
{
    public class VisualHost : FrameworkElement
    {
        private readonly VisualCollection visuals;

        public VisualHost()
        {
            visuals = new VisualCollection(this);
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
            RenderOptions.SetEdgeMode(this, EdgeMode.Aliased);
            SnapsToDevicePixels = true;
            UseLayoutRounding = true;
        }

        public void AddVisual(Visual visual)
        {
            visuals.Add(visual);
        }

        protected override int VisualChildrenCount => visuals.Count;
        protected override Visual GetVisualChild(int index) => visuals[index];
    }
}