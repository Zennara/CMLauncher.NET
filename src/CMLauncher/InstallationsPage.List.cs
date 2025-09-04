using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ComboBox = System.Windows.Controls.ComboBox;
using FontFamily = System.Windows.Media.FontFamily;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;

namespace CMLauncher
{
	public partial class InstallationsPage
	{
		private async System.Threading.Tasks.Task LoadVersionsIntoComboAsync(ComboBox combo, string gameKey)
		{
			try
			{
				// Clear loading item (but keep Steam if present)
				var steamItem = combo.Items.OfType<ComboBoxItem>().FirstOrDefault(i => (i.Tag as string) == "Steam Version");
				combo.Items.Clear();
				if (steamItem != null) combo.Items.Add(steamItem);

				if (string.Equals(gameKey, InstallationService.CMZKey, StringComparison.OrdinalIgnoreCase))
				{
					var list = await ManifestService.FetchCmzManifestsAsync();
					foreach (var m in list)
					{
						var label = string.IsNullOrWhiteSpace(m.Version) ? m.ManifestId : m.Version;
						var branch = string.IsNullOrWhiteSpace(m.Branch) ? "public" : m.Branch;
						// Tag holds manifest|branch, content shows full version name
						combo.Items.Add(new ComboBoxItem { Content = label, Tag = $"{m.ManifestId}|{branch}" });
					}
				}
				else
				{
					// fallback to local versions folder for other games (no mapping)
					foreach (var v in InstallationService.LoadAvailableVersionsDetailed(gameKey))
					{
						combo.Items.Add(new ComboBoxItem { Content = v.display, Tag = v.key });
					}
				}

				combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
			}
			catch
			{
				// If anything fails, fallback to local versions
				combo.Items.Clear();
				foreach (var v in InstallationService.LoadAvailableVersions(gameKey))
				{
					combo.Items.Add(new ComboBoxItem { Content = v, Tag = v });
				}
				combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
			}
		}

		private void PopulateVersionCombo(ComboBox combo, string gameKey)
		{
			try
			{
				combo.Items.Clear();
				foreach (var (key, display) in InstallationService.LoadAvailableVersionsDetailed(gameKey))
				{
					combo.Items.Add(new ComboBoxItem { Content = display, Tag = key });
				}
				combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
			}
			catch
			{
				// If anything fails, fallback to local versions
				combo.Items.Clear();
				foreach (var v in InstallationService.LoadAvailableVersions(gameKey))
				{
					combo.Items.Add(new ComboBoxItem { Content = v, Tag = v });
				}
				combo.SelectedIndex = combo.Items.Count > 0 ? 0 : -1;
			}
		}

		private static string GetVersionKey(object? item)
		{
			if (item is ComboBoxItem cbi)
			{
				return cbi.Tag?.ToString() ?? cbi.Content?.ToString() ?? string.Empty;
			}
			return item?.ToString() ?? string.Empty;
		}

		private static void SetIconImage(Image img, string? iconName)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(iconName)) { img.Source = null; return; }
				var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks", iconName);
				if (!System.IO.File.Exists(path)) { img.Source = null; return; }
				var bmp = new BitmapImage();
				bmp.BeginInit();
				bmp.UriSource = new System.Uri(path, System.UriKind.Absolute);
				bmp.CacheOption = BitmapCacheOption.OnLoad;
				bmp.EndInit();
				bmp.Freeze();
				img.Source = bmp;
			}
			catch
			{
				img.Source = null;
			}
		}

		private void RefreshList()
		{
			_listHost.Children.Clear();

			bool firstAdded = false;

			var steamExe = InstallationService.GetSteamExePath(_gameKey);
			if (!string.IsNullOrWhiteSpace(steamExe))
			{
				string steamVersion = InstallationService.GetSteamExeVersion(_gameKey) ?? "Steam Version";
				var steamTs = InstallationService.GetSteamLastPlayed(_gameKey);
				var steamInfo = new InstallationInfo { GameKey = _gameKey, Name = "Steam Installation", Version = steamVersion, IconName = SteamIconName, RootPath = "", Timestamp = steamTs };
				AddInstallationItem(_listHost, steamInfo, isSelected: true);
				firstAdded = true;
			}

			var installs = InstallationService.LoadInstallations(_gameKey)
				.OrderByDescending(i => i.Timestamp ?? DateTime.MinValue)
				.ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
				.ToList();

			foreach (var inst in installs)
			{
				AddInstallationItem(_listHost, inst, isSelected: !firstAdded);
				firstAdded = true;
			}
		}

		private void AddInstallationItem(StackPanel panel, InstallationInfo info, bool isSelected)
		{
			var border = new Border
			{
				Background = isSelected ? new SolidColorBrush(Color.FromRgb(60, 60, 60)) : new SolidColorBrush(Color.FromRgb(45, 45, 45)),
				Margin = new Thickness(0, 0, 0, 10),
				Padding = new Thickness(15),
				CornerRadius = new CornerRadius(3)
			};

			var grid = new Grid();
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			// Icon
			var iconHost = new Border { Width = 48, Height = 48, Background = Brushes.Transparent, Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Center };
			var img = new Image { Stretch = Stretch.Uniform };
			if (!string.IsNullOrWhiteSpace(info.IconName))
			{
				try
				{
					var path = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "assets", "blocks", info.IconName);
					if (System.IO.File.Exists(path))
					{
						var bmp = new BitmapImage();
						bmp.BeginInit();
						bmp.UriSource = new System.Uri(path, System.UriKind.Absolute);
						bmp.CacheOption = BitmapCacheOption.OnLoad;
						bmp.EndInit();
						bmp.Freeze();
						img.Source = bmp;
					}
				}
				catch { }
			}
			iconHost.Child = img;
			Grid.SetColumn(iconHost, 0);
			grid.Children.Add(iconHost);

			// Info
			var infoPanel = new StackPanel();
			infoPanel.Children.Add(new TextBlock { Text = info.Name, FontSize = 16, Foreground = Brushes.White });
			var displayVersion = InstallationService.GetDisplayVersionForInstallation(info);
			infoPanel.Children.Add(new TextBlock { Text = $"Version: {displayVersion}", FontSize = 12, Foreground = Brushes.LightGray, Margin = new Thickness(0, 4, 0, 0) });
			var ts = info.Timestamp;
			var tsText = ts.HasValue ? $"Last played: {ts.Value.ToLocalTime():g}" : "Last played: never";
			infoPanel.Children.Add(new TextBlock { Text = tsText, FontSize = 11, Foreground = Brushes.Gray, Margin = new Thickness(0, 2, 0, 0) });
			Grid.SetColumn(infoPanel, 1);
			grid.Children.Add(infoPanel);

			// Actions (hover)
			var actions = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center, Visibility = Visibility.Collapsed };

			var playBtn = new Button
			{
				Content = "Play",
				Padding = new Thickness(15, 6, 15, 6),
				Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"),
				Foreground = Brushes.White,
				BorderThickness = new Thickness(0),
				Margin = new Thickness(0, 0, 8, 0)
			};
			playBtn.Click += (s, e) => { LaunchInstallation(info); };
			actions.Children.Add(playBtn);

			// Folder glyph (unchanged)
			var folderBtn = new Button
			{
				Padding = new Thickness(10, 6, 10, 6),
				Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
				Foreground = Brushes.White,
				BorderThickness = new Thickness(0),
				Margin = new Thickness(0, 0, 8, 0),
				ToolTip = "Open folder"
			};
			folderBtn.Content = new TextBlock { Text = "\uE8B7", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White };
			folderBtn.Click += (s, e) =>
			{
				try
				{
					string? path;
					var isSteamPseudo = string.IsNullOrEmpty(info.RootPath) || string.Equals(info.Name, "Steam Installation", System.StringComparison.OrdinalIgnoreCase);
					if (isSteamPseudo)
					{
						path = LauncherSettings.Current.GetSteamPathForGame(info.GameKey) ?? SteamLocator.FindGamePath(InstallationService.GetAppId(info.GameKey));
					}
					else
					{
						path = string.IsNullOrEmpty(info.RootPath) ? InstallationService.GetInstallationsPath(info.GameKey) : info.RootPath;
					}

					if (!string.IsNullOrWhiteSpace(path) && System.IO.Directory.Exists(path))
						Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
				}
				catch { }
			};
			actions.Children.Add(folderBtn);

			// More menu (unchanged)
			bool isDefaultSteam = string.IsNullOrEmpty(info.RootPath) || string.Equals(info.Name, "Steam Installation", System.StringComparison.OrdinalIgnoreCase);
			if (!isDefaultSteam)
			{
				var menuToggle = new ToggleButton
				{
					Padding = new Thickness(10, 6, 10, 6),
					Background = new SolidColorBrush(Color.FromRgb(60, 60, 60)),
					Foreground = Brushes.White,
					BorderThickness = new Thickness(0),
					ToolTip = "More"
				};
				menuToggle.Content = new TextBlock { Text = "\uE712", FontFamily = new FontFamily("Segoe MDL2 Assets"), FontSize = 14, Foreground = Brushes.White };
				var menuPopup = new Popup { PlacementTarget = menuToggle, Placement = PlacementMode.Bottom, StaysOpen = false, AllowsTransparency = true };
				var menuStack = new StackPanel();
				menuStack.Children.Add(CreateMenuItem("Edit", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; ShowEditDialog(info); }));
				menuStack.Children.Add(CreateMenuItem("Duplicate", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; var dup = InstallationService.DuplicateInstallation(info); RefreshList(); if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu(); }));
				menuStack.Children.Add(CreateMenuItem("Delete", () => { menuPopup.IsOpen = false; menuToggle.IsChecked = false; InstallationService.DeleteInstallation(info); RefreshList(); if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu(); }));
				menuPopup.Child = new Border { Background = new SolidColorBrush(Color.FromRgb(50, 50, 50)), BorderBrush = new SolidColorBrush(Color.FromRgb(80, 80, 80)), BorderThickness = new Thickness(1), Child = menuStack };
				menuToggle.Checked += (s, e) => menuPopup.IsOpen = true;
				menuToggle.Unchecked += (s, e) => menuPopup.IsOpen = false;
				menuPopup.Closed += (s, e) => menuToggle.IsChecked = false;
				actions.Children.Add(menuToggle);
			}

			Grid.SetColumn(actions, 2);
			grid.Children.Add(actions);

			border.MouseEnter += (s, e) => actions.Visibility = Visibility.Visible;
			border.MouseLeave += (s, e) => { actions.Visibility = Visibility.Collapsed; };

			border.Child = grid;
			panel.Children.Add(border);
		}

		private UIElement CreateMenuItem(string text, System.Action onClick)
		{
			var btn = new Button
			{
				Content = text,
				Padding = new Thickness(12, 8, 12, 8),
				Background = Brushes.Transparent,
				Foreground = Brushes.White,
				BorderThickness = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			btn.Click += (s, e) => onClick();
			btn.MouseEnter += (s, e) => btn.Background = new SolidColorBrush(Color.FromRgb(70, 70, 70));
			btn.MouseLeave += (s, e) => btn.Background = Brushes.Transparent;
			return btn;
		}
	}
}
