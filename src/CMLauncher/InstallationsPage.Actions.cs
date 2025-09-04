using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Button = System.Windows.Controls.Button;
using Brushes = System.Windows.Media.Brushes;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;

namespace CMLauncher
{
	public partial class InstallationsPage
	{
		private void ShowEditDialog(InstallationInfo info)
		{
			// Guard: do not allow editing the default Steam installation
			if (string.IsNullOrEmpty(info.RootPath) || string.Equals(info.Name, "Steam Installation", StringComparison.OrdinalIgnoreCase))
				return;

			var dlg = new Window
			{
				Title = "Edit installation",
				Owner = Application.Current.MainWindow,
				WindowStartupLocation = WindowStartupLocation.CenterOwner,
				SizeToContent = SizeToContent.WidthAndHeight,
				Background = new SolidColorBrush(Color.FromRgb(27, 27, 27)),
				Foreground = Brushes.White,
				ResizeMode = ResizeMode.NoResize
			};

			var panel = new StackPanel { Margin = new Thickness(20) };

			// Icon picker identical to create
			var iconArea = new Grid { HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 12) };
			iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			iconArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

			var currentIcon = new System.Windows.Controls.Image { Width = 56, Height = 56, Stretch = Stretch.Uniform };
			string? selectedIcon = info.IconName;
			SetIconImage(currentIcon, selectedIcon);
			Grid.SetColumn(currentIcon, 0);
			iconArea.Children.Add(currentIcon);

			var caretToggle = new System.Windows.Controls.Primitives.ToggleButton
			{
				Margin = new Thickness(8, 0, 0, 0),
				Padding = new Thickness(6, 0, 6, 0),
				Background = Brushes.Transparent,
				Foreground = Brushes.White,
				BorderBrush = new SolidColorBrush(Color.FromRgb(64, 64, 64)),
				BorderThickness = new Thickness(1),
				VerticalAlignment = VerticalAlignment.Center,
				FocusVisualStyle = null,
				FontFamily = new System.Windows.Media.FontFamily("Segoe MDL2 Assets"),
				Content = "\uE70D"
			};
			Grid.SetColumn(caretToggle, 1);
			iconArea.Children.Add(caretToggle);

			var iconPopup = new System.Windows.Controls.Primitives.Popup
			{
				PlacementTarget = caretToggle,
				Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
				StaysOpen = false,
				AllowsTransparency = true
			};
			var iconScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, MaxHeight = 260, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)) };
			var iconWrap = new WrapPanel { Margin = new Thickness(8) };
			foreach (var ic in InstallationService.LoadAvailableIcons())
			{
				var im = new System.Windows.Controls.Image { Width = 40, Height = 40, Stretch = Stretch.Uniform, Margin = new Thickness(4) };
				SetIconImage(im, ic);
				var b = new Button { Content = im, Padding = new Thickness(0), BorderThickness = new Thickness(0), Background = Brushes.Transparent };
				var chosen = ic;
				b.Click += (_, __) => { selectedIcon = chosen; SetIconImage(currentIcon, selectedIcon); iconPopup.IsOpen = false; caretToggle.IsChecked = false; };
				iconWrap.Children.Add(b);
			}
			iconScroll.Content = iconWrap;
			iconPopup.Child = new Border { Width = 420, Background = new SolidColorBrush(Color.FromRgb(26, 26, 26)), BorderThickness = new Thickness(1), BorderBrush = new SolidColorBrush(Color.FromRgb(48, 48, 48)), Child = iconScroll };
			caretToggle.Checked += (s, e) => iconPopup.IsOpen = true;
			caretToggle.Unchecked += (s, e) => iconPopup.IsOpen = false;
			iconPopup.Closed += (s, e) => caretToggle.IsChecked = false;

			panel.Children.Add(iconArea);

			// Name
			panel.Children.Add(new TextBlock { Text = "Name", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 6) });
			var nameBox = new TextBox { Width = 360, Text = info.Name };
			panel.Children.Add(nameBox);

			// Version dropdown from remote/local
			panel.Children.Add(new TextBlock { Text = "Version", FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 6) });
			var versionCombo = new ComboBox { Width = 360 };
			var steamExe2 = InstallationService.GetSteamExePath(_gameKey);
			if (!string.IsNullOrWhiteSpace(steamExe2))
			{
				string steamVersion = InstallationService.GetSteamExeVersion(_gameKey) ?? "Unknown";
				versionCombo.Items.Add(new ComboBoxItem { Content = $"Steam - {steamVersion}", Tag = "Steam Version" });
			}
			versionCombo.Items.Add(new ComboBoxItem { Content = "Loading versions...", IsEnabled = false });
			panel.Children.Add(versionCombo);
			_ = LoadVersionsIntoComboAsync(versionCombo, _gameKey);

			var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = System.Windows.HorizontalAlignment.Right, Margin = new Thickness(0, 16, 0, 0) };
			var cancel = new Button { Content = "Cancel", Margin = new Thickness(0, 0, 8, 0), Padding = new Thickness(14, 6, 14, 6) };
			cancel.Click += (s, e) => dlg.Close();
			var save = new Button { Content = "Save", Padding = new Thickness(14, 6, 14, 6), Background = (Brush)Application.Current.MainWindow.FindResource("AccentBrush"), Foreground = Brushes.White, BorderThickness = new Thickness(0) };
			save.Click += (s, e) =>
			{
				var newName = string.IsNullOrWhiteSpace(nameBox.Text) ? info.Name : nameBox.Text.Trim();
				var newVersion = GetVersionKey(versionCombo.SelectedItem);

				var updated = info;
				if (!string.Equals(newName, info.Name, System.StringComparison.Ordinal))
				{
					updated = InstallationService.RenameInstallation(updated, newName);
				}
				if (!string.Equals(selectedIcon, updated.IconName, System.StringComparison.Ordinal))
				{
					InstallationService.UpdateInstallationIcon(updated, selectedIcon);
					updated.IconName = selectedIcon;
				}
				if (!string.Equals(newVersion, updated.Version, System.StringComparison.Ordinal))
				{
					InstallationService.UpdateInstallationVersion(updated, newVersion);
					updated.Version = newVersion;
				}

				dlg.Close();
				RefreshList();
				if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu();
			};
			buttons.Children.Add(cancel);
			buttons.Children.Add(save);
			panel.Children.Add(buttons);

			dlg.Content = panel;
			dlg.ShowDialog();
		}

		private void LaunchInstallation(InstallationInfo info)
		{
			try
			{
				var exeName = info.GameKey == InstallationService.CMWKey ? "CastleMinerWarfare.exe" : "CastleMinerZ.exe";
				string? gameDir;
				var isSteamPseudo = string.IsNullOrEmpty(info.RootPath) || string.Equals(info.Name, "Steam Installation", StringComparison.OrdinalIgnoreCase);
				if (isSteamPseudo)
				{
					gameDir = LauncherSettings.Current.GetSteamPathForGame(info.GameKey);
					if (string.IsNullOrWhiteSpace(gameDir))
					{
						gameDir = SteamLocator.FindGamePath(InstallationService.GetAppId(info.GameKey));
					}
					if (string.IsNullOrWhiteSpace(gameDir))
					{
						gameDir = InstallationService.GetVersionsPath(info.GameKey);
					}
				}
				else
				{
					gameDir = System.IO.Path.Combine(info.RootPath, "Game");
				}

				InstallationService.EnsureSteamAppId(info.GameKey, gameDir!);
				var exePath = System.IO.Path.Combine(gameDir!, exeName);
				if (File.Exists(exePath))
				{
					if (isSteamPseudo)
					{
						// Launch Steam install normally
						Process.Start(new ProcessStartInfo
						{
							FileName = exePath,
							WorkingDirectory = gameDir,
							UseShellExecute = true
						});
					}
					else
					{
						// Launch custom installation with isolated AppData rooted at Data directory
						var dataDir = Path.Combine(info.RootPath, "Data");
						Directory.CreateDirectory(dataDir);
						var appData = Path.Combine(dataDir, "AppData", "Roaming");
						var localAppData = Path.Combine(dataDir, "AppData", "Local");
						Directory.CreateDirectory(appData);
						Directory.CreateDirectory(localAppData);

						var psi = new ProcessStartInfo
						{
							FileName = exePath,
							WorkingDirectory = gameDir,
							UseShellExecute = false
						};
						// Inherit existing PATH and prepend game dir
						var existingPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
						psi.Environment["PATH"] = gameDir + ";" + existingPath;
						psi.Environment["PWD"] = gameDir;
						psi.Environment["USERPROFILE"] = dataDir;
						psi.Environment["APPDATA"] = appData;
						psi.Environment["LOCALAPPDATA"] = localAppData;

						Process.Start(psi);
					}

					// Update timestamp for this installation or steam
					if (isSteamPseudo)
					{
						InstallationService.MarkSteamLaunched(info.GameKey);
					}
					else if (!string.IsNullOrWhiteSpace(info.RootPath))
					{
						InstallationService.MarkInstallationLaunched(info);
					}

					// Immediately refresh list ordering and details
					RefreshList();
					if (Application.Current?.MainWindow is MainWindow mw) mw.RefreshInstallationsMenu();

					if (LauncherSettings.Current.CloseOnLaunch)
					{
						Application.Current.MainWindow?.Close();
					}
				}
				else
				{
					MessageBox.Show($"Executable not found: {exePath}", "CastleMiner Launcher", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
			catch
			{
				MessageBox.Show("Failed to launch installation.", "CastleMiner Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
