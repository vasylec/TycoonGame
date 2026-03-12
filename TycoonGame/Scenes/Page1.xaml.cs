using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TycoonGame.Animations;
using TycoonGame.Helpers;
using TycoonGame.Scripts;

namespace TycoonGame.Scenes
{
    public partial class Page1 : Page
    {
        private readonly MainMenu _parentWindow;
        private readonly DispatcherTimer _gameTimer;
        private readonly Random _rng = new();
        private decimal _incomeMultiplier = 1m;
        private int _eventTicksRemaining;
        private int _ticksUntilNextEvent;
        private readonly DispatcherTimer _eventPopupTimer = new() { Interval = TimeSpan.FromSeconds(2.2) };

        private decimal _money = 300;
        private int _population = 0;

        private readonly List<LotState> _lots = new();
        private readonly List<Border> _lotControls = new();

        private readonly Dictionary<string, BuildingDef> _defs = new()
        {
            ["House"] = new BuildingDef(
                "House", 120, 3, 4, "/Assets/Buildings/BuildingBeige.png",
                360, 5, 6, "/Assets/Buildings/BuildingBeige_LVL2.png",
                900, 8, 8, "/Assets/Buildings/BuildingBeige_LVL3.png"),
            ["Shop"] = new BuildingDef(
                "Shop", 260, 5, 2, "/Assets/Buildings/BuildingBlue.png",
                520, 8, 3, "/Assets/Buildings/BuildingBlue_LVL2.png",
                1100, 12, 4, "/Assets/Buildings/BuildingBlue_LVL3.png"),
            ["Factory"] = new BuildingDef(
                "Factory", 380, 7, 1, "/Assets/Buildings/BuildingWhite.png",
                760, 11, 2, "/Assets/Buildings/BuildingWhite_LVL2.png",
                1500, 16, 3, "/Assets/Buildings/BuildingWhite_LVL3.png")
        };

        private string? _selectedBuilding;

        private bool _isDragging;
        private string? _dragBuilding;
        private Point _dragStart;
        private Point _lastDragMousePos;
        private double _currentDragAngle;
        private int _upgradeLotIndex = -1;

        private WaterTiles_Animate waterTilesGrid;
        private DrawingVisual waterVisual = new DrawingVisual();
        private int tileSize = 80;
        private BitmapSource[] waterFrames;
        private Stopwatch stopwatch = new Stopwatch();
        private double totalTime = 0;
        private double animationSpeed = 4; // frames per second
        private int gridWidth;
        private int gridHeight;

        public Page1(MainMenu parent)
        {
            InitializeComponent();
            _parentWindow = parent;

            SaveNameText.Text = $"Save: {(string.IsNullOrWhiteSpace(App.saveName) ? "NoName" : App.saveName)}";

            // Lots on the island (must match XAML names + Tag indexes)
            _lotControls.AddRange(new[]
            {
                Lot1, Lot2, Lot3, Lot4, Lot5, Lot6,
                Lot6_Copy, Lot6_Copy1, Lot6_Copy2, Lot6_Copy3, Lot6_Copy4, Lot6_Copy5, Lot6_Copy6
            });
            for (int i = 0; i < _lotControls.Count; i++)
                _lots.Add(new LotState());

            _ticksUntilNextEvent = 12;

            _eventPopupTimer.Tick += (_, _) => HideEventPopup();

            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _gameTimer.Tick += (_, _) => TickGame();
            _gameTimer.Start();

            TryLoadCurrentSlot();

            UIHelper.ApplyPixelFontAndSettings(this);
            RefreshHud();

            // Force map from code (same loading logic as building sprites)
            // IMPORTANT: map must be above water, so we set it on MapImage (not canvas background)
            try
            {
                MapImage.Source = LoadSprite("/Assets/Map/Map.png");
                MapImage.Stretch = Stretch.Fill;
            }
            catch
            {
                // ignore
            }

            Loaded += Game_Loaded; // wait until window is shown
            WaterCanvas.Children.Clear(); // remove old stuff+
            var host = new VisualHost();
            host.AddVisual(waterVisual);
            WaterCanvas.Children.Add(host);
        }

        private void TickGame()
        {
            if (_eventTicksRemaining > 0)
            {
                _eventTicksRemaining--;
                if (_eventTicksRemaining == 0)
                {
                    _incomeMultiplier = 1m;
                    StatusText.Text = "Event ended. Income back to normal.";
                }
            }

            var baseIncome = _lots.Sum(x => x.IncomePerSec);
            var tickIncome = decimal.Round(baseIncome * _incomeMultiplier, 2);
            _money += tickIncome;

            _ticksUntilNextEvent--;
            if (_ticksUntilNextEvent <= 0)
                TriggerRandomEvent();

            RefreshHud();
        }

        private void RefreshHud()
        {
            MoneyText.Text = $"${_money:0}";
            var displayIncome = decimal.Round(_lots.Sum(x => x.IncomePerSec) * _incomeMultiplier, 2);
            IncomeText.Text = _incomeMultiplier == 1m
                ? $"Income/s: ${displayIncome:0.##}"
                : $"Income/s: ${displayIncome:0.##} (x{_incomeMultiplier:0.##})";
            PopulationText.Text = $"Pop: {_population}";
            UpdateUpgradePanelIfOpen();
        }

        private void ShowEventPopup(string message, bool positive = false)
        {
            EventPopupText.Text = message;
            EventPopup.Background = positive
                ? new SolidColorBrush(Color.FromRgb(220, 252, 231))
                : new SolidColorBrush(Color.FromRgb(254, 226, 226));
            EventPopup.BorderBrush = positive
                ? new SolidColorBrush(Color.FromRgb(21, 128, 61))
                : new SolidColorBrush(Color.FromRgb(127, 29, 29));
            EventPopupText.Foreground = positive
                ? new SolidColorBrush(Color.FromRgb(21, 128, 61))
                : new SolidColorBrush(Color.FromRgb(127, 29, 29));

            EventPopup.BeginAnimation(UIElement.OpacityProperty, null);
            EventPopupScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            EventPopupScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            EventPopupTranslate.BeginAnimation(TranslateTransform.YProperty, null);

            EventPopup.Visibility = Visibility.Visible;
            EventPopup.Opacity = 0;
            EventPopupScale.ScaleX = 0.85;
            EventPopupScale.ScaleY = 0.85;
            EventPopupTranslate.Y = -220;

            EventPopup.BeginAnimation(UIElement.OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220)));

            var dropIn = new DoubleAnimationUsingKeyFrames();
            dropIn.KeyFrames.Add(new EasingDoubleKeyFrame(-220, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            dropIn.KeyFrames.Add(new EasingDoubleKeyFrame(18, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(230)))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
            dropIn.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(340)))
            {
                EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut }
            });
            EventPopupTranslate.BeginAnimation(TranslateTransform.YProperty, dropIn);

            var pulseX = new DoubleAnimationUsingKeyFrames();
            pulseX.KeyFrames.Add(new EasingDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            pulseX.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            pulseX.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(360))));
            EventPopupScale.BeginAnimation(ScaleTransform.ScaleXProperty, pulseX);

            var pulseY = new DoubleAnimationUsingKeyFrames();
            pulseY.KeyFrames.Add(new EasingDoubleKeyFrame(0.85, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(0))));
            pulseY.KeyFrames.Add(new EasingDoubleKeyFrame(1.06, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(240))));
            pulseY.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(360))));
            EventPopupScale.BeginAnimation(ScaleTransform.ScaleYProperty, pulseY);

            _eventPopupTimer.Stop();
            _eventPopupTimer.Start();
        }

        private void HideEventPopup()
        {
            _eventPopupTimer.Stop();

            var fadeOut = new DoubleAnimation(0, TimeSpan.FromMilliseconds(220));
            fadeOut.Completed += (_, _) =>
            {
                EventPopup.Visibility = Visibility.Collapsed;
            };
            EventPopup.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private int RollNextEventTicks()
        {
            return _rng.Next(20, 41);
        }

        private void TriggerRandomEvent()
        {
            _ticksUntilNextEvent = RollNextEventTicks();

            var roll = _rng.Next(0, 3);
            switch (roll)
            {
                case 0:
                    _incomeMultiplier = 0.70m;
                    _eventTicksRemaining = 25;
                    StatusText.Text = "? Power outage: income -30% for 25s.";
                    ShowEventPopup("? POWER OUTAGE! Income -30% for 25 seconds.");
                    break;

                case 1:
                    _incomeMultiplier = 1.35m;
                    _eventTicksRemaining = 15;
                    StatusText.Text = "?? Tourist boom: income +35% for 15s.";
                    ShowEventPopup("?? TOURIST BOOM! Income +35% for 15 seconds.", positive: true);
                    break;

                default:
                    var fee = Math.Max(25m, decimal.Round(_money * 0.12m, 0));
                    _money = Math.Max(0m, _money - fee);
                    _incomeMultiplier = 1m;
                    _eventTicksRemaining = 0;
                    StatusText.Text = $"?? Maintenance fee: -${fee:0}.";
                    ShowEventPopup($"?? MAINTENANCE FEE! You paid ${fee:0}.");
                    break;
            }
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

            //if (_selectedBuilding is null)
            //{
            //    StatusText.Text = "Selectează întâi o clădire din stânga.";
            //    return;
            //}

            //PlaceBuilding(idx, _selectedBuilding);
        }

        private void ShowUpgradePanel(int lotIndex, Border lotControl)
        {
            if (lotIndex == _upgradeLotIndex)
                return;

            var lot = _lots[lotIndex];
            _upgradeLotIndex = lotIndex;

            // Ensure the popup is above lots/buildings
            Panel.SetZIndex(UpgradePanel, 10000);

            Canvas.SetLeft(UpgradePanel, Canvas.GetLeft(lotControl) + lotControl.Width + 10);
            Canvas.SetTop(UpgradePanel, Canvas.GetTop(lotControl));

            if (!_defs.TryGetValue(lot.BuildingKey, out var def))
            {
                UpgradePanel.Visibility = Visibility.Collapsed;
                return;
            }

            if (lot.Level >= 3)
            {
                UpgradePreviewImage.Source = LoadSprite(lot.Sprite);
                UpgradeStatsText.Text = $"{lot.BuildingName} este deja la nivel maxim (LVL3).\nIncome: +${lot.IncomePerSec}/s\nPop: +{lot.PopulationGain}";
                UpgradeButton.IsEnabled = false;
                UpgradeButton.Content = "MAX";
            }
            else
            {
                var nextLevel = lot.Level + 1;
                var nextSprite = nextLevel == 2 ? def.SpriteLvl2 : def.SpriteLvl3;
                UpgradePreviewImage.Source = LoadSprite(nextSprite);
                UpgradeStatsText.Text = BuildUpgradeStatsText(lot, def);
                UpgradeButton.IsEnabled = _money >= GetUpgradeCostForNextLevel(lot, def);
                UpgradeButton.Content = "Upgrade";
            }

            UpgradePanel.Visibility = Visibility.Visible;
            PlayUpgradePanelShowAnimation();
        }

        private void UpdateUpgradePanelIfOpen()
        {
            if (_upgradeLotIndex < 0 || UpgradePanel.Visibility != Visibility.Visible)
                return;

            var lot = _lots[_upgradeLotIndex];
            if (!_defs.TryGetValue(lot.BuildingKey, out var def))
                return;

            if (lot.Level >= 3)
            {
                UpgradeButton.IsEnabled = false;
                UpgradeButton.Content = "MAX";
                return;
            }

            UpgradeStatsText.Text = BuildUpgradeStatsText(lot, def);
            UpgradeButton.IsEnabled = _money >= GetUpgradeCostForNextLevel(lot, def);
            UpgradeButton.Content = "Upgrade";
        }

        private string BuildUpgradeStatsText(LotState lot, BuildingDef def)
        {
            var nextLevel = lot.Level + 1;
            var nextCost = GetUpgradeCostForNextLevel(lot, def);
            var nextIncome = nextLevel == 2 ? def.IncomePerSecLvl2 : def.IncomePerSecLvl3;
            var nextPop = nextLevel == 2 ? def.PopulationGainLvl2 : def.PopulationGainLvl3;
            var etaText = GetUpgradeEtaText(nextCost);
            return $"Upgrade cost: ${nextCost}\nAfter upgrade (LVL{nextLevel}):\nIncome: +${nextIncome}/s\nPop: +{nextPop}\nTime to afford: {etaText}";
        }

        private decimal GetUpgradeCostForNextLevel(LotState lot, BuildingDef def)
        {
            return lot.Level switch
            {
                1 => def.UpgradeCost,
                2 => def.UpgradeCostLvl3,
                _ => 0
            };
        }

        private string GetUpgradeEtaText(decimal targetCost)
        {
            if (_money >= targetCost)
                return "now";

            var incomePerSec = _lots.Sum(x => x.IncomePerSec);
            if (incomePerSec <= 0)
                return "never (no income)";

            var missing = targetCost - _money;
            var totalSeconds = (int)Math.Ceiling((double)(missing / incomePerSec));
            var ts = TimeSpan.FromSeconds(totalSeconds);

            if (ts.TotalHours >= 1)
                return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";

            return $"{ts.Seconds}s";
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

            if (lot.Level >= 3)
            {
                StatusText.Text = "Clădirea este deja la nivel maxim.";
                return;
            }

            var upgradeCost = GetUpgradeCostForNextLevel(lot, def);
            if (_money < upgradeCost)
            {
                StatusText.Text = "Fonduri insuficiente pentru upgrade.";
                return;
            }

            _money -= upgradeCost;
            _population -= lot.PopulationGain;

            if (lot.Level == 1)
            {
                lot.Level = 2;
                lot.IncomePerSec = def.IncomePerSecLvl2;
                lot.PopulationGain = def.PopulationGainLvl2;
                lot.Sprite = def.SpriteLvl2;
            }
            else
            {
                lot.Level = 3;
                lot.IncomePerSec = def.IncomePerSecLvl3;
                lot.PopulationGain = def.PopulationGainLvl3;
                lot.Sprite = def.SpriteLvl3;
            }

            _population += lot.PopulationGain;

            RenderLot(_upgradeLotIndex);
            RefreshHud();
            StatusText.Text = $"{lot.BuildingName} upgraded la LVL{lot.Level}.";
            HideUpgradePanel();
        }

        private void DemolishConfirm_Click(object sender, RoutedEventArgs e)
        {
            if (_upgradeLotIndex < 0) return;
            var lot = _lots[_upgradeLotIndex];
            if (lot.IsEmpty) return;


            decimal refund = 0;

            if (_defs.TryGetValue(lot.BuildingKey, out var def))
            {
                decimal totalInvested = def.Cost;

                if (lot.Level >= 2)
                    totalInvested += def.UpgradeCost;

                if (lot.Level >= 3)
                    totalInvested += def.UpgradeCostLvl3;

                refund = decimal.Round(totalInvested * 0.70m, 0); // 70%

                //_money += refund;

                StatusText.Text = $"Clădire ștearsă de pe lotul {_upgradeLotIndex + 1}. Refund: ${refund:0}.";
            }
            else
            {
                StatusText.Text = $"Clădire ștearsă de pe lotul {_upgradeLotIndex + 1}.";
            }



            var result = MessageBox.Show(
                $"Sell {lot.BuildingName} from lot {_upgradeLotIndex + 1} for ${refund:0}$ ?",
                "Confirm sell",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.No)
            {
                return;
            }

            _money += refund;



            



            _population -= lot.PopulationGain;
            if (_population < 0) _population = 0;

            _lots[_upgradeLotIndex] = new LotState();
            RenderLot(_upgradeLotIndex);
            RefreshHud();
            StatusText.Text = $"Clădire vândută de pe lotul {_upgradeLotIndex + 1}.";

            


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

            var panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            // Fit sprite inside the lot (avoid cropping)
            var slotW = (lotControl.ActualWidth > 0 ? lotControl.ActualWidth : lotControl.Width);
            var slotH = (lotControl.ActualHeight > 0 ? lotControl.ActualHeight : lotControl.Height);

            // Leave room for text below
            var imgMaxW = Math.Max(10, slotW - 10);
            var imgMaxH = Math.Max(10, slotH - 28);

            var spriteImg = new Image
            {
                Source = LoadSprite(lotState.Sprite),
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                SnapsToDevicePixels = true
            };
            RenderOptions.SetBitmapScalingMode(spriteImg, BitmapScalingMode.NearestNeighbor);

            spriteImg.Width = imgMaxW;
            spriteImg.Height = imgMaxH;

            panel.Children.Add(spriteImg);
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
                var key = string.IsNullOrWhiteSpace(savedLot.BuildingKey) ? savedLot.BuildingName : savedLot.BuildingKey;
                var lvl = savedLot.Level <= 0 ? 1 : savedLot.Level;
                _defs.TryGetValue(key, out var loadedDef);
                var fallbackSprite = lvl switch
                {
                    3 => loadedDef?.SpriteLvl3 ?? loadedDef?.SpriteLvl2 ?? loadedDef?.Sprite ?? string.Empty,
                    2 => loadedDef?.SpriteLvl2 ?? loadedDef?.Sprite ?? string.Empty,
                    _ => loadedDef?.Sprite ?? string.Empty
                };

                _lots[i] = new LotState
                {
                    BuildingKey = key,
                    BuildingName = savedLot.BuildingName,
                    Level = lvl,
                    IncomePerSec = savedLot.IncomePerSec,
                    PopulationGain = savedLot.PopulationGain,
                    Sprite = string.IsNullOrWhiteSpace(savedLot.Sprite) ? fallbackSprite : savedLot.Sprite
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
            _money = 300;
            _population = 0;
            _incomeMultiplier = 1m;
            _eventTicksRemaining = 0;
            _ticksUntilNextEvent = 12;
            for (int i = 0; i < _lots.Count; i++)
            {
                _lots[i] = new LotState();
                _lotControls[i].Background = new SolidColorBrush(Color.FromRgb(253, 230, 138));
                RenderLot(i);
            }
            RefreshHud();
            HideUpgradePanel();
            HideEventPopup();
            StatusText.Text = "Run resetat.";
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            _gameTimer.Stop();
            _eventPopupTimer.Stop();
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
            public decimal UpgradeCostLvl3 { get; }
            public decimal IncomePerSecLvl3 { get; }
            public int PopulationGainLvl3 { get; }
            public string SpriteLvl3 { get; }

            public BuildingDef(string name, decimal cost, decimal incomePerSec, int populationGain, string sprite,
                decimal upgradeCost, decimal incomePerSecLvl2, int populationGainLvl2, string spriteLvl2,
                decimal upgradeCostLvl3, decimal incomePerSecLvl3, int populationGainLvl3, string spriteLvl3)
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
                UpgradeCostLvl3 = upgradeCostLvl3;
                IncomePerSecLvl3 = incomePerSecLvl3;
                PopulationGainLvl3 = populationGainLvl3;
                SpriteLvl3 = spriteLvl3;
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

        

        private void Lot_click_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            _money += 1;
            RefreshHud();
        }

        private void Lot_click_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = App.HoverCursor; 
            }
        }

        private void Lot_click_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Border border)
            {
                border.Cursor = App.NormalCursor; // sau App.NormalCursor
            }
        }

        private void Border_MouseDown_1(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left)
                return;

            var hit = e.OriginalSource as DependencyObject;
            if (FindParentLot(hit) != null)
                return; // click pe lot → nu ascundem upgrade panel

            HideUpgradePanel();
        }
        private void Game_Loaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering += GameLoop;
            stopwatch.Start();
            LoadWaterFrames();              // load sprite sheet ONCE
            InitializeWaterTiles(16, 9);   // fill screen
        }

        // 🔹 Slice sprite sheet into animation frames
        private void LoadWaterFrames()
        {

            

            BitmapImage spriteSheet = new BitmapImage(
                new Uri("pack://application:,,,/Assets/Textures/Water.png"));

            int frameCount = spriteSheet.PixelWidth / tileSize;
            waterFrames = new BitmapSource[frameCount];

            for (int f = 0; f < frameCount; f++)
            {
                waterFrames[f] = new CroppedBitmap(
                    spriteSheet,
                    new Int32Rect(f * tileSize, 0, tileSize, tileSize));
            }
        }

        // 🔹 Create grid of water tiles
        private void InitializeWaterTiles(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;

            waterTilesGrid = new WaterTiles_Animate(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var tile = new WaterTiles(waterFrames);

                    // Offset animation phase per tile
                    tile.CurrentFrame = (x + y) % waterFrames.Length;

                    waterTilesGrid.Tiles[x, y] = tile;
                }
            }

            WaterCanvas.IsHitTestVisible = false; // still good
        }

        // 🔹 Animation loop
        private void GameLoop(object sender, EventArgs e)
        {
            double deltaTime = stopwatch.Elapsed.TotalSeconds;
            stopwatch.Restart();
            totalTime += deltaTime;

            int baseFrame = (int)(totalTime * animationSpeed);

            using (DrawingContext dc = waterVisual.RenderOpen())
            {
                // Water tiles are full-frame and opaque; make them translucent so the map stays visible.
                dc.PushOpacity(0.35);

                for (int x = 0; x < 16; x++)
                {
                    for (int y = 0; y < 9; y++)
                    {
                        int frameIndex = (baseFrame + x + y) % waterFrames.Length;

                        dc.DrawImage(
                            waterFrames[frameIndex],
                            new Rect(x * tileSize, y * tileSize, tileSize, tileSize));
                    }
                }

                dc.Pop();
            }
        }
    }
}









