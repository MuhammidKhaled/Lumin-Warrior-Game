using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Shapes;

using LuminWarrior.Effects;

namespace LuminWarrior
{
    public class UIManager
    {
        private const double MENU_SHIP_SIZE = 200;
        private const double FLAME_WIDTH = 30;
        private const double FLAME_HEIGHT = 100;
        private const double SHIP_CONTAINER_SIZE = 300;
        private static Style? _roundedButtonStyle;

        private Canvas gameCanvas;
        private BackgroundManager backgroundManager;
        private Grid? mainMenuGrid;
        private Window? helpWindow;
        private Label scoreText;
        private Label levelText;
        private Image star1, star2, star3;
        private Image star1Empty, star2Empty, star3Empty;
        private Image coinImage;
        private StackPanel scorePanel, levelPanel, starsPanel;
        private DispatcherTimer? backgroundAnimationTimer;
        private Rectangle? menuPlayerShip;
        private Image? menuLeftFlame, menuRightFlame;
        private double menuFlameOffsetY = 0;
        private bool menuMovingUp = true;
        private DispatcherTimer? menuPlayerAnimationTimer;

        private static Style RoundedButtonStyle => _roundedButtonStyle ??= CreateRoundedButtonStyle();

        public event Action? OnPlayClicked;
        public event Action? OnFreeFlyClicked;
        public event Action? OnQuitClicked;
        public event Action? OnHelpClicked;

        public UIManager(Canvas canvas, Label scoreLabel, Label levelLabel,
                Image star1Img, Image star2Img, Image star3Img,
                Image star1EmptyImg, Image star2EmptyImg, Image star3EmptyImg,
                Image coinImg, StackPanel scoreStackPanel, StackPanel levelStackPanel, StackPanel starsStackPanel)
        {
            gameCanvas = canvas;
            backgroundManager = new BackgroundManager(gameCanvas);

            scoreText = scoreLabel;
            levelText = levelLabel;
            star1 = star1Img;
            star2 = star2Img;
            star3 = star3Img;
            star1Empty = star1EmptyImg;
            star2Empty = star2EmptyImg;
            star3Empty = star3EmptyImg;
            coinImage = coinImg;
            scorePanel = scoreStackPanel;
            levelPanel = levelStackPanel;
            starsPanel = starsStackPanel;
        }

        private void HideOriginalUI()
        {
            //Stack panels at the far top left (Score,Levels,Try again stars)
            if (scorePanel != null)
            {
                scorePanel.Visibility = Visibility.Hidden;
            }
            if (levelPanel != null)
            {
                levelPanel.Visibility = Visibility.Hidden;
            }
            if (starsPanel != null)
            {
                starsPanel.Visibility = Visibility.Hidden;
            }
        }

        public void ShowOriginalUI()
        {
            //Stack panels at the far top left (Score,Levels,Try again stars)
            if (scorePanel != null)
            {
                scorePanel.Visibility = Visibility.Visible;
            }
            if (levelPanel != null)
            {
                levelPanel.Visibility = Visibility.Visible;
            }
            if (starsPanel != null)
            {
                starsPanel.Visibility = Visibility.Visible;
            }
        }

        public void ShowMenu()
        {
            HideOriginalUI();

            HideMenu();

            CreateMainMenu();

            gameCanvas.Children.Add(mainMenuGrid);

            StartBackgroundAnimation();

            SoundEffectsManager.PlayUIMusic();
        }

        public void HideMenu()
        {
            if (mainMenuGrid != null)
            {
                if (gameCanvas.Children.Contains(mainMenuGrid))
                {
                    gameCanvas.Children.Remove(mainMenuGrid);
                }
                mainMenuGrid = null;
            }

            ShowOriginalUI(); 

            SoundEffectsManager.StopUIMusic();
        }

        private void CreateMainMenu()
        {
            double canvasWidth = gameCanvas.ActualWidth > 0 ? gameCanvas.ActualWidth : SystemParameters.PrimaryScreenWidth;
            double canvasHeight = gameCanvas.ActualHeight > 0 ? gameCanvas.ActualHeight : SystemParameters.PrimaryScreenHeight;

            mainMenuGrid = new Grid
            {
                Width = canvasWidth,
                Height = canvasHeight,
                Background = Brushes.Transparent
            };

            Canvas.SetLeft(mainMenuGrid, 0);
            Canvas.SetTop(mainMenuGrid, 0);
            Canvas.SetZIndex(mainMenuGrid, 1000);

            StackPanel mainContent = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, -100, 0, 0) 
            };

            TextBlock gameTitle = new TextBlock
            {
                Text = "LUMIN WARRIOR",
                FontSize = 90,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Aqua,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 18, 0, 3),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 5,
                    Opacity = 0.8,
                    BlurRadius = 10
                }
            };

            TextBlock gameSlogan = new TextBlock
            {
                Text = "Fight for the light",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 18),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 3,
                    Opacity = 0.6,
                    BlurRadius = 5
                }
            };

            menuPlayerShip = new Rectangle
            {
                Width = MENU_SHIP_SIZE,
                Height = MENU_SHIP_SIZE,
                Fill = new ImageBrush
                {
                    ImageSource = new BitmapImage(new Uri("pack://application:,,,/assets/images/player.png"))
                },
                Effect = new DropShadowEffect
                {
                    Color = Colors.White,
                    BlurRadius = 20,
                    ShadowDepth = 0,
                    Opacity = 0.3
                },
                Margin = new Thickness(0, 0, 0, 9)
            };

            menuLeftFlame = new Image
            {
                Width = FLAME_WIDTH,
                Height = FLAME_HEIGHT,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/aquaflame.png")),
                Opacity = 0.8,
            };

            menuRightFlame = new Image
            {
                Width = FLAME_WIDTH,
                Height = FLAME_HEIGHT,
                Source = new BitmapImage(new Uri("pack://application:,,,/assets/images/aquaflame.png")),
                Opacity = 0.8,
            };

            Grid shipContainer = new Grid
            {
                Width = SHIP_CONTAINER_SIZE,
                Height = SHIP_CONTAINER_SIZE,
                Margin = new Thickness(0, 3, 0, 18)
            };

            shipContainer.Children.Add(menuLeftFlame);
            shipContainer.Children.Add(menuRightFlame);
            shipContainer.Children.Add(menuPlayerShip);

            Grid buttonGrid = new Grid
            {
                Width = 360,
                Height = 150
            };

            buttonGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            buttonGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Button playButton = CreateMenuButton("Play");
            Button freeFlyButton = CreateMenuButton("Free Fly");
            Button quitButton = CreateMenuButton("Quit");
            Button helpButton = CreateMenuButton("Help");

            playButton.Click += (s, e) =>
            {
                SoundEffectsManager.PlayButtonClickSound();
                OnPlayClicked?.Invoke();
            };
            freeFlyButton.Click += (s, e) =>
            {
                SoundEffectsManager.PlayButtonClickSound();
                OnFreeFlyClicked?.Invoke();
            };
            quitButton.Click += (s, e) =>
            {
                SoundEffectsManager.PlayButtonClickSound();
                OnQuitClicked?.Invoke();
            };
            helpButton.Click += (s, e) =>
            {
                SoundEffectsManager.PlayButtonClickSound();
                ShowHelpWindow();
            };

            Grid.SetRow(playButton, 0);
            Grid.SetColumn(playButton, 0);

            Grid.SetRow(freeFlyButton, 0);
            Grid.SetColumn(freeFlyButton, 1);

            Grid.SetRow(helpButton, 1);
            Grid.SetColumn(helpButton, 0);

            Grid.SetRow(quitButton, 1);
            Grid.SetColumn(quitButton, 1);

            buttonGrid.Children.Add(playButton);
            buttonGrid.Children.Add(freeFlyButton);
            buttonGrid.Children.Add(quitButton);
            buttonGrid.Children.Add(helpButton);

            mainContent.Children.Add(gameTitle);
            mainContent.Children.Add(gameSlogan);
            mainContent.Children.Add(shipContainer);
            mainContent.Children.Add(buttonGrid);

            StartMenuPlayerAnimation();

            mainMenuGrid.Children.Add(mainContent);
        }

        private Button CreateMenuButton(string text)
        {
            Button button = new Button
            {
                Content = text,
                Width = 150,
                Height = 50,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Aqua,
                BorderBrush = Brushes.DarkCyan,
                BorderThickness = new Thickness(2),
                Margin = new Thickness(10),
                Effect = new DropShadowEffect
                {
                    Color = Colors.Black,
                    Direction = 320,
                    ShadowDepth = 3,
                    Opacity = 0.5,
                    BlurRadius = 5
                }
            };

            button.Style = CreateRoundedButtonStyle();

            button.MouseEnter += (s, e) =>
            {
                button.Background = Brushes.DarkCyan;
                button.Foreground = Brushes.LightCyan;
            };
            button.MouseLeave += (s, e) =>
            {
                button.Background = Brushes.Aqua;
                button.Foreground = Brushes.White;
            };

            return button;
        }

        private static Style CreateRoundedButtonStyle()
        {
            Style buttonStyle = new Style(typeof(Button));

            ControlTemplate template = new ControlTemplate(typeof(Button));

            FrameworkElementFactory borderFactory = new FrameworkElementFactory(typeof(Border));
            borderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(15));
            borderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            borderFactory.SetValue(Border.BorderBrushProperty, new TemplateBindingExtension(Button.BorderBrushProperty));
            borderFactory.SetValue(Border.BorderThicknessProperty, new TemplateBindingExtension(Button.BorderThicknessProperty));

            FrameworkElementFactory contentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            contentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            contentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

            borderFactory.AppendChild(contentFactory);
            template.VisualTree = borderFactory;

            buttonStyle.Setters.Add(new Setter(Button.TemplateProperty, template));

            return buttonStyle;
        }

        private void ShowHelpWindow()
        {
            if (helpWindow != null && helpWindow.IsVisible)
            {
                helpWindow.Focus();
                return;
            }

            helpWindow = new Window
            {
                Title = "Help",
                Width = 500,
                Height = 400,
                Background = Brushes.Black,
                BorderBrush = Brushes.Aqua,
                BorderThickness = new Thickness(3),
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow
            };

            ScrollViewer scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(20)
            };

            TextBlock helpContent = new TextBlock
            {
                Text = "GAME CONTROLS AND INSTRUCTIONS:\n\n" +
                       "• Use Arrow Keys to move your spaceship\n" +
                       "• Press 'M' button to switch to mouse control mode\n" +
                       "• Press 'SPACEBAR' to fire rockets\n" +
                       "• Press 'esc' to pause/resume the game or return to main menu\n" +
                       "• Avoid enemy spaceships and obstacles\n" +
                       "• Defeat bosses to progress through levels\n\n" +
                       "POWER-UPS:\n\n" +
                        "• Power-ups provide various combat advantages\n" +
                        "• Shield: Protects against enemy rockets (not collisions)\n" +
                        "• Rockets: Provides extra ammunition and faster fire rate\n" +
                        "• Health Boost: Restores 25% of your health\n\n" +
                       "GAME MODES:\n\n" +
                       "• PLAY MODE:\n" +
                       "    • Complete 6 challenging levels.\n" +
                       "    • Face mini-bosses and final bosses.\n" +
                       "• FREE FLY MODE:\n" +
                       "    • Practice your flying skills in a simplified environment.\n" +
                       "    • No enemies, no health - just you in the space.\n\n" +
                       "Good luck, Warrior!",
                FontSize = 14,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                LineHeight = 20
            };

            scrollViewer.Content = helpContent;
            helpWindow.Content = scrollViewer;

            helpWindow.Show();
            OnHelpClicked?.Invoke();
        }

        private void StartBackgroundAnimation()
        {
            if (backgroundAnimationTimer == null)
            {
                backgroundAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
                backgroundAnimationTimer.Tick += (s, e) => backgroundManager?.Update();
            }
            backgroundAnimationTimer.Start();
        }

        private void UpdateMenuPlayerAnimation(object? sender, EventArgs e)
        {
            if (menuLeftFlame == null || menuRightFlame == null) return;

            if (menuMovingUp)
            {
                menuFlameOffsetY -= 2;
                if (menuFlameOffsetY <= -2.05) menuMovingUp = false;
            }
            else
            {
                menuFlameOffsetY += 2;
                if (menuFlameOffsetY >= 2.05) menuMovingUp = true;
            }

            menuLeftFlame.Margin = new Thickness(-36, 118 + menuFlameOffsetY, 0, 0);
            menuRightFlame.Margin = new Thickness(32, 118 + menuFlameOffsetY, 0, 0);
        }

        private void StartMenuPlayerAnimation()
        {
            if (menuPlayerAnimationTimer == null)
            {
                menuPlayerAnimationTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
                menuPlayerAnimationTimer.Tick += UpdateMenuPlayerAnimation;
            }
            menuPlayerAnimationTimer.Start();
        }

        public void Cleanup()
        {
            HideMenu();
            backgroundAnimationTimer?.Stop();
            menuPlayerAnimationTimer?.Stop();
            backgroundAnimationTimer = null;
            menuPlayerAnimationTimer = null;

            if (helpWindow != null)
            {
                helpWindow.Close();
                helpWindow = null;
            }

            backgroundManager?.ClearStars();
            SoundEffectsManager.StopUIMusic();
        }
    }
}