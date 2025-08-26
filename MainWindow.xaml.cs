using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Shell;

namespace LuminWarrior
{
    public partial class MainWindow : Window
    {
        private GameManager? gameManager;
        private UIManager? uiManager;
        private Label? levelText;
        private Levels? levels;


        public MainWindow()
        {
            InitializeComponent();

            InitializeLevelsText();

            this.Loaded += (s, e) => {
                ScorePanel.Visibility = Visibility.Hidden;
                LevelPanel.Visibility = Visibility.Hidden;
                StarsPanel.Visibility = Visibility.Hidden;

                WindowChrome.SetWindowChrome(this, new WindowChrome()
                {
                    CaptionHeight = 0,
                    CornerRadius = new CornerRadius(0),
                    GlassFrameThickness = new Thickness(0),
                    NonClientFrameEdges = NonClientFrameEdges.None,
                    ResizeBorderThickness = new Thickness(0),
                    UseAeroCaptionButtons = false
                });
            };

            uiManager = new UIManager(GameCanvas, ScoreText, LevelText,
                         Star1, Star2, Star3,
                         Star1Empty, Star2Empty, Star3Empty,
                         CoinImage, ScorePanel, LevelPanel, StarsPanel);

            uiManager.OnPlayClicked += () => {
                if (gameManager != null)
                {
                    gameManager.Cleanup();
                }

                // Showing the UI panels again to the canvas after cleanup
                if (!GameCanvas.Children.Contains(ScorePanel))
                    GameCanvas.Children.Add(ScorePanel);
                if (!GameCanvas.Children.Contains(LevelPanel))
                    GameCanvas.Children.Add(LevelPanel);
                if (!GameCanvas.Children.Contains(StarsPanel))
                    GameCanvas.Children.Add(StarsPanel);

                ScorePanel.Visibility = Visibility.Visible;
                LevelPanel.Visibility = Visibility.Visible;
                StarsPanel.Visibility = Visibility.Visible;

                gameManager = new GameManager(GameCanvas, uiManager);
                gameManager.OnScoreChanged += UpdateScore;
                gameManager.OnLevelChanged += UpdateLevel;
                gameManager.OnLivesChanged += UpdateLives;
                gameManager.OnGameOver += HandleGameOver;

                UpdateScore(0);        
                UpdateLevel(1);        
                UpdateLives(3);

                levels = new Levels(GameCanvas, gameManager, new Random(), gameManager.GetBackgroundManager(),
                                   gameManager.GetEnemies(), gameManager.GetObstacles(), gameManager.GetPlayer(),
                                   gameManager.GetPowerUps(), gameManager.GetHealth(), uiManager, gameManager.GetHitEffects());

                levels.StartGame();
                gameManager.StartGame();
                GameCanvas.Focus();
            };

            uiManager.OnFreeFlyClicked += () => {
                if (gameManager != null)
                {
                    gameManager.Cleanup();
                }

                uiManager.HideMenu();

                gameManager = new GameManager(GameCanvas, uiManager);

                gameManager.SetFreeFlyMode(true);

                ScorePanel.Visibility = Visibility.Hidden;
                LevelPanel.Visibility = Visibility.Hidden;
                StarsPanel.Visibility = Visibility.Hidden;

                Player player = gameManager.GetPlayer();
                if (player != null)
                {
                    player.Enable(); 
                    player.Reset(); 
                }

                gameManager.StartGame();
                GameCanvas.Focus();
            };

            uiManager.OnQuitClicked += () => {
                Application.Current.Shutdown();
            };

            uiManager.ShowMenu();
        }

        private void InitializeLevelsText()
        {
            levelText = (Label)FindName("LevelText");
        }

        private void UpdateScore(int score)
        {
            ScoreText.Content = score.ToString();
        }

        private void UpdateLevel(int level)
        {
            if (levelText != null)
                levelText.Content = $"Level {level}";
        }

        private void UpdateLives(int lives)
        {
            bool star1Visible = lives >= 1;
            Star1.Visibility = star1Visible ? Visibility.Visible : Visibility.Hidden;
            Star1Empty.Visibility = star1Visible ? Visibility.Hidden : Visibility.Visible;

            bool star2Visible = lives >= 2;
            Star2.Visibility = star2Visible ? Visibility.Visible : Visibility.Hidden;
            Star2Empty.Visibility = star2Visible ? Visibility.Hidden : Visibility.Visible;

            bool star3Visible = lives >= 3;
            Star3.Visibility = star3Visible ? Visibility.Visible : Visibility.Hidden;
            Star3Empty.Visibility = star3Visible ? Visibility.Hidden : Visibility.Visible;
        }

        private void HandleGameOver()
        {
            DispatcherTimer gameOverTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };

            gameOverTimer.Tick += (sender, e) =>
            {
                gameOverTimer.Stop();
                gameManager?.Cleanup();
                uiManager?.ShowMenu();
            };

            gameOverTimer.Start();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            gameManager?.HandleKeyDown(e.Key);
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            gameManager?.HandleKeyUp(e.Key);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            gameManager?.HandleMouseMove(e.GetPosition(GameCanvas));
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            gameManager?.HandleMouseDown();
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            gameManager?.HandleMouseUp();
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            gameManager?.HandleMouseLeave();
        }

        protected override void OnClosed(EventArgs e)
        {
            levels?.Cleanup();
            gameManager?.Cleanup();
            uiManager?.Cleanup();
            base.OnClosed(e);
        }
    }
}