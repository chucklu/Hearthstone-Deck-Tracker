﻿#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Hearthstone_Deck_Tracker.Controls;
using Hearthstone_Deck_Tracker.Enums;
using Hearthstone_Deck_Tracker.FlyoutControls;
using Hearthstone_Deck_Tracker.Hearthstone;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using Hearthstone_Deck_Tracker.Windows;
using MahApps.Metro;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Application = System.Windows.Application;
using Card = Hearthstone_Deck_Tracker.Hearthstone.Card;
using Color = System.Drawing.Color;
using MediaColor = System.Windows.Media.Color;
using MessageBox = System.Windows.MessageBox;
using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Point = System.Drawing.Point;
using Region = Hearthstone_Deck_Tracker.Enums.Region;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using Size = System.Drawing.Size;

#endregion

namespace Hearthstone_Deck_Tracker
{
	public static class Helper
	{
		public static double DpiScalingX = 1.0, DpiScalingY = 1.0;

		public static readonly string[] EventKeys = {"None", "F1", "F2", "F3", "F4", "F5", "F6", "F7", "F8", "F9", "F10", "F11", "F12"};

		public static readonly Dictionary<string, string> LanguageDict = new Dictionary<string, string>
		{
			{"English", "enUS"},
			{"Chinese (China)", "zhCN"},
			{"Chinese (Taiwan)", "zhTW"},
			{"English (Great Britain)", "enGB"},
			{"French", "frFR"},
			{"German", "deDE"},
			{"Italian", "itIT"},
			{"Japanese", "jaJP"},
			{"Korean", "koKR"},
			{"Polish", "plPL"},
			{"Portuguese (Brazil)", "ptBR"},
			{"Russian", "ruRU"},
			{"Spanish (Mexico)", "esMX"},
			{"Spanish (Spain)", "esES"}
		};

		public static readonly List<string> LatinLanguages = new List<string>
		{
			"enUS",
			"enGB",
			"frFR",
			"deDE",
			"itIT",
			"ptBR",
			"esMX",
			"esES"
		};

		private static Version _currentVersion;

		private static bool? _hearthstoneDirExists;

		private static readonly Regex CardLineRegexCountFirst = new Regex(@"(^(\s*)(?<count>\d)(\s*x)?\s+)(?<cardname>[\w\s'\.:!-]+)");
		private static readonly Regex CardLineRegexCountLast = new Regex(@"(?<cardname>[\w\s'\.:!-]+)(\s+(x\s*)(?<count>\d))(\s*)$");
		private static readonly Regex CardLineRegexCountLast2 = new Regex(@"(?<cardname>[\w\s'\.:!-]+)(\s+(?<count>\d))(\s*)$");

		public static Dictionary<string, MediaColor> ClassicClassColors = new Dictionary<string, MediaColor>
		{
			{"Druid", MediaColor.FromArgb(0xFF, 0xFF, 0x7D, 0x0A)}, //#FF7D0A, 
			{"Death Knight", MediaColor.FromArgb(0xFF, 0xC4, 0x1F, 0x3B)}, //#C41F3B,
			{"Hunter", MediaColor.FromArgb(0xFF, 0xAB, 0xD4, 0x73)}, //#ABD473,
			{"Mage", MediaColor.FromArgb(0xFF, 0x69, 0xCC, 0xF0)}, //#69CCF0,
			{"Monk", MediaColor.FromArgb(0xFF, 0x00, 0xFF, 0x96)}, //#00FF96,
			{"Paladin", MediaColor.FromArgb(0xFF, 0xF5, 0x8C, 0xBA)}, //#F58CBA,
			{"Priest", MediaColor.FromArgb(0xFF, 0xFF, 0xFF, 0xFF)}, //#FFFFFF,
			{"Rogue", MediaColor.FromArgb(0xFF, 0xFF, 0xF5, 0x69)}, //#FFF569,
			{"Shaman", MediaColor.FromArgb(0xFF, 0x00, 0x70, 0xDE)}, //#0070DE,
			{"Warlock", MediaColor.FromArgb(0xFF, 0x94, 0x82, 0xC9)}, //#9482C9,
			{"Warrior", MediaColor.FromArgb(0xFF, 0xC7, 0x9C, 0x6E)} //#C79C6E
		};

		public static Dictionary<string, MediaColor> HearthStatsClassColors = new Dictionary<string, MediaColor>
		{
			{"Druid", MediaColor.FromArgb(0xFF, 0x62, 0x31, 0x13)}, //#623113,
			{"Death Knight", MediaColor.FromArgb(0xFF, 0xC4, 0x1F, 0x3B)}, //#C41F3B,
			{"Hunter", MediaColor.FromArgb(0xFF, 0x20, 0x8D, 0x43)}, //#208D43,
			{"Mage", MediaColor.FromArgb(0xFF, 0x25, 0x81, 0xBC)}, //#2581BC,
			{"Monk", MediaColor.FromArgb(0xFF, 0x00, 0xFF, 0x96)}, //#00FF96,
			{"Paladin", MediaColor.FromArgb(0xFF, 0xFB, 0xD7, 0x07)}, //#FBD707,
			{"Priest", MediaColor.FromArgb(0xFF, 0xA3, 0xB2, 0xB2)}, //#A3B2B2,
			{"Rogue", MediaColor.FromArgb(0xFF, 0x2F, 0x2C, 0x27)}, //#2F2C27,
			{"Shaman", MediaColor.FromArgb(0xFF, 0x28, 0x32, 0x73)}, //#283273,
			{"Warlock", MediaColor.FromArgb(0xFF, 0x4F, 0x26, 0x69)}, //#4F2669,
			{"Warrior", MediaColor.FromArgb(0xFF, 0xB3, 0x20, 0x25)} //#B32025
		};


		[Obsolete("Use Core.MainWindow", true)]
		public static MainWindow MainWindow => Core.MainWindow;

		public static OptionsMain OptionsMain { get; set; }
		public static bool SettingUpConstructedImporting { get; set; }

		public static Visibility UseButtonVisiblity => Config.Instance.AutoUseDeck ? Visibility.Collapsed : Visibility.Visible;

		public static bool HearthstoneDirExists
		{
			get
			{
				if(!_hearthstoneDirExists.HasValue)
					_hearthstoneDirExists = FindHearthstoneDir();
				return _hearthstoneDirExists.Value;
			}
		}

		public static bool UpdateLogConfig { get; set; }

		public static async Task<Version> CheckForUpdates(bool beta)
		{
			var betaString = beta ? "BETA" : "LIVE";
			Logger.WriteLine("Checking for " + betaString + " updates...", "Helper");

			var versionXmlUrl = beta
									? @"https://raw.githubusercontent.com/Epix37/HDT-Data/master/beta-version"
									: @"https://raw.githubusercontent.com/Epix37/HDT-Data/master/live-version";

			var currentVersion = GetCurrentVersion();
			if(currentVersion == null)
				return null;
			try
			{
				Logger.WriteLine("Current version: " + currentVersion, "Helper");
				string xml;
				using(var wc = new WebClient())
					xml = await wc.DownloadStringTaskAsync(versionXmlUrl);

				var newVersion = new Version(XmlManager<SerializableVersion>.LoadFromString(xml).ToString());
				Logger.WriteLine("Latest " + betaString + " version: " + newVersion, "Helper");

				if(newVersion > currentVersion)
					return newVersion;
			}
			catch(Exception e)
			{
				MessageBox.Show("Error checking for new " + betaString + " version.\n\n" + e.Message + "\n\n" + e.InnerException);
			}
			return null;
		}

		// A bug in the SerializableVersion.ToString() method causes this to load Version.xml incorrectly.
		// The build and revision numbers are swapped (i.e. a Revision of 21 in Version.xml loads to Version.Build == 21).
		public static Version GetCurrentVersion()
		{
			try
			{
				return _currentVersion ?? (_currentVersion = new Version(XmlManager<SerializableVersion>.Load("Version.xml").ToString()));
			}
			catch(Exception e)
			{
				MessageBox.Show(
							    e.Message + "\n\n" + e.InnerException
								+ "\n\n If you don't know how to fix this, please overwrite Version.xml with the default file.", "Error loading Version.xml");

				return null;
			}
		}

		public static string ToVersionString(this Version version) => $"{version?.Major}.{version?.Minor}.{version?.Build}";

		public static bool IsNumeric(char c)
		{
			int output;
			return int.TryParse(c.ToString(), out output);
		}

		public static bool IsHex(IEnumerable<char> chars)
			=> chars.All(c => ((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')));

		public static double DrawProbability(int copies, int deck, int draw)
			=> 1 - (BinomialCoefficient(deck - copies, draw) / BinomialCoefficient(deck, draw));

		public static double BinomialCoefficient(int n, int k)
		{
			double result = 1;
			for(var i = 1; i <= k; i++)
			{
				result *= n - (k - i);
				result /= i;
			}
			return result;
		}

		public static PngBitmapEncoder ScreenshotDeck(DeckListView dlv, double dpiX, double dpiY, string name)
		{
			try
			{
				var rtb = new RenderTargetBitmap((int)dlv.ActualWidth, (int)dlv.ActualHeight, dpiX, dpiY, PixelFormats.Pbgra32);
				rtb.Render(dlv);

				var encoder = new PngBitmapEncoder();
				encoder.Frames.Add(BitmapFrame.Create(rtb));
				return encoder;
			}
			catch(Exception)
			{
				return null;
			}
		}

		public static string ShowSaveFileDialog(string filename, string ext)
		{
			var defaultExt = $"*.{ext}";
			var saveFileDialog = new SaveFileDialog
			{
				FileName = filename,
				DefaultExt = defaultExt,
				Filter = $"{ext.ToUpper()} ({defaultExt})|{defaultExt}"
			};
			return saveFileDialog.ShowDialog() == true ? saveFileDialog.FileName : null;
		}

		public static string GetValidFilePath(string dir, string name, string extension)
		{
			var validDir = RemoveInvalidPathChars(dir);
			if(!Directory.Exists(validDir))
				Directory.CreateDirectory(validDir);

			if(!extension.StartsWith("."))
				extension = "." + extension;

			var path = validDir + "\\" + RemoveInvalidFileNameChars(name);
			if(File.Exists(path + extension))
			{
				var num = 1;
				while(File.Exists(path + "_" + num + extension))
					num++;
				path += "_" + num;
			}

			return path + extension;
		}

		public static string RemoveInvalidPathChars(string s) => RemoveChars(s, Path.GetInvalidPathChars());
		public static string RemoveInvalidFileNameChars(string s) => RemoveChars(s, Path.GetInvalidFileNameChars());
		public static string RemoveChars(string s, char[] c) => new Regex($"[{Regex.Escape(new string(c))}]").Replace(s, "");

		public static void SortCardCollection(IEnumerable collection, bool classFirst)
		{
			if(collection == null)
				return;
			var view1 = (CollectionView)CollectionViewSource.GetDefaultView(collection);
			view1.SortDescriptions.Clear();

			if(classFirst)
				view1.SortDescriptions.Add(new SortDescription(nameof(Card.IsClassCard), ListSortDirection.Descending));

			view1.SortDescriptions.Add(new SortDescription(nameof(Card.Cost), ListSortDirection.Ascending));
			view1.SortDescriptions.Add(new SortDescription(nameof(Card.Type), ListSortDirection.Descending));
			view1.SortDescriptions.Add(new SortDescription(nameof(Card.LocalizedName), ListSortDirection.Ascending));
		}

		public static List<Card> ToSortedCardList(this IEnumerable<Card> cards)
			=> cards.OrderBy(x => x.Cost).ThenByDescending(x => x.Type).ThenBy(x => x.LocalizedName).ToArray().ToList();

		public static string DeckToIdString(Deck deck)
			=> deck.GetSelectedDeckVersion().Cards.Aggregate("", (current, card) => current + (card.Id + ":" + card.Count + ";"));

		public static Bitmap CaptureHearthstone(Point point, int width, int height, IntPtr wndHandle = default(IntPtr),
												bool requireInForeground = true) => CaptureHearthstoneAsync(point, width, height, wndHandle, requireInForeground).Result;

		public static async Task<Bitmap> CaptureHearthstoneAsync(Point point, int width, int height, IntPtr wndHandle = default(IntPtr),
																 bool requireInForeground = true, bool? altScreenCapture = null)
		{
			if(wndHandle == default(IntPtr))
				wndHandle = User32.GetHearthstoneWindow();

			if(requireInForeground && !User32.IsHearthstoneInForeground())
				return null;

			try
			{
				if(altScreenCapture ?? Config.Instance.AlternativeScreenCapture)
					return await Task.Run(() => CaptureWindow(wndHandle, point, width, height));
				return await Task.Run(() => CaptureScreen(wndHandle, point, width, height));
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error capturing hearthstone: " + ex, "Helper");
				return null;
			}
		}

		public static Bitmap CaptureWindow(IntPtr wndHandle, Point point, int width, int height)
		{
			User32.Rect windowRect;
			User32.GetWindowRect(wndHandle, out windowRect);
			var windowWidth = windowRect.right - windowRect.left;
			var windowHeight = windowRect.bottom - windowRect.top;
			var bmp = new Bitmap(windowWidth, windowHeight, PixelFormat.Format32bppArgb);
			using(var graphics = Graphics.FromImage(bmp))
			{
				var hdc = graphics.GetHdc();
				
				try
				{
					User32.PrintWindow(wndHandle, hdc, 0);
				}
				finally
				{
					graphics.ReleaseHdc(hdc);
				}
			}
			var cRect = new User32.Rect();
			User32.GetClientRect(wndHandle, ref cRect);
			var cWidth = cRect.right - cRect.left;
			var cHeight = cRect.bottom - cRect.top;
			var captionHeight = windowHeight - cHeight > 0 ? SystemInformation.CaptionHeight : 0;
			return bmp.Clone(new Rectangle((windowWidth - cWidth) / 2 + point.X, 
				(windowHeight - cHeight - captionHeight) / 2 + captionHeight + point.Y,
				width, height), PixelFormat.Format32bppArgb);
		}

		public static Bitmap CaptureScreen(IntPtr wndHandle, Point point, int width, int height)
		{
			User32.ClientToScreen(wndHandle, ref point);
			var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
			var graphics = Graphics.FromImage(bmp);
			graphics.CopyFromScreen(point.X, point.Y, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
			return bmp;
		}

		public static async Task<bool> FriendsListOpen()
		{
			//wait for friendslist to open/close
			await Task.Delay(300);

			var rect = User32.GetHearthstoneRect(false);
			var capture = await CaptureHearthstoneAsync(new Point(0, (int)(rect.Height * 0.85)), (int)(rect.Width * 0.1), (int)(rect.Height * 0.15));
			if(capture == null)
				return false;

			for(var y = 0; y < capture.Height; y++)
			{
				for(var x = 0; x < capture.Width; x++)
				{
					if(!IsYellowPixel(capture.GetPixel(x, y)))
						continue;
					var foundFriendsList = true;

					//check for a straight yellow line (left side of add button)
					for(var i = 0; i < 5; i++)
					{
						if(x + i >= capture.Width || !IsYellowPixel(capture.GetPixel(x + i, y)))
							foundFriendsList = false;
					}
					if(foundFriendsList)
					{
						Logger.WriteLine("Found Friendslist", "Helper");
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsYellowPixel(Color pixel)
		{
			const int red = 216;
			const int green = 174;
			const int blue = 10;
			const int deviation = 10;
			return Math.Abs(pixel.R - red) <= deviation && Math.Abs(pixel.G - green) <= deviation && Math.Abs(pixel.B - blue) <= deviation;
		}

		public static void UpdateEverything(GameV2 game)
		{
			if(Core.Overlay.IsVisible)
				Core.Overlay.Update(false);

			if(Core.Windows.PlayerWindow.IsVisible)
				Core.Windows.PlayerWindow.SetCardCount(game.Player.HandCount, game.Player.DeckCount);

			if(Core.Windows.OpponentWindow.IsVisible)
				Core.Windows.OpponentWindow.SetOpponentCardCount(game.Opponent.HandCount, game.Opponent.DeckCount, game.Opponent.HasCoin);


			if(Core.MainWindow.NeedToIncorrectDeckMessage && !Core.MainWindow.IsShowingIncorrectDeckMessage
			   && game.CurrentGameMode != GameMode.Spectator && game.IgnoreIncorrectDeck != DeckList.Instance.ActiveDeck)
			{
				Core.MainWindow.IsShowingIncorrectDeckMessage = true;
				Core.MainWindow.ShowIncorrectDeckMessage();
			}
		}

		//http://stackoverflow.com/questions/23927702/move-a-folder-from-one-drive-to-another-in-c-sharp
		public static void CopyFolder(string sourceFolder, string destFolder)
		{
			if(!Directory.Exists(destFolder))
				Directory.CreateDirectory(destFolder);
			var files = Directory.GetFiles(sourceFolder);
			foreach(var file in files)
			{
				var name = Path.GetFileName(file);
				var dest = Path.Combine(destFolder, name);
				File.Copy(file, dest);
			}
			var folders = Directory.GetDirectories(sourceFolder);
			foreach(var folder in folders)
			{
				var name = Path.GetFileName(folder);
				var dest = Path.Combine(destFolder, name);
				CopyFolder(folder, dest);
			}
		}

		//http://stackoverflow.com/questions/3769457/how-can-i-remove-accents-on-a-string
		public static string RemoveDiacritics(string src, bool compatNorm)
		{
			var sb = new StringBuilder();
			foreach(var c in src.Normalize(compatNorm ? NormalizationForm.FormKD : NormalizationForm.FormD))
			{
				switch(CharUnicodeInfo.GetUnicodeCategory(c))
				{
					case UnicodeCategory.NonSpacingMark:
					case UnicodeCategory.SpacingCombiningMark:
					case UnicodeCategory.EnclosingMark:
						break;
					default:
						sb.Append(c);
						break;
				}
			}

			return sb.ToString();
		}

		public static string GetWinPercentString(int wins, int losses)
			=> wins + losses == 0 ? "-%" : Math.Round(wins * 100.0 / (wins + losses), 0) + "%";

		public static T DeepClone<T>(T obj)
		{
			using(var ms = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(ms, obj);
				ms.Position = 0;
				return (T)formatter.Deserialize(ms);
			}
		}

		public static long ToUnixTime(this DateTime time)
		{
			var total = (long)(time.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
			return total < 0 ? 0 : total;
		}

		public static DateTime FromUnixTime(long unixTime)
			=> new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Add(TimeSpan.FromSeconds(unixTime)).ToLocalTime();

		public static DateTime FromUnixTime(string unixTime)
		{
			long time;
			return long.TryParse(unixTime, out time) ? FromUnixTime(time) : DateTime.Now;
		}

		public static async Task SetupConstructedImporting(GameV2 game)
		{
			var settings = new MessageDialogs.Settings {AffirmativeButtonText = "continue"};
			if(!game.IsRunning)
				await Core.MainWindow.ShowMessageAsync("Step 0:", "Start Hearthstone", settings: settings);
			await Core.MainWindow.ShowMessageAsync("Step 1:", "Go to the main menu", settings: settings);
			SettingUpConstructedImporting = true;
			await
				Core.MainWindow.ShowMessageAsync("Step 2:",
												 "Open \"My Collection\" and click each class icon at the top once.\n\n- Do not click on neutral\n- Do not open any decks\n- Do not flip the pages.",
												 settings: new MessageDialogs.Settings {AffirmativeButtonText = "done"});
			Config.Instance.ConstructedImportingIgnoreCachedIds = game.PossibleConstructedCards.Select(c => c.Id).ToArray();
			Config.Save();
			SettingUpConstructedImporting = false;
		}

		public static Rectangle GetHearthstoneRect(bool dpiScaling) => User32.GetHearthstoneRect(dpiScaling);

		public static string ParseDeckNameTemplate(string template) => ParseDeckNameTemplate(template, null);

		public static string ParseDeckNameTemplate(string template, Deck deck)
		{
			try
			{
				var result = template;
				const string dateRegex = "{Date (?<date>(.*?))}";
				var match = Regex.Match(template, dateRegex);
				if(match.Success)
				{
					var date = DateTime.Now.ToString(match.Groups["date"].Value);
					result = Regex.Replace(result, dateRegex, date);
				}
				const string classRegex = "{Class}";
				match = Regex.Match(template, classRegex);
				if(match.Success)
					result = Regex.Replace(result, classRegex, deck?.Class ?? "");
				return result;
			}
			catch
			{
				return template;
			}
		}

		//http://stackoverflow.com/questions/14795197/forcefully-replacing-existing-files-during-extracting-file-using-system-io-compr
		public static void ExtractToDirectory(this ZipArchive archive, string destinationDirectoryName, bool overwrite)
		{
			if(!overwrite)
			{
				archive.ExtractToDirectory(destinationDirectoryName);
				return;
			}
			foreach(var file in archive.Entries)
			{
				var completeFileName = Path.Combine(destinationDirectoryName, file.FullName);
				if(file.Name == "")
				{
					// Assuming Empty for Directory
					Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
					continue;
				}
				file.ExtractToFile(completeFileName, true);
			}
		}

		public static void UpdatePlayerCards()
		{
			Core.Overlay.UpdatePlayerCards();
			Core.Windows.PlayerWindow.UpdatePlayerCards();
		}

		public static void UpdateOpponentCards()
		{
			Core.Overlay.UpdateOpponentCards();
			Core.Windows.OpponentWindow.UpdateOpponentCards();
		}


		public static async Task StartHearthstoneAsync()
		{
			if(User32.GetHearthstoneWindow() != IntPtr.Zero)
				return;
			Core.MainWindow.BtnStartHearthstone.IsEnabled = false;
			var useNoDeckMenuItem = Core.TrayIcon.NotifyIcon.ContextMenu.MenuItems.IndexOfKey("startHearthstone");
			Core.TrayIcon.NotifyIcon.ContextMenu.MenuItems[useNoDeckMenuItem].Enabled = false;
			try
			{
				var bnetProc = Process.GetProcessesByName("Battle.net").FirstOrDefault();
				if(bnetProc == null)
				{
					Process.Start("battlenet://");

					var foundBnetWindow = false;
					Core.MainWindow.TextBlockBtnStartHearthstone.Text = "STARTING LAUNCHER...";
					for(var i = 0; i < 20; i++)
					{
						bnetProc = Process.GetProcessesByName("Battle.net").FirstOrDefault();
						if(bnetProc != null && bnetProc.MainWindowHandle != IntPtr.Zero)
						{
							foundBnetWindow = true;
							break;
						}
						await Task.Delay(500);
					}
					Core.MainWindow.TextBlockBtnStartHearthstone.Text = "START LAUNCHER / HEARTHSTONE";
					if(!foundBnetWindow)
					{
						Core.MainWindow.ShowMessageAsync("Error starting battle.net launcher", "Could not find or start the battle.net launcher.").Forget();
						Core.MainWindow.BtnStartHearthstone.IsEnabled = true;
						return;
					}
				}
				await Task.Delay(2000); //just to make sure
				Process.Start("battlenet://WTCG");
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error starting launcher/hearthstone: " + ex);
			}

			Core.TrayIcon.NotifyIcon.ContextMenu.MenuItems[useNoDeckMenuItem].Enabled = true;
			Core.MainWindow.BtnStartHearthstone.IsEnabled = true;
		}

		public static Region GetCurrentRegion()
		{
			try
			{
				var regex = new Regex(@"AccountListener.OnAccountLevelInfoUpdated.*currentRegion=(?<region>(\d))");
				var conLogPath = Path.Combine(Config.Instance.HearthstoneDirectory, "ConnectLog.txt");
				using(var fs = new FileStream(conLogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using(var reader = new StreamReader(fs))
				{
					var lines = reader.ReadToEnd().Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
					foreach(var line in lines)
					{
						var match = regex.Match(line);
						if(!match.Success)
							continue;
						Region region;
						if(Enum.TryParse(match.Groups["region"].Value, out region))
						{
							Logger.WriteLine("Current region: " + region, "LogReader");
							return region;
						}
					}
				}
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error getting region:\n" + ex, "LogReader");
			}
			return Region.UNKNOWN;
		}

		private static bool FindHearthstoneDir()
		{
			if(string.IsNullOrEmpty(Config.Instance.HearthstoneDirectory)
			   || !File.Exists(Config.Instance.HearthstoneDirectory + @"\Hearthstone.exe"))
			{
				using(var hsDirKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone"))
				{
					if(hsDirKey == null)
						return false;
					var hsDir = (string)hsDirKey.GetValue("InstallLocation");

					//verify the install location actually is correct (possibly moved?)
					if(!File.Exists(hsDir + @"\Hearthstone.exe"))
						return false;
					Config.Instance.HearthstoneDirectory = hsDir;
					Config.Save();
				}
			}
			return true;
		}

		public static Deck ParseCardString(string cards, bool localizedNames = false)
		{
			try
			{
				var deck = new Deck();
				var lines = cards.Split('\n');
				foreach(var line in lines)
				{
					var count = 1;
					var cardName = line.Trim();
					Match match = null;
					if(CardLineRegexCountFirst.IsMatch(cardName))
						match = CardLineRegexCountFirst.Match(cardName);
					else if(CardLineRegexCountLast.IsMatch(cardName))
						match = CardLineRegexCountLast.Match(cardName);
					else if(CardLineRegexCountLast2.IsMatch(cardName))
						match = CardLineRegexCountLast2.Match(cardName);
					if(match != null)
					{
						var tmpCount = match.Groups["count"];
						if(tmpCount.Success)
							count = int.Parse(tmpCount.Value);
						cardName = match.Groups["cardname"].Value.Trim();
					}

					var card = Database.GetCardFromName(cardName, localizedNames);
					if(string.IsNullOrEmpty(card?.Name))
						continue;
					card.Count = count;

					if(string.IsNullOrEmpty(deck.Class) && card.PlayerClass != "Neutral")
						deck.Class = card.PlayerClass;

					if(deck.Cards.Contains(card))
					{
						var deckCard = deck.Cards.First(c => c.Equals(card));
						deck.Cards.Remove(deckCard);
						deckCard.Count += count;
						deck.Cards.Add(deckCard);
					}
					else
						deck.Cards.Add(card);
				}
				return deck;
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error parsing card string: " + ex, "Import");
				return null;
			}
		}


		public static void CopyReplayFiles()
		{
			if(Config.Instance.SaveDataInAppData == null)
				return;
			var appDataReplayDirPath = Config.AppDataPath + @"\Replays";
			var dataReplayDirPath = Config.Instance.DataDirPath + @"\Replays";
			if(Config.Instance.SaveDataInAppData.Value)
			{
				if(Directory.Exists(dataReplayDirPath))
				{
					//backup in case the file already exists
					var time = DateTime.Now.ToFileTime();
					if(Directory.Exists(appDataReplayDirPath))
					{
						CopyFolder(appDataReplayDirPath, appDataReplayDirPath + time);
						Directory.Delete(appDataReplayDirPath, true);
						Logger.WriteLine("Created backups of replays in appdata", "Load");
					}


					CopyFolder(dataReplayDirPath, appDataReplayDirPath);
					Directory.Delete(dataReplayDirPath, true);

					Logger.WriteLine("Moved replays to appdata", "Load");
				}
			}
			else if(Directory.Exists(appDataReplayDirPath)) //Save in DataDir and AppData Replay dir still exists
			{
				//backup in case the file already exists
				var time = DateTime.Now.ToFileTime();
				if(Directory.Exists(dataReplayDirPath))
				{
					CopyFolder(dataReplayDirPath, dataReplayDirPath + time);
					Directory.Delete(dataReplayDirPath, true);
				}
				Logger.WriteLine("Created backups of replays locally", "Load");


				CopyFolder(appDataReplayDirPath, dataReplayDirPath);
				Directory.Delete(appDataReplayDirPath, true);
				Logger.WriteLine("Moved replays to appdata", "Load");
			}
		}

		public static void UpdateAppTheme()
		{
			var theme = string.IsNullOrEmpty(Config.Instance.ThemeName)
							? ThemeManager.DetectAppStyle().Item1 : ThemeManager.AppThemes.First(t => t.Name == Config.Instance.ThemeName);
			var accent = string.IsNullOrEmpty(Config.Instance.AccentName)
							 ? ThemeManager.DetectAppStyle().Item2 : ThemeManager.Accents.First(a => a.Name == Config.Instance.AccentName);
			ThemeManager.ChangeAppStyle(Application.Current, accent, theme);
			Application.Current.Resources["GrayTextColorBrush"] = theme.Name == "BaseLight"
																	  ? new SolidColorBrush((MediaColor)Application.Current.Resources["GrayTextColor1"])
																	  : new SolidColorBrush((MediaColor)Application.Current.Resources["GrayTextColor2"]);
		}

		public static double GetScaledXPos(double left, int width, double ratio) => (width * ratio * left) + (width * (1 - ratio) / 2);

		public static MediaColor GetClassColor(string className, bool priestAsGray)
		{
			if(string.IsNullOrEmpty(className))
				return Colors.DimGray;
			MediaColor color;
			if(Config.Instance.ClassColorScheme == ClassColorScheme.HearthStats)
			{
				if(!HearthStatsClassColors.TryGetValue(className, out color))
					color = Colors.DimGray;
			}
			else
			{
				if(className == "Priest" && priestAsGray)
					color = MediaColor.FromArgb(0xFF, 0xD2, 0xD2, 0xD2); //#D2D2D2
				else if(!ClassicClassColors.TryGetValue(className, out color))
					color = MediaColor.FromArgb(0xFF, 0x80, 0x80, 0x80); //#808080
			}
			return color;
		}

		public static MetroWindow GetParentWindow(DependencyObject current)
		{
			var parent = VisualTreeHelper.GetParent(current);
			while(parent != null && !(parent is MetroWindow))
				parent = VisualTreeHelper.GetParent(parent);
			return (MetroWindow)parent;
		}

		public static bool IsWindows10()
		{
			try
			{
				var reg = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
				return reg != null && ((string)reg.GetValue("ProductName")).Contains("Windows 10");
			}
			catch(Exception ex)
			{
				Logger.WriteLine("Error getting windows version information. " + ex);
				return false;
			}
		}

		public static void TryOpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch(Exception e)
			{
				Logger.WriteLine($"Error opening url: {e}");
			}
		}
	}
}