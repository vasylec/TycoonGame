using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.IO;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class Page1 : Page
    {
        private readonly MainMenu _parentWindow;
        private readonly DispatcherTimer _gameTimer;

        private decimal _money = 250;
        private int _population = 0;

        private readonly List<LotState> _lots = new();
        private readonly List<Border> _lotControls = new();

        private readonly Dictionary<string, BuildingDef> _defs = new()
        {
            ["House"] = new BuildingDef("House", 100, 6, 4, "/Assets/Buildings/BuildingBeige.png", 180, 11, 6, "/Assets/Buildings/BuildingBeige_LVL2.png"),
            ["Shop"] = new BuildingDef("Shop", 220, 12, 2, "/Assets/Buildings/BuildingBlue.png", 320, 19, 4, "/Assets/Buildings/BuildingBlue_LVL2.png"),
            ["Factory"] = new BuildingDef("Factory", 300, 18, 1, "/Assets/Buildings/BuildingWhite.png", 430, 28, 2, "/Assets/Buildings/BuildingWhite_LVL2.png")
        };

        private string? _selectedBuilding;

        private bool _isDragging;
        private string? _dragBuilding;
        private Point _dragStart;
        private Point _lastDragMousePos;
        private double _currentDragAngle;
        private int _upgradeLotIndex = -1;

        public Page1(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            SaveNameText.Text = $"Save: {(string.IsNullOrWhiteSpace(App.saveName) ? "NoName" : App.saveName)}";

            _lotControls.AddRange(new[] { Lot1, Lot2, Lot3, Lot4, Lot5, Lot6 });
            for (int i = 0; i < _lotControls.Count; i++)
                _lots.Add(new LotState());

            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _gameTimer.Tick += (_, _) => TickGame();
            _gameTimer.Start();

            TryLoadCurrentSlot();

            UIHelper.ApplyPixelFontAndSettings(this);
            RefreshHud();
        }

        private void TickGame()
        {
            _money += _lots.Sum(x => x.IncomePerSec);
            RefreshHud();
        }

        private void RefreshHud()
        {
            MoneyText.Text = $"${_money:0}";
            IncomeText.Text = $"Income/s: ${_lots.Sum(x => x.IncomePerSec):0}";
            PopulationText.Text = $"Pop: {_population}";
        }

        private void SelectBuilding_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string key && _defs.ContainsKey(key))
            {
                _selectedBuilding = key;
                SelectionText.Text = $"Selected building: {key}";
                StatusText.Text = $"Ai selectat {key}. Acum click pe lot sau drag & drop.";
            }
        }

        private void SelectBuilding_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Button btn || btn.Tag is not string key) return;

            if (_isDragging)
                StopDrag();

            _selectedBuilding = key;
            _dragBuilding = key;
            _dragStart = e.GetPosition(RootGrid);

            SelectionText.Text = $"Selected building: {key}";
            StatusText.Text = $"Ai selectat {key}. Click pe lot sau trage clădirea.";
            e.Handled = true;
        }

        private void Building_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton != MouseButtonState.Pressed) return;
            if (sender is not Button btn || btn.Tag is not string key) return;

            if (!_isDragging)
            {
                var current = e.GetPosition(RootGrid);
                if (Math.Abs(current.X - _dragStart.X) < 6 && Math.Abs(current.Y - _dragStart.Y) < 6)
                    return;

                StartDrag(key, current);
                e.Handled = true;
            }
        }

        private void StartDrag(string buildingKey, Point mousePos)
        {
            _isDragging = true;
            _dragBuilding = buildingKey;

            DragGhost.Source = LoadSprite(_defs[buildingKey].Sprite);
            DragGhost.Visibility = Visibility.Visible;
            _lastDragMousePos = mousePos;
            _currentDragAngle = 0;
            DragGhostRotate.Angle = 0;
            UpdateDragGhost(mousePos);

            SelectionText.Text = $"Selected building: {buildingKey} (dragging)";
        }

        private void StopDrag()
        {
            _isDragging = false;
            _dragBuilding = null;
            DragGhost.Visibility = Visibility.Collapsed;
            _currentDragAngle = 0;
            DragGhostRotate.Angle = 0;

            if (!string.IsNullOrWhiteSpace(_selectedBuilding))
                SelectionText.Text = $"Selected building: {_selectedBuilding}";
            else
                SelectionText.Text = "Selected building: none";
        }

        private void RootGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_isDragging) return;

            if (e.LeftButton != MouseButtonState.Pressed)
            {
                StopDrag();
                return;
            }

            UpdateDragGhost(e.GetPosition(RootGrid));
        }

        private void RootGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging || string.IsNullOrWhiteSpace(_dragBuilding)) return;

            var mousePosOnIsland = e.GetPosition(IslandCanvas);
            var hit = VisualTreeHelper.HitTest(IslandCanvas, mousePosOnIsland);
            var lot = FindParentLot(hit?.VisualHit as DependencyObject);

            if (lot != null)
            {
                var idx = GetLotIndex(lot);
                PlaceBuilding(idx, _dragBuilding);
            }

            StopDrag();
        }

        private Border? FindParentLot(DependencyObject? current)
        {
            while (current != null)
            {
                if (current is Border b && b.Tag != null)
                    return b;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        private void UpdateDragGhost(Point mousePos)
        {
            Canvas.SetLeft(DragGhost, mousePos.X - DragGhost.Width / 2);
            Canvas.SetTop(DragGhost, mousePos.Y - DragGhost.Height / 2 - 8);

            var dx = mousePos.X - _lastDragMousePos.X;

            // Mouse right => building tilts right; mouse left => tilts left
            var targetAngle = dx * 3.8;
            if (targetAngle > 40) targetAngle = 40;
            if (targetAngle < -40) targetAngle = -40;

            // If almost still, spring back toward 0
            if (Math.Abs(dx) < 0.45)
                targetAngle = 0;

            // Smooth + pronounced inertia
            _currentDragAngle = (_currentDragAngle * 0.68) + (targetAngle * 0.32);
            DragGhostRotate.Angle = _currentDragAngle;
            _lastDragMousePos = mousePos;
        }

        private void Lot_Click(object sender, MouseButtonEventArgs e)
        {
            if (_isDragging) return;
            if (sender is not Border lotControl) return;

            var idx = GetLotIndex(lotControl);
            if (!_lots[idx].IsEmpty)
            {
                ShowUpgradePanel(idx, lotControl);
                return;
            }

            HideUpgradePanel();

            if (_selectedBuilding is null)
            {
                StatusText.Text = "Selectează întâi o clădire din stânga.";
                return;
            }

            PlaceBuilding(idx, _selectedBuilding);
        }

        private void ShowUpgradePanel(int lotIndex, Border lotControl)
        {
            var lot = _lots[lotIndex];
            _upgradeLotIndex = lotIndex;

            Canvas.SetLeft(UpgradePanel, Canvas.GetLeft(lotControl) + lotControl.Width + 10);
            Canvas.SetTop(UpgradePanel, Canvas.GetTop(lotControl));

            if (!_defs.TryGetValue(lot.BuildingKey, out var def))
            {
                UpgradePanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (lot.Level >= 2)
            {
                UpgradePreviewImage.Source = LoadSprite(lot.Sprite);
                UpgradeStatsText.Text = $"{lot.BuildingName} este deja la nivel maxim (LVL2).\nIncome: +${lot.IncomePerSec}/s\nPop: +{lot.PopulationGain}";
                UpgradeButton.IsEnabled = false;
                UpgradeButton.Content = "MAX";
            }
            else
            {
                UpgradePreviewImage.Source = LoadSprite(def.SpriteLvl2);
                UpgradeStatsText.Text = $"Upgrade cost: ${def.UpgradeCost}\nAfter upgrade (LVL2):\nIncome: +${def.IncomePerSecLvl2}/s\nPop: +{def.PopulationGainLvl2}";
                UpgradeButton.IsEnabled = _money >= def.UpgradeCost;
                UpgradeButton.Content = "Upgrade";
            }

            UpgradePanel.Visibility = Visibility.Visible;
            PlayUpgradePanelShowAnimation();
        }

        private void HideUpgradePanel()
        {
            _upgradeLotIndex = -1;
            UpgradePanel.Visibility = Visibility.Collapsed;
            UpgradePanel.Opacity = 0;
            UpgradePanelScale.ScaleX = 0.82;
            UpgradePanelScale.ScaleY = 0.82;
        }

        private void PlayUpgradePanelShowAnimation()
        {
            UpgradePanel.Opacity = 0;
            UpgradePanelScale.ScaleX = 0.82;
            UpgradePanelScale.ScaleY = 0.82;

            var fade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(160));

            var scaleX = new DoubleAnimation(0.82, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut }
            };

            var scaleY = new DoubleAnimation(0.82, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new BackEase { Amplitude = 0.45, EasingMode = EasingMode.EaseOut }
            };

            UpgradePanel.BeginAnimation(UIElement.OpacityProperty, fade);
            UpgradePanelScale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleX);
            UpgradePanelScale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleY);
        }

        private void UpgradeClose_Click(object sender, RoutedEventArgs e)
        {
            HideUpgradePanel();
        }

        private void UpgradeConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_upgradeLotIndex < 0) return;
            var lot = _lots[_upgradeLotIndex];
            if (!_defs.TryGetValue(lot.BuildingKey, out var def)) return;

            if (lot.Level >= 2)
            {
                StatusText.Text = "Clădirea este deja la nivel maxim.";
                return;
            }

            if (_money < def.UpgradeCost)
            {
                StatusText.Text = "Fonduri insuficiente pentru upgrade.";
                return;
            }

            _money -= def.UpgradeCost;
            _population -= lot.PopulationGain;

            lot.Level = 2;
            lot.IncomePerSec = def.IncomePerSecLvl2;
            lot.PopulationGain = def.PopulationGainLvl2;
            lot.Sprite = def.SpriteLvl2;
            _population += lot.PopulationGain;

            RenderLot(_upgradeLotIndex);
            RefreshHud();
            StatusText.Text = $"{lot.BuildingName} upgraded la LVL2.";
            HideUpgradePanel();
        }

        private void DemolishConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_upgradeLotIndex < 0) return;
            var lot = _lots[_upgradeLotIndex];
            if (lot.IsEmpty) return;

            var result = MessageBox.Show(
                $"Delete {lot.BuildingName} from lot {_upgradeLotIndex + 1}?",
                "Confirm delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            _population -= lot.PopulationGain;
            if (_population < 0) _population = 0;

            _lots[_upgradeLotIndex] = new LotState();
            RenderLot(_upgradeLotIndex);
            RefreshHud();
            StatusText.Text = $"Clădire ștearsă de pe lotul {_upgradeLotIndex + 1}.";
            HideUpgradePanel();
        }

        private int GetLotIndex(Border lotControl)
        {
            if (lotControl.Tag is int i) return i;
            return int.Parse(lotControl.Tag.ToString() ?? "0");
        }

        private void PlaceBuilding(int lotIndex, string buildingKey)
        {
            if (!_defs.TryGetValue(buildingKey, out var def)) return;

            var lot = _lots[lotIndex];
            if (!lot.IsEmpty)
            {
                StatusText.Text = $"Lot {lotIndex + 1} e ocupat deja.";
                return;
            }

            if (_money < def.Cost)
            {
                StatusText.Text = $"Fonduri insuficiente pentru {def.Name}.";
                return;
            }

            _money -= def.Cost;
            lot.BuildingKey = buildingKey;
            lot.BuildingName = def.Name;
            lot.Level = 1;
            lot.IncomePerSec = def.IncomePerSec;
            lot.PopulationGain = def.PopulationGain;
            lot.Sprite = def.Sprite;
            _population += def.PopulationGain;

            RenderLot(lotIndex);
            RefreshHud();
            StatusText.Text = $"Ai plasat {def.Name} pe lotul {lotIndex + 1}.";
        }

        private void RenderLot(int idx)
        {
            var lotState = _lots[idx];
            var lotControl = _lotControls[idx];

            if (lotState.IsEmpty)
            {
                lotControl.Child = new TextBlock
                {
                    Text = $"Lot {idx + 1}",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    FontWeight = FontWeights.Bold
                };
                lotControl.Cursor = App.NormalCursor;
                return;
            }

            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            panel.Children.Add(new Image
            {
                Source = LoadSprite(lotState.Sprite),
                Width = 122,
                Height = 96,
                HorizontalAlignment = HorizontalAlignment.Center
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"{lotState.BuildingName} LVL{lotState.Level}",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontWeight = FontWeights.Bold,
                FontSize = 11
            });
            panel.Children.Add(new TextBlock
            {
                Text = $"+${lotState.IncomePerSec}/s",
                HorizontalAlignment = HorizontalAlignment.Center,
                FontSize = 10
            });

            lotControl.Child = panel;
            lotControl.Background = new SolidColorBrush(Color.FromRgb(187, 247, 208));
            lotControl.Cursor = App.HoverCursor;
        }

        private ImageSource LoadSprite(string relativeSpritePath)
        {
            var cleaned = relativeSpritePath.TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var fullPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cleaned);
            if (!File.Exists(fullPath))
                return new BitmapImage();

            return new BitmapImage(new Uri(fullPath, UriKind.Absolute));
        }

        private void TryLoadCurrentSlot()
        {
            var data = SaveSystem.LoadSlot(App.currentSlot);
            if (data == null) return;

            _money = data.Money;
            _population = data.Population;
            App.saveName = data.SaveName;
            SaveNameText.Text = $"Save: {App.saveName}";

            for (int i = 0; i < _lots.Count; i++)
            {
                _lots[i] = new LotState();
                _lotControls[i].Background = new SolidColorBrush(Color.FromRgb(253, 230, 138));
            }

            for (int i = 0; i < data.Lots.Count && i < _lots.Count; i++)
            {
                var savedLot = data.Lots[i];
                _lots[i] = new LotState
                {
                    BuildingKey = string.IsNullOrWhiteSpace(savedLot.BuildingKey) ? savedLot.BuildingName : savedLot.BuildingKey,
                    BuildingName = savedLot.BuildingName,
                    Level = savedLot.Level <= 0 ? 1 : savedLot.Level,
                    IncomePerSec = savedLot.IncomePerSec,
                    PopulationGain = savedLot.PopulationGain,
                    Sprite = savedLot.Sprite
                };
            }

            for (int i = 0; i < _lots.Count; i++)
                RenderLot(i);

            StatusText.Text = $"Loaded slot {App.currentSlot}.";
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var data = new GameSaveData
            {
                SaveName = string.IsNullOrWhiteSpace(App.saveName) ? $"Slot {App.currentSlot}" : App.saveName,
                Money = _money,
                Population = _population,
                Lots = _lots.Select(x => new LotSaveData
                {
                    BuildingKey = x.BuildingKey,
                    BuildingName = x.BuildingName,
                    Level = x.Level,
                    IncomePerSec = x.IncomePerSec,
                    PopulationGain = x.PopulationGain,
                    Sprite = x.Sprite
                }).ToList()
            };

            SaveSystem.SaveSlot(App.currentSlot, data);
            StatusText.Text = $"Saved in slot {App.currentSlot}.";
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            _parentWindow.NavigateTo(new Settings(_parentWindow, this));
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            _money = 250;
            _population = 0;
            for (int i = 0; i < _lots.Count; i++)
            {
                _lots[i] = new LotState();
                _lotControls[i].Background = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                RenderLot(i);
            }
            RefreshHud();
            HideUpgradePanel();
            StatusText.Text = "Run resetat.";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _gameTimer.Stop();
            _parentWindow.GoBack();
        }

        private sealed class BuildingDef
        {
            public string Name { get; }
            public decimal Cost { get; }
            public decimal IncomePerSec { get; }
            public int PopulationGain { get; }
            public string Sprite { get; }
            public decimal UpgradeCost { get; }
            public decimal IncomePerSecLvl2 { get; }
            public int PopulationGainLvl2 { get; }
            public string SpriteLvl2 { get; }

            public BuildingDef(string name, decimal cost, decimal incomePerSec, int populationGain, string sprite,
                decimal upgradeCost, decimal incomePerSecLvl2, int populationGainLvl2, string spriteLvl2)
            {
                Name = name;
                Cost = cost;
                IncomePerSec = incomePerSec;
                PopulationGain = populationGain;
                Sprite = sprite;
                UpgradeCost = upgradeCost;
                IncomePerSecLvl2 = incomePerSecLvl2;
                PopulationGainLvl2 = populationGainLvl2;
                SpriteLvl2 = spriteLvl2;
            }
        }

        private sealed class LotState
        {
            public string BuildingKey { get; set; } = string.Empty;
            public string BuildingName { get; set; } = string.Empty;
            public int Level { get; set; } = 1;
            public decimal IncomePerSec { get; set; }
            public int PopulationGain { get; set; }
            public string Sprite { get; set; } = string.Empty;

            public bool IsEmpty => string.IsNullOrWhiteSpace(BuildingName);
        }
    }
}
