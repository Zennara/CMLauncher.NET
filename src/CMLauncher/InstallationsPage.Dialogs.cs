using System;
using System.Linq;
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

namespace CMLauncher
{
	public partial class InstallationsPage
	{
		private void ShowCreateDialog()
		{
			var dlg = new Window
			{
				Title = "Create new installation",
				Owner = Application.Current.MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				SizeToContent = SizeToContent.WidthAndHeight,
				Background = new SolidColorBrush(Color.FromRgb(27, 27, 27)),
				Foreground = Brushes.White,
				ResizeMode = ResizeMode.NoResize
			};

			var panel = new StackPanel { Margin = new Thickness(20) };

			// Icon selector - centered with small caret toggle
			var iconArea = new Grid { HorizontalAlignment = System.Windows.HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
			iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var currentIcon = new Image { Width = 56, Height = 56, Stretch = Stretch.Uniform };
			var allIcons = InstallationService.LoadAvailableIcons();
			string? selectedIcon = null;
			// pick a random default for preview and selection
			if (allIcons.Count > 0)
			{
				var rnd = new System.Random();
				selectedIcon = allIcons[rnd.Next(allIcons.Count)];
			}
			SetIconImage(currentIcon, selectedIcon);
			Grid.SetColumn(currentIcon, 0);
			iconArea.Children.Add(currentIcon);

			var caretToggle = new ToggleButton
			{
				Margin = new Thickness(8, 0, 0, 0),
				Padding = new Thickness(6, 0, 6, 0),
				Background = Brushes.Transparent,
				Foreground = Brushes.White,
				BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
				BorderThickness = new Thickness(1),
				VerticalAlignment = System.Windows.VerticalAlignment.Center,
				FocusVisualStyle = null,
				FontFamily = new FontFamily("Segoe MDL2 Assets"),
				Content = "\uE70D"
			};
			Grid.SetColumn(caretToggle, 1);
			iconArea.Children.Add(caretToggle);

			var iconPopup = new Popup
			{
				PlacementTarget = caretToggle,
				Placement = PlacementMode.Bottom,
				StaysOpen = false,
				AllowsTransparency = true
			};
			var iconScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 260, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)) };
			var iconWrap = new WrapPanel { Margin = new Thickness(8) };
			foreach (var iconName in allIcons)
			{
				var img = new Image { Width = 40, Height = 40, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
				SetIconImage(img, iconName);
				var btn = new Button { Padding = new Thickness(0), BorderThickness = new Thickness(0), Background = Brushes.Transparent, Tag = iconName, Content = img };
				btn.Click += (s, e) => { selectedIcon = iconName; SetIconImage(currentIcon, selectedIcon); iconPopup.IsOpen = false; caretToggle.IsChecked = false; };
				iconWrap.Children.Add(btn);
			}
			iconScroll.Content = iconWrap;
			iconPopup.Child = new Border { Width = 420, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48)), Child = iconScroll };
			caretToggle.Checked += (s, e) => iconPopup.IsOpen = true;
			caretToggle.Unchecked += (s, e) => iconPopup.IsOpen = false;
			iconPopup.Closed += (s, e) => { caretToggle.IsChecked = false; };

			panel.Children.Add(iconArea);

			panel.Children.Add(new TextBlock { Text = "Name", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
			var nameBox = new TextBox { Width = 360, Text = "Unnamed installation" };
			panel.Children.Add(nameBox);

			panel.Children.Add(new TextBlock { Text = "Version", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
			var versionCombo = new ComboBox { Width = 360 };

			// Add Steam with pretty label if EXE exists
			var steamExe = InstallationService.GetSteamExePath(_gameKey);
			if (!string.IsNullOrWhiteSpace(steamExe))
			{
				string steamVersion = InstallationService.GetSteamExeVersion(_gameKey) ?? "Unknown";
				versionCombo.Items.Add(new ComboBoxItem { Content = $"Steam - {steamVersion}", Tag = "Steam Version" });
			}

			// Load CMZ manifests from remote, else fallback to local versions list
			versionCombo.Items.Add(new ComboBoxItem { Content = "Loading versions...", IsEnabled = false });
			versionCombo.SelectedIndex = versionCombo.Items.Count - 1;
			panel.Children.Add(versionCombo);

			_ = LoadVersionsIntoComboAsync(versionCombo, _gameKey);

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
			var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 6, 14, 6) };
			cancel.Click += (s, e) => dlg.Close();
			var create = new Button { Content = "Install", Padding = new Thickness(14, 6, 14, 6), Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
			create.Click += (s, e) =>
			{
				var name = string.IsNullOrWhiteSpace(nameBox.Text) ? "Unnamed installation" : nameBox.Text.Trim();
				var version = GetVersionKey(versionCombo.SelectedItem);
				if (string.IsNullOrEmpty(version)) { MessageBox.Show("Please select a version."); return; }

				// If CMZ and version is manifest|branch or digits, ensure it exists (download now)
				try
				{
					var parts = version.Split('|');
					var manifest = parts[0];
					var branch = parts.Length > 1 ? parts[1] : "public";
					if (_gameKey == InstallationService.CMZKey && manifest.All(char.IsDigit))
					{
						// Perform download synchronously
						InstallationService.EnsureVersionByManifest(_gameKey, manifest, branch);
					}
				}
				catch { }

				InstallationService.CreateInstallation(_gameKey, name, version, selectedIcon);
				dlg.Close();
				RefreshList();
				// Also refresh the bottom-left selector immediately
				if (Application.Current?.MainWindow is MainWindow mw)
				{
					mw.RefreshInstallationsMenu();
				}
			};
			buttons.Children.Add(cancel);
			buttons.Children.Add(create);
			panel.Children.Add(buttons);

			dlg.Content = panel;
			dlg.ShowDialog();
		}
	}
}
