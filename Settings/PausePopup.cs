using LuminWarrior.Effects;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LuminWarrior
{
    public class PausePopup
    {
        private Canvas gameCanvas;
        private Border popupBorder = null!;
        private bool isVisible = false;

        private static ControlTemplate? buttonTemplate;
        private static readonly SolidColorBrush AquaBrush = new SolidColorBrush(Color.FromRgb(0, 255, 255));

        public event Action? OnResume;
        public event Action? OnMainMenu;

        public bool IsVisible => isVisible;

        public PausePopup(Canvas canvas)
        {
            gameCanvas = canvas;
            CreatePopup();
        }

        private void CreatePopup()
        {
            popupBorder = new Border
            {
                Background = Brushes.Black,
                BorderBrush = AquaBrush,
                BorderThickness = new Thickness(3),
                CornerRadius = new CornerRadius(10),
                Width = 300,
                Height = 200,
                Visibility = Visibility.Collapsed
            };

            StackPanel contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20)
            };

            TextBlock titleText = new TextBlock
            {
                Text = "PAUSED",
                FontSize = 28,
                FontWeight = FontWeights.Bold,
                Foreground = AquaBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20)
            };

            Button resumeButton = CreateRoundedButton("RESUME");
            resumeButton.Click += (s, e) => {
                Hide();
                OnResume?.Invoke();
            };
            resumeButton.Margin = new Thickness(0, 0, 0, 10);

            Button mainMenuButton = CreateRoundedButton("MAIN MENU");
            mainMenuButton.Click += (s, e) => {
                SoundEffectsManager.PlayButtonClickSound();
                Hide();
                OnMainMenu?.Invoke();
            };

            contentPanel.Children.Add(titleText);
            contentPanel.Children.Add(resumeButton);
            contentPanel.Children.Add(mainMenuButton);

            popupBorder.Child = contentPanel;

            Canvas.SetZIndex(popupBorder, 1000);
            gameCanvas.Children.Add(popupBorder);
        }

        private Button CreateRoundedButton(string text)
        {
            Button button = new Button
            {
                Content = text,
                Width = 150,
                Height = 40,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Background = AquaBrush,
                Foreground = Brushes.White,
                Cursor = System.Windows.Input.Cursors.Hand
            };

            if (buttonTemplate == null)
            {
                buttonTemplate = new ControlTemplate(typeof(Button));

                FrameworkElementFactory border = new FrameworkElementFactory(typeof(Border));
                border.SetValue(Border.CornerRadiusProperty, new CornerRadius(20));
                border.SetValue(Border.BackgroundProperty, AquaBrush);
                border.SetValue(Border.BorderBrushProperty, AquaBrush);
                border.SetValue(Border.BorderThicknessProperty, new Thickness(2));

                FrameworkElementFactory contentPresenter = new FrameworkElementFactory(typeof(ContentPresenter));
                contentPresenter.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
                contentPresenter.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);

                border.AppendChild(contentPresenter);
                buttonTemplate.VisualTree = border;
            }

            button.Template = buttonTemplate;
            return button;
        }

        public void Show()
        {
            if (!isVisible)
            {
                Canvas.SetLeft(popupBorder, (gameCanvas.ActualWidth - 300) / 2);
                Canvas.SetTop(popupBorder, (gameCanvas.ActualHeight - 200) / 2);

                popupBorder.Visibility = Visibility.Visible;
                isVisible = true;
            }
        }

        public void Hide()
        {
            if (isVisible)
            {
                popupBorder.Visibility = Visibility.Collapsed;
                isVisible = false;
            }
        }

        public void Remove()
        {
            if (gameCanvas.Children.Contains(popupBorder))
            {
                gameCanvas.Children.Remove(popupBorder);
            }
        }
    }
}