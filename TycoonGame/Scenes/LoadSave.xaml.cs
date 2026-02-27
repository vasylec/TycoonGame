using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class LoadSave : Page
    {
        private readonly MainMenu _parentWindow;

        public LoadSave(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;
        }

        private void LoadSave_Loaded(object sender, RoutedEventArgs e)
        {
            UIHelper.ApplyPixelFontAndSettings(this);
            SetupAnimatedBackground();
            RefreshSlots();
        }

        private void RefreshSlots()
        {
            PaintSlot(1, slot1, slotDate1);
            PaintSlot(2, slot2, slotDate2);
            PaintSlot(3, slot3, slotDate3);
        }

        private static void PaintSlot(int slot, Label name, Label date)
        {
            var data = SaveSystem.LoadSlot(slot);
            if (data == null)
            {
                name.Content = $"Slot {slot} - Empty";
                date.Content = "No save";
                return;
            }

            name.Content = $"{data.SaveName} | ${data.Money:0} | Pop {data.Population}";
            date.Content = data.SavedAtUtc.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
        }

        private void btnMainMenu_Click(object sender, RoutedEventArgs e) => _parentWindow.GoBack();

        private void btnLoad1_Click(object sender, RoutedEventArgs e) => LoadSlot(1);
        private void btnLoad2_Click(object sender, RoutedEventArgs e) => LoadSlot(2);
        private void btnLoad3_Click(object sender, RoutedEventArgs e) => LoadSlot(3);

        private void btnSave1_Click(object sender, RoutedEventArgs e) => PrepareSlotAndStart(1);
        private void btnSave2_Click(object sender, RoutedEventArgs e) => PrepareSlotAndStart(2);
        private void btnSave3_Click(object sender, RoutedEventArgs e) => PrepareSlotAndStart(3);

        private void btnDelete1_Click(object sender, RoutedEventArgs e) => DeleteSlot(1);
        private void btnDelete2_Click(object sender, RoutedEventArgs e) => DeleteSlot(2);
        private void btnDelete3_Click(object sender, RoutedEventArgs e) => DeleteSlot(3);

        private void LoadSlot(int slot)
        {
            if (!SaveSystem.SlotExists(slot))
            {
                MessageBox.Show($"Slot {slot} is empty.");
                return;
            }

            var data = SaveSystem.LoadSlot(slot)!;
            App.currentSlot = slot;
            App.saveName = data.SaveName;
            _parentWindow.NavigateTo(new Page1(_parentWindow));
        }

        private void PrepareSlotAndStart(int slot)
        {
            App.currentSlot = slot;
            if (string.IsNullOrWhiteSpace(App.saveName))
                App.saveName = $"Slot {slot}";

            _parentWindow.NavigateTo(new Page1(_parentWindow));
        }

        private void DeleteSlot(int slot)
        {
            SaveSystem.DeleteSlot(slot);
            RefreshSlots();
        }

        private void SetupAnimatedBackground()
        {
            try
            {
                string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "MainMenuBackground.png");
                if (!System.IO.File.Exists(fullPath))
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
            catch { }
        }
    }
}
