using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Application = System.Windows.Application;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using System.Linq;

namespace CMLauncher
{
	public partial class InstallationsPage : Page
	{
		private const string SteamIconName = "Lantern.png"; // icon file in assets/blocks

		private StackPanel _listHost = null!;
		private readonly string _gameKey;

		public InstallationsPage(string gameKey)
		{
			_gameKey = gameKey;
			Background = new SolidColorBrush(Color.FromRgb(32, 32, 32));

			var root = new Grid { Margin = new Thickness(20) };
			root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			var headerGrid = new Grid();
			headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			headerGrid.Children.Add(new TextBlock
			{
				Text = "Installations",
				FontSize = 24,
				FontWeight = FontWeights.Bold,
				Foreground = Brushes.White,
				Margin = new Thickness(0, 0, 0, 20)
			});
			var newBtn = new Button
			{
				Content = "New Installation",
				Padding = new Thickness(15, 8, 15, 8),
				Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"),
				Foreground = Brushes.White,
				BorderThickness = new Thickness(0),
				Margin = new Thickness(10, 0, 0, 20)
			};
			newBtn.Click += (s, e) => ShowCreateDialog();
			Grid.SetColumn(newBtn, 1);
			headerGrid.Children.Add(newBtn);
			Grid.SetRow(headerGrid, 0);
			root.Children.Add(headerGrid);

			var scroll = new ScrollViewer
			{
				VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
				Background = Brushes.Transparent
			};
			_listHost = new StackPanel();
			scroll.Content = _listHost;
			Grid.SetRow(scroll, 1);
			root.Children.Add(scroll);

			Content = root;

			RefreshList();
		}
	}
}