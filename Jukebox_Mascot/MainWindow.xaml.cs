using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.IO;
using System.Data.SqlTypes;

namespace Jukebox_Mascot
{
    /// <summary>
    /// Interaction logic for Jukebox.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon TRAY_ICON;

        private const int DEFAULT_WIDTH = 300;
        private const int DEFAULT_HEIGHT = 300;
        private int FRAME_WIDTH_JUKEBOX = 300;
        private int FRAME_HEIGHT_JUKEBOX = 450;
        private int FRAME_WIDTH = 300;
        private int FRAME_HEIGHT = 300;
        private string START_CHAR = "RiceShower";

        private int JUKEBOX_FUNNY_FRAME_COUNT = 17;
        private int MUSIC_NOTE_FRAME = 123;
        private int INTRO_FRAME_COUNT = 52;
        private int DANCE_FRAME_COUNT = 187;
        private int JUKEBOX_FRAME_COUNT = 22;

        private int CURRENT_MASCOT_INDEX = 0;
        private int SPRITE_COLUMN = 5; // This is the main default on how I hand Sprite Columns
        private int CURRENT_JUKEBOXF_FRAME = 0;
        private int CURRENT_INTRO_FRAME = 0;
        private int CURRENT_DANCE_FRAME = 0;
        private int CURRENT_JUKEBOX_FRAME = 0;
        private int CURRENT_MUSIC_NOTE_FRAME = 0;
        private int CURRENT_TRACK_INDEX = 0;
        private int FRAME_RATE = 31;
        private string MUSIC_DIR = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");

        private BitmapImage MUSIC_NOTE_SHEET;
        private BitmapImage JUKEBOX_FUNNY_SHEET;
        private BitmapImage INTRO_SHEET;
        private BitmapImage DANCE_SHEET;
        private BitmapImage JUKEBOX_SHEET;

        private DispatcherTimer CLOSE_TIMER;
        private DispatcherTimer MASTER_TIMER;
        private DispatcherTimer SCROLL_TIMER;

        private bool PLAY_INTRO_ON_NEW_SONG = true;
        private bool IS_INTRO = true;
        private bool IS_INTRO_JUKEBOX = true;
        private bool FORWARD_ANIMATION = true;
        private bool ALLOW_RANDOM_MASCOT = true;
        private bool ALLOW_MUSIC_NOTES = true;
        private bool IS_RANDOM = false;

        private double SCROLL_POS;

        private List<string> MASCOTS = new List<string>();
        private List<string> MUSIC_FILES = new List<string>();

        private MediaPlayer PLAYER;

        private BitmapImage LoadSprite(string filefolder, string fileName, string rootFolder = "Characters")
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", rootFolder, filefolder, fileName);

            if (!File.Exists(path))
            {
                FatalError("Cannot find the spritefile for selected character", "Missing Character File");
            }

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(path);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
        private void LoadSpritesSheet()
        {
            JUKEBOX_SHEET = LoadSprite("Jukebox", "jukebox.png", rootFolder: "");
            INTRO_SHEET = LoadSprite(START_CHAR, "intro.png");
            DANCE_SHEET = LoadSprite(START_CHAR, "dance.png");
            if (ALLOW_MUSIC_NOTES)
            {
                MUSIC_NOTE_SHEET = LoadSprite("Jukebox", "music_note.png", rootFolder: "");
            }
            JUKEBOX_FUNNY_SHEET = LoadSprite("Jukebox", "music_box_goofy.png", rootFolder: "");
        }

        private void LoadMascotList()
        {
            string spriteDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SpriteSheet", "Characters");

            if (!Directory.Exists(spriteDir))
            {
                FatalError("SpriteSheet/Characters folder is missing!");
            }

            MASCOTS = Directory.GetDirectories(spriteDir).Select(Path.GetFileName).ToList();

            if (MASCOTS.Count == 0)
            {
                FatalError("No mascots found in SpriteSheet/Characters!");
            }
        }

        //Mainly for the user, This will be updated or debugging purposes
        //To many edge cases for the program to crash.
        private void FatalError(string message, string title = "Error")
        {
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
            TRAY_ICON?.Dispose();
            System.Windows.Application.Current.Shutdown();
        }
        private int PlayAnimation(BitmapImage sheet,int currentFrame,int frameCount,int frameWidth,int frameHeight,System.Windows.Controls.Image targetImage,bool reverse = false)
        {
            if (sheet == null)
                return currentFrame;

            int x = (currentFrame % SPRITE_COLUMN) * frameWidth;
            int y = (currentFrame / SPRITE_COLUMN) * frameHeight;

            if (x + frameWidth > sheet.PixelWidth || y + frameHeight > sheet.PixelHeight)
                return currentFrame;

            targetImage.Source = new CroppedBitmap(sheet, new Int32Rect(x, y, frameWidth, frameHeight));

            if (!reverse)
            {             
                return (currentFrame + 1) % frameCount;
            }
            else
            {
                if (FORWARD_ANIMATION)
                {
                    currentFrame++;
                    if (currentFrame >= frameCount - 1) FORWARD_ANIMATION = false;
                }
                else
                {
                    currentFrame--;
                    if (currentFrame <= 0) FORWARD_ANIMATION = true;
                }
                return currentFrame;
            }
        }

        private void InitializeAnimations()
        {
            MASTER_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(FRAME_RATE)};
            MASTER_TIMER.Tick += (s, e) =>
            {
                if (IS_INTRO)
                {
                    CURRENT_INTRO_FRAME = PlayAnimation(INTRO_SHEET, CURRENT_INTRO_FRAME, INTRO_FRAME_COUNT,FRAME_WIDTH, FRAME_HEIGHT, SpriteImage);

                    if (CURRENT_INTRO_FRAME == 0)
                    {
                        IS_INTRO_JUKEBOX = false;
                        IS_INTRO = false;
                        JukeBoxSprite.Source = null;
                    }
                }
                if (IS_INTRO_JUKEBOX)
                {
                    CURRENT_JUKEBOX_FRAME = PlayAnimation(JUKEBOX_SHEET, CURRENT_JUKEBOX_FRAME, JUKEBOX_FRAME_COUNT,
                        FRAME_WIDTH_JUKEBOX, FRAME_HEIGHT_JUKEBOX, JukeBoxSprite);
                }

                if (!IS_INTRO)
                {
                    CURRENT_JUKEBOXF_FRAME = PlayAnimation(JUKEBOX_FUNNY_SHEET, CURRENT_JUKEBOXF_FRAME, JUKEBOX_FUNNY_FRAME_COUNT,
                        DEFAULT_WIDTH, DEFAULT_HEIGHT, JukeBoxSpriteFunny, reverse: true);

                    CURRENT_DANCE_FRAME = PlayAnimation(DANCE_SHEET, CURRENT_DANCE_FRAME, DANCE_FRAME_COUNT,
                        FRAME_WIDTH, FRAME_HEIGHT, SpriteImage);
                }
                if (ALLOW_MUSIC_NOTES)
                {
                    CURRENT_MUSIC_NOTE_FRAME = PlayAnimation(MUSIC_NOTE_SHEET, CURRENT_MUSIC_NOTE_FRAME, MUSIC_NOTE_FRAME,
                    DEFAULT_WIDTH, DEFAULT_HEIGHT, MusicNote);
                }
            };
            MASTER_TIMER.Start();
        }

        private void InitializeMusic()
        {
            PLAYER = new MediaPlayer();
            if (MUSIC_DIR == "Music")
            {
                MUSIC_DIR = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Music");
            }

            if (!Directory.Exists(MUSIC_DIR))
            {
                Directory.CreateDirectory(MUSIC_DIR);
            }

            MUSIC_FILES = Directory.GetFiles(MUSIC_DIR, "*.mp3", SearchOption.AllDirectories)
            .Union(Directory.GetFiles(MUSIC_DIR, "*.flac", SearchOption.AllDirectories))
            .Union(Directory.GetFiles(MUSIC_DIR, "*.wav", SearchOption.AllDirectories))
            .Union(Directory.GetFiles(MUSIC_DIR, "*.ogg", SearchOption.AllDirectories))
            .Union(Directory.GetFiles(MUSIC_DIR, "*.m4a", SearchOption.AllDirectories))
            .Union(Directory.GetFiles(MUSIC_DIR, "*.wma", SearchOption.AllDirectories))
            .Union(Directory.GetFiles(MUSIC_DIR, "*.aac", SearchOption.AllDirectories))
            .ToList();

            if (MUSIC_FILES.Count == 0)
            {
                return;
            }

            CURRENT_TRACK_INDEX = 0;
            LoadTrack(CURRENT_TRACK_INDEX);

            PLAYER.MediaEnded += (s, e) => { NextTrack(); };
            PLAYER.Play();
        }

        private void LoadTrack(int index)
        {
            if (index < 0 || index >= MUSIC_FILES.Count)
            {           
                return;
            }
     
            string filePath = MUSIC_FILES[index];
            PLAYER.Open(new Uri(filePath));

            string songName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            ScrollingText.Text = $"🎵 Now Playing: {songName} 🎵";
            ReopenScrollingBorder();

            if (ALLOW_RANDOM_MASCOT)
            {
                SwitchToRandomCharacter();
            }
            if (PLAY_INTRO_ON_NEW_SONG)
            {
                JukeBoxSprite.Source = null;
                IS_INTRO = true;
                CURRENT_INTRO_FRAME = 0;
                CURRENT_JUKEBOX_FRAME = 0;
            }
        }


        private void PlayMusic() => PLAYER.Play();
        private void PauseMusic() => PLAYER.Pause();

        private void NextTrack()
        {
            if(MUSIC_FILES.Count <= 0)
            {
                return;
            }
            if (IS_RANDOM)
            {
                var rand = new Random();
                CURRENT_TRACK_INDEX = rand.Next(MUSIC_FILES.Count);
            }
            else
            {
                CURRENT_TRACK_INDEX = (CURRENT_TRACK_INDEX + 1) % MUSIC_FILES.Count;
            }

            LoadTrack(CURRENT_TRACK_INDEX);
            PLAYER.Play();
        }

        private void ReopenScrollingBorder()
        {
            SCROLL_TIMER?.Stop();
            CLOSE_TIMER?.Stop();

            ScrollingText.Visibility = Visibility.Collapsed;
            ScrollingBorder.Width = 0;
            ScrollingBorder.Visibility = Visibility.Collapsed;

            DispatcherTimer openTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            openTimer.Tick += (s, e) =>
            {
                if (ScrollingBorder.Width == 0)
                {
                    ScrollingBorder.Visibility = Visibility.Visible;
                }
                if (ScrollingBorder.Width < 250)
                {
                    ScrollingBorder.Width += 10;
                }
                else
                {
                    openTimer.Stop();
                    string filePath = MUSIC_FILES[CURRENT_TRACK_INDEX];
                    string songName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    ScrollingText.Text = $"🎵 Now Playing: {songName} 🎵";
                    ScrollingText.Visibility = Visibility.Visible;                    
                    StartScrolling();
                }
            };
            openTimer.Start();
        }

        private void StartScrolling()
        {
            SCROLL_TIMER?.Stop();
            SCROLL_POS = (int)ScrollingBorder.Width;
            var transform = new TranslateTransform(SCROLL_POS, 0);
            ScrollingText.RenderTransform = transform;

            SCROLL_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
            SCROLL_TIMER.Tick += (s, e) =>
            {
                SCROLL_POS -= 2;
                transform.X = SCROLL_POS;

                if (SCROLL_POS < -ScrollingText.ActualWidth)
                {
                    SCROLL_TIMER.Stop();
                    StartClosingAnimation();
                }
            };
            SCROLL_TIMER.Start();
        }

        private void StartClosingAnimation()
        {
            CLOSE_TIMER = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(15) };
            CLOSE_TIMER.Tick += (s, e) =>
            {
                if (ScrollingBorder.Width > 0)
                {
                    ScrollingBorder.Width -= 10;
                }
                else
                {
                    CLOSE_TIMER.Stop();
                    ScrollingBorder.Visibility = Visibility.Collapsed;
                }
            };
            CLOSE_TIMER.Start();
        }

        private void ResetApp()
        {
            TRAY_ICON.Visible = false;
            string exePath = Process.GetCurrentProcess().MainModule.FileName;
            Process.Start(exePath);
            System.Windows.Application.Current.Shutdown();
        }

        private void CloseApp()
        {
            TRAY_ICON.Visible = false;
            TRAY_ICON?.Dispose();
            MASTER_TIMER.Stop();
            System.Windows.Application.Current.Shutdown();
        }
        private void SetupTrayIcon()
        {
            TRAY_ICON = new NotifyIcon();
            TRAY_ICON.Icon = new Icon("icon2.ico");
            TRAY_ICON.Visible = true;
            TRAY_ICON.Text = "Jukebox";

            var menu = new ContextMenuStrip();
            menu.Items.Add("Play", null, (s, e) => PlayMusic());
            menu.Items.Add("Pause", null, (s, e) => PauseMusic());
            menu.Items.Add("Next Track", null, (s, e) => NextTrack());

            var randomChar = new ToolStripMenuItem("Random Characters") { CheckOnClick = true };
            randomChar.CheckedChanged += (s, e) =>
            {
                ALLOW_RANDOM_MASCOT = randomChar.Checked;
            };

            var randomItem = new ToolStripMenuItem("Random Music") { CheckOnClick = true };
            randomItem.CheckedChanged += (s, e) =>
            { 
                IS_RANDOM = randomItem.Checked;
            };

            menu.Items.Add(randomItem);
            menu.Items.Add(randomChar); 
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Reappear", null, (s, e) => ResetApp());
            menu.Items.Add("Close", null, (s, e) => CloseApp());

            TRAY_ICON.ContextMenuStrip = menu;
        }
        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }
        public MainWindow()
        {
            InitializeComponent();
            this.ShowInTaskbar = false;
            LoadMasterConfig();
            LoadConfigChar();
            SetupTrayIcon();
            LoadSpritesSheet();
            LoadMascotList(); 
            InitializeAnimations();
            InitializeMusic();
        }
        private void JukeBoxSprite_Click(object sender, MouseButtonEventArgs e)
        {
            if (MASCOTS.Count == 0) return;

            CURRENT_MASCOT_INDEX = (CURRENT_MASCOT_INDEX + 1) % MASCOTS.Count;
            START_CHAR = MASCOTS[CURRENT_MASCOT_INDEX];
            
            
            INTRO_SHEET = null;
            DANCE_SHEET = null;
            SpriteImage.Source = null;

            GC.Collect();
            LoadConfigChar();
            LoadSpritesSheet();

            IS_INTRO = true;
            CURRENT_INTRO_FRAME = 0;
            CURRENT_DANCE_FRAME = 0;


            SpriteLabel.Content = $"Mascot: {START_CHAR}";
        }     
        private void SwitchToRandomCharacter()
        {
            if (MASCOTS == null || MASCOTS.Count == 0)
            {
                return;
            }

            INTRO_SHEET = null;
            DANCE_SHEET = null;
            SpriteImage.Source = null;

            GC.Collect(); // optional (forces cleanup now)

            var rand = new Random();
            string randomChar = MASCOTS[rand.Next(MASCOTS.Count)];

            START_CHAR = randomChar;
            LoadConfigChar();

            INTRO_SHEET = LoadSprite(randomChar, "intro.png");
            DANCE_SHEET = LoadSprite(randomChar, "dance.png");

            CURRENT_INTRO_FRAME = 0;
            CURRENT_DANCE_FRAME = 0;
            IS_INTRO = true;

            JukeBoxSprite.Source = null;
        }

        private void LoadMasterConfig()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config.txt");
            if (!File.Exists(path))
            {
                FatalError("Cannot find the config file for the main directory", "Missing Config File");
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                {
                    continue;
                }

                var parts = line.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                switch (key.ToUpper())
                {
                    case "START_CHAR":
                    {
                        START_CHAR = value;
                        break;
                    }
                    case "ALLOW_RANDOM_MASCOT":
                    {
                        if (bool.TryParse(value, out bool boolValue))
                        {
                            ALLOW_RANDOM_MASCOT = boolValue;
                        }
                        break;
                    }
                    case "ALLOW_MUSIC_NOTES":
                    {
                        if (bool.TryParse(value, out bool boolValue2))
                        {
                            ALLOW_MUSIC_NOTES = boolValue2;
                        }
                        break;
                    }
                    case "SPRITE_SPEED":
                    {
                        if (int.TryParse(value, out int intValue))
                        {
                            FRAME_RATE = intValue;
                        }
                        break;
                    }
                    case "MUSIC_DIR":
                    {
                        MUSIC_DIR = value;
                        break;
                    }
                }
            }
        }
        private void LoadConfigChar()
        {
            string path = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "SpriteSheet", "Characters", START_CHAR, "config.txt");

            if (!File.Exists(path))
            {
                FatalError("Cannot find character name from config/folder. Please check config file if filename matches",
                    "Missing Character File");
            }

            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) || !line.Contains("="))
                {
                    continue;
                }

                var parts = line.Split('=');
                if (parts.Length != 2)
                {
                    continue;
                }

                string key = parts[0].Trim();
                string value = parts[1].Trim();

                if (!int.TryParse(value, out int intValue))
                {
                    continue;
                }

                switch (key.ToUpper())
                {
                    case "FRAME_HEIGHT": FRAME_HEIGHT = intValue; break;
                    case "FRAME_RATE": FRAME_RATE = intValue; break;
                    case "INTRO_FRAME_COUNT": INTRO_FRAME_COUNT = intValue; break;
                    case "DANCE_FRAME_COUNT": DANCE_FRAME_COUNT = intValue; break;
                }
            }
        }
    }
}
