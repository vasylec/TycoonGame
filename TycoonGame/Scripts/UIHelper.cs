using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TycoonGame
{
    public static class UIHelper
    {
        // Font pixel art global
        private static FontFamily _pixelFont = new FontFamily(new Uri("pack://application:,,,/"), "./Assets/Fonts/#Daydream");

        /// <summary>
        /// Aplică font pixel și setări vizuale recursiv pe toate elementele
        /// </summary>
        public static void ApplyPixelFontAndSettings(DependencyObject parent)
        {
            if (parent == null) return;

            int count = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                // Font pixel
                if (child is Button btn)
                    btn.FontFamily = _pixelFont;
                else if (child is Label lbl)
                    lbl.FontFamily = _pixelFont;
                else if (child is TextBlock tb)
                    tb.FontFamily = _pixelFont;

                // Imagini, scalare și edge mode
                if (child is Image img)
                {
                    RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.NearestNeighbor);
                    RenderOptions.SetEdgeMode(img, EdgeMode.Aliased);
                    img.SnapsToDevicePixels = true;
                    img.UseLayoutRounding = true;
                }

                // Recursiv
                ApplyPixelFontAndSettings(child);
            }
        }
    }
}
