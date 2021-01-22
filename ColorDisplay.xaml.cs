using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Numerics;

namespace QuickPalette
{
    /// <summary>
    /// Interaction logic for ColorDisplay.xaml
    /// </summary>
    public partial class ColorDisplay : Page
    {
        const int ROWS = 8;
        const int COLS = 8;
        const int MAX_COLORS = ROWS * COLS;

        public ColorDisplay()
        {
            InitializeComponent();
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            Grid grid = new Grid();
            grid.Background = new SolidColorBrush(System.Windows.Media.Color.FromScRgb(1.0f, 0.0f, 0.0f, 0.0f));
            
            Application.Current.Windows[0].Height = ROWS * 64;
            Application.Current.Windows[0].Width = COLS * 64;

            grid.Drop += OnImageDrop;
            grid.AllowDrop = true;

            for (int i = 0; i < COLS; i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            for (int i = 0; i < ROWS; i++)
            {
                grid.RowDefinitions.Add(new RowDefinition());
            }

            for (int y = 0; y < ROWS; y++)
            {
                for (int x = 0; x < COLS; x++)
                {
                    System.Windows.Media.Color tempColor = System.Windows.Media.Color.FromScRgb(1.0f, x / (float)COLS, y / (float)ROWS, 0.5f);
                    System.Drawing.Color tempDrawColor = System.Drawing.Color.FromArgb(tempColor.A, tempColor.R, tempColor.G, tempColor.B);

                    Label buttonLabel = new Label
                    {
                        Content = String.Format("{0},{1}", x, y),
                        Foreground = new SolidColorBrush(tempDrawColor.GetBrightness() < 0.5 ? System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f) : System.Windows.Media.Color.FromScRgb(1.0f, 0.0f, 0.0f, 0.0f)),
                        FontSize = 12,
                    };

                    Button button = new Button
                    {
                        Tag = new Vector2(x, y),
                        ClickMode = ClickMode.Press,
                        Cursor = Cursors.Hand,
                        Content = buttonLabel,
                        BorderThickness = new Thickness(0.0),
                        SnapsToDevicePixels = true,
                    };

                    button.Click += OnColorClicked;

                    UpdateButtonStyle(button, tempColor);

                    button.Visibility = Visibility.Hidden;

                    Grid.SetColumn(button, x);
                    Grid.SetRow(button, y);

                    grid.Children.Add(button);
                }
            }

            (sender as Page).Content = grid;
        }

        private async void OnColorClicked(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Label label = button.Content as Label;

            Vector2 vec = (Vector2)(sender as Control).Tag;
            System.Windows.Media.Color col = ((SolidColorBrush)button.Background).Color;

            string hexColor = ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(col.A, col.R, col.G, col.B));

            Clipboard.SetText(hexColor);

            label.Content = "Copied!";

            await Task.Delay(TimeSpan.FromSeconds(1));

            label.Content = hexColor;
        }

        private void OnImageDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                if (files.Length > 0)
                {
                    try
                    {
                        Bitmap bmImage = new Bitmap(files[0]);

                        List<System.Drawing.Color> colors = new List<System.Drawing.Color>();

                        for (int y = 0; y < bmImage.Height; y++)
                        {
                            for (int x = 0; x < bmImage.Width; x++)
                            {
                                System.Drawing.Color col = bmImage.GetPixel(x, y);

                                if (!ContainsSimilarColor(colors, col))
                                {
                                    colors.Add(col);
                                }
                            }

                            if (colors.Count >= MAX_COLORS)
                            {
                                break;
                            }
                        }

                        Grid grid = sender as Grid;
                        for (int i = 0; i < grid.Children.Count; i++)
                        {
                            Button button = grid.Children[i] as Button;
                            if (button != null)
                            {
                                Label label = button.Content as Label;

                                UpdateButtonStyle(button, System.Windows.Media.Color.FromScRgb(0.5f, 0.0f, 0.0f, 0.0f));
                                button.Visibility = Visibility.Hidden;
                                label.Content = "";
                            }
                        }

                        for (int i = 0; i < Math.Min(grid.Children.Count, colors.Count); i++)
                        {
                            Button button = grid.Children[i] as Button;

                            if (button != null)
                            {
                                Label label = button.Content as Label;
                                UpdateButtonStyle(button, System.Windows.Media.Color.FromRgb(colors[i].R, colors[i].G, colors[i].B));
                                button.Visibility = Visibility.Visible;

                                label.Content = ColorTranslator.ToHtml(colors[i]);

                                label.Foreground = new SolidColorBrush(
                                    colors[i].GetBrightness() < 0.5 ?
                                    System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 1.0f, 1.0f) : System.Windows.Media.Color.FromScRgb(1.0f, 0.0f, 0.0f, 0.0f)
                                );
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }

        private bool ContainsSimilarColor(List<System.Drawing.Color> colors, System.Drawing.Color checkColor)
        {
            foreach (System.Drawing.Color col in colors)
            {
                float grayCol = 0.11f * col.B + 0.59f * col.G + 0.30f * col.R;
                float grayCheck = 0.11f * checkColor.B + 0.59f * checkColor.G + 0.30f * checkColor.R;
                float difference = Math.Abs(grayCol - grayCheck) * 100.0f / 255.0f;
                if (difference < 0.01)
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateButtonStyle(Button button, System.Windows.Media.Color bgColor)
        {
            Setter bgSetter = new Setter()
            {
                Property = Button.BackgroundProperty,
                Value = new SolidColorBrush(bgColor),
            };

            System.Drawing.Color dimColor = System.Drawing.Color.FromArgb(204, bgColor.R, bgColor.G, bgColor.B);

            Setter hoverBgSetter = new Setter()
            {
                Property = Button.BackgroundProperty,
                Value = new SolidColorBrush(System.Windows.Media.Color.FromArgb(dimColor.A, dimColor.R, dimColor.G, dimColor.B)),
            };

            Setter tempSetter = new Setter()
            {
                Property = Button.TemplateProperty,
                Value = (ControlTemplate)FindResource("hoverButton"),
            };

            Trigger trigger = new Trigger()
            {
                Property = Button.IsMouseOverProperty,
                Value = true,
                Setters = { hoverBgSetter },
            };

            Style style = new Style()
            {
                TargetType = typeof(Button),
                Setters = { bgSetter, tempSetter },
                Triggers = { trigger },
            };

            button.Style = style;
        }
    }
}
