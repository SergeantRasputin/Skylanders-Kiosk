using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Com.Skylanders.Kiosk
{
    public partial class MainWindow : Window
    {
        public enum MediaState
        {
            PORTAL_INIT,
            PORTAL_IDLE,
            PORTAL_CHARACTER
        }

        private MediaState currentMediaState = MediaState.PORTAL_INIT;

        TextBlock[] skylanderNames = new TextBlock[16];
        bool displaySkylanderNames = true;

        string configFile = "KioskConfig.ini";

        long lastTime = NowMillis();
        SkylanderPortal mySkylanderPortal;
        SkylanderPortal.SkylanderOnPortal currentSkylanderPlayingMedia = null;

        int lastIdleVideo = -1;
        Random rand = new Random();

        SkylanderNameDatabase skylanderNameDatabase = new SkylanderNameDatabase("SkylanderIDs.txt");
        KioskConfig config = new KioskConfig();

        DispatcherTimer animator;

        string currentMediaSourceString = null;
        bool mediaReady = false;
        bool currentIsIdle = false;

        private static long NowMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            config = new KioskConfig();
            config.LoadConfig(configFile);

            if (config.GetConfigItemString(KioskConfig.videoDirectoryConfig).GetValue().Length == 0)
            {
                config.SelectConfigVideoFolder(this);
                config.SaveConfig();
            }

            skylanderNameDatabase.CheckForVideoFiles(config.GetConfigItemString(KioskConfig.videoDirectoryConfig).GetValue());

            KioskConfig.ConfigValueInteger cfgItm = config.GetConfigItemInteger(KioskConfig.windowFullScreen);

            if (cfgItm != null)
            {
                SetFullScreen(cfgItm.GetValue() == 1);
            }

            cfgItm = config.GetConfigItemInteger(KioskConfig.displaySkylnaderNames);

            if (cfgItm != null)
            {
                displaySkylanderNames = (cfgItm.GetValue() == 1);
            }

            Width = config.GetConfigItemInteger(KioskConfig.windowSizeXConfig).GetValue();
            Height = config.GetConfigItemInteger(KioskConfig.windowSizeYConfig).GetValue();

            Title = "Kiosk";

            mySkylanderPortal = new SkylanderPortal();
            mySkylanderPortal.Connect();
            mySkylanderPortal.Activate();

            for (int i = 0; i < skylanderNames.Length; i++)
            {
                skylanderNames[i] = new TextBlock();
                skylanderNames[i].FontFamily = new FontFamily("Verdana");
                skylanderNames[i].FontWeight = FontWeights.Bold;
                skylanderNames[i].FontSize = 20;
                skylanderNames[i].TextAlignment = TextAlignment.Center;
                skylanderNames[i].HorizontalAlignment = HorizontalAlignment.Center;
                skylanderNames[i].Foreground = Brushes.White;
                namesStack.Children.Add(skylanderNames[i]);
            }

            animator = new DispatcherTimer(DispatcherPriority.Render);
            animator.Interval = TimeSpan.FromMilliseconds(15);
            animator.Tick += (s, args) =>
            {
                try
                {
                    Update(NowMillis());
                }
                catch (Exception ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            };
            animator.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                config.GetConfigItemInteger(KioskConfig.windowFullScreen).SetValue(IsFullScreen() ? 1 : 0);
                config.GetConfigItemInteger(KioskConfig.displaySkylnaderNames).SetValue(IsFullScreen() ? 1 : 0);
                config.SaveConfig();
            }
            catch (IOException ex)
            {
                throw new Exception(ex.Message, ex);
            }
            finally
            {
                if (mySkylanderPortal != null)
                {
                    mySkylanderPortal.Shutdown();
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs keyEvent)
        {
            if (keyEvent.Key == Key.Space)
            {
                if (mediaReady)
                {
                    Debug.WriteLine(currentMediaSourceString);
                    Debug.WriteLine(mediaView.Position);
                    Debug.WriteLine(mediaView.NaturalDuration);

                    Debug.WriteLine("Portal Status = " + mySkylanderPortal.Status());

                    mediaView.Position = TimeSpan.Zero;
                }
            }
            else if (keyEvent.Key == Key.C)
            {
                config.SelectConfigVideoFolder(this);
                skylanderNameDatabase.CheckForVideoFiles(config.GetConfigItemString(KioskConfig.videoDirectoryConfig).GetValue());
            }
            else if (keyEvent.Key == Key.F)
            {
                SetFullScreen(!IsFullScreen());
            }
            else if (keyEvent.Key == Key.Add)
            {
                KioskConfig.ConfigValueDouble cfgItm = config.GetConfigItemDouble(KioskConfig.mediaVolumeConfig);
                if (cfgItm != null)
                {
                    cfgItm.SetValue(Math.Min(1.0, cfgItm.GetValue() + 0.1));
                    mediaView.Volume = cfgItm.GetValue();
                }
            }
            else if (keyEvent.Key == Key.Subtract)
            {
                KioskConfig.ConfigValueDouble cfgItm = config.GetConfigItemDouble(KioskConfig.mediaVolumeConfig);
                if (cfgItm != null)
                {
                    cfgItm.SetValue(Math.Max(0, cfgItm.GetValue() - 0.1));
                    mediaView.Volume = cfgItm.GetValue();
                }
            }
            else if (keyEvent.Key == Key.Tab)
            {
                displaySkylanderNames = !displaySkylanderNames;
                keyEvent.Handled = true; 
            }
            else if (keyEvent.Key == Key.Down)
            {
                int index = mySkylanderPortal.skylandersOnPortal.IndexOf(currentSkylanderPlayingMedia);
                if (index != -1)
                {
                    index = (index + 1) % mySkylanderPortal.skylandersOnPortal.Count;
                    currentSkylanderPlayingMedia = mySkylanderPortal.skylandersOnPortal[index];
                    UpdateDisplaySkylander(true);
                }
            }
            else if (keyEvent.Key == Key.Up)
            {
                int index = mySkylanderPortal.skylandersOnPortal.IndexOf(currentSkylanderPlayingMedia);
                if (index != -1)
                {
                    index = (index - 1);
                    if (index < 0)
                        index = mySkylanderPortal.skylandersOnPortal.Count - 1;

                    currentSkylanderPlayingMedia = mySkylanderPortal.skylandersOnPortal[index];
                    UpdateDisplaySkylander(false);
                }
            }
        }

        void UpdateDisplayNames()
        {
            for (int textIndex = 0; textIndex < skylanderNames.Length; textIndex++)
            {
                skylanderNames[textIndex].Foreground = Brushes.White;
                if (textIndex < mySkylanderPortal.skylandersOnPortal.Count)
                {
                    SkylanderPortal.SkylanderOnPortal skylander = mySkylanderPortal.skylandersOnPortal[textIndex];

                    if (skylander != null)
                    {
                        SkylanderNameDatabase.SkylanderEntry nameEntry =
                            skylanderNameDatabase.GetSkylanderEntry(skylander.characterId, skylander.variantId);

                        if (nameEntry != null)
                        {
                            skylanderNames[textIndex].Text = nameEntry.GetName() + " " + skylander.GetMoney();
                            skylanderNames[textIndex].Foreground =
                                (nameEntry.GetHasVideo() == SkylanderNameDatabase.SkylanderEntry.VideoFileStatus.MISSING
                                    ? Brushes.Red : Brushes.White);
                        }
                        else
                        {
                            skylanderNames[textIndex].Text = "Unknown Skylander " + skylander.characterId + " " + skylander.variantId;
                        }

                        if (skylander == currentSkylanderPlayingMedia)
                        {
                            skylanderNames[textIndex].Foreground = Brushes.Green;
                        }
                    }
                    else
                    {
                        skylanderNames[textIndex].Text = "";
                    }
                }
                else
                {
                    skylanderNames[textIndex].Text = "";
                }
            }
        }

        async Task Update(long time)
        {
            long currentTime = NowMillis();

            mySkylanderPortal.MonitorConnection();

            if (mediaReady)
            {
                mediaView.Volume = config.GetConfigItemDouble(KioskConfig.mediaVolumeConfig).GetValue();

                mediaView.Play();
            }

            groupDisplayName.Visibility = displaySkylanderNames ? Visibility.Visible : Visibility.Hidden;

            if (!mySkylanderPortal.PortalConnected)
            {
                Title = "Skylanders Kiosk " + "(Portal Not Connected)";
                Idle();
            }

            if (currentTime - lastTime < 50)
            {
                return;
            }

            foreach (var skylander in mySkylanderPortal.skylandersOnPortal)
            {
                var newEntry = new SkylanderCollectionTracker.SkylanderData { CharacterId = skylander.characterId, VariantId = skylander.variantId, Xp = skylander.GetExperience(), Gold = skylander.GetMoney(), HatId = skylander.GetHatId(), decoId = skylander.GetDecoId(), cumulativeTime = skylander.GetCumulativeTime(), areaSequence = skylander.DetermineAreaInUse(), platform = skylander.GetPlatform(), nickname = skylander.GetNickname(), LastUsedDate = skylander.GetLastUsedDate() };
                await SkylanderCollectionTracker.SaveFigureData(skylander.GetYearCode(), skylander.GetDecoId(), newEntry);
            }

            if (!mySkylanderPortal.UpdateStatus())
            {
                return;
            }

            int numOnPortal = mySkylanderPortal.UpdateSkylandersOnPortal();

            lastTime = currentTime;

            if (numOnPortal == 0 || !mySkylanderPortal.PortalConnected)
            {
                if (currentMediaState != MediaState.PORTAL_IDLE)
                {
                    Idle();
                }
            }
            else
            {
                SelectMedia();
            }

            Title = "Skylanders Kiosk " + "(Portal Connected (" + numOnPortal + "))";
        }

        void SetupMediaPlayer(string mediaURL)
        {
            currentMediaSourceString = mediaURL;
            mediaReady = false;

            mediaView.Stop();
            mediaView.Close();

            mediaView.Source = new Uri(mediaURL);
            mediaView.Stretch = Stretch.Fill;
            mediaView.Play();
        }

        void UpdateDisplaySkylander(bool searchDown)
        {
            if (!mySkylanderPortal.skylandersOnPortal.Contains(currentSkylanderPlayingMedia))
            {
                currentSkylanderPlayingMedia = null;
            }

            if (mySkylanderPortal.skylandersOnPortal.Count != 0)
            {
                SkylanderPortal.SkylanderOnPortal startSkylander =
                    (currentSkylanderPlayingMedia == null ? mySkylanderPortal.skylandersOnPortal[0] : currentSkylanderPlayingMedia);
                SkylanderPortal.SkylanderOnPortal currentSkylander = null;
                int index = mySkylanderPortal.skylandersOnPortal.IndexOf(startSkylander);

                while (startSkylander != currentSkylander)
                {
                    if (currentSkylander == null)
                    {
                        currentSkylander = startSkylander;
                    }

                    FileInfo videoFile = GetVideoFileForSkylander(currentSkylander);

                    if (videoFile != null)
                    {
                        string url = new Uri(videoFile.FullName).ToString();

                        if (currentSkylander == currentSkylanderPlayingMedia
                            && currentMediaSourceString != null
                            && currentMediaSourceString.Contains(url))
                            break;

                        currentIsIdle = false;
                        SetupMediaPlayer(url);

                        currentSkylanderPlayingMedia = currentSkylander;
                        currentMediaState = MediaState.PORTAL_CHARACTER;

                        break;
                    }

                    if (searchDown)
                    {
                        index = (index + 1) % mySkylanderPortal.skylandersOnPortal.Count;
                    }
                    else
                    {
                        index = index - 1;
                        if (index < 0)
                        {
                            index = mySkylanderPortal.skylandersOnPortal.Count - 1;
                        }
                    }
                    currentSkylander = mySkylanderPortal.skylandersOnPortal[index];
                }
            }

            UpdateDisplayNames();
        }

        FileInfo GetVideoFileForSkylander(SkylanderPortal.SkylanderOnPortal skylander)
        {
            SkylanderNameDatabase.SkylanderEntry se =
                skylanderNameDatabase.GetSkylanderEntry(skylander.characterId, skylander.variantId);

            if (se == null || se.GetHasVideo() == SkylanderNameDatabase.SkylanderEntry.VideoFileStatus.MISSING)
            {
                return null;
            }

            FileInfo selectedFile = new FileInfo(
                config.GetConfigItemString(KioskConfig.videoDirectoryConfig).GetValue() + "\\" + se.GetVideoFilename());

            if (!selectedFile.Exists)
            {
                return null;
            }

            return selectedFile;
        }

        void SelectMedia()
        {
            UpdateDisplaySkylander(true);
        }

        void Idle()
        {
            if (currentMediaState == MediaState.PORTAL_IDLE)
                return;

            UpdateDisplayNames();

            currentMediaState = MediaState.PORTAL_IDLE;

            int nextVideo = rand.Next(3) + 1;
            while (nextVideo == lastIdleVideo)
            {
                nextVideo = rand.Next(3) + 1;
            }

            lastIdleVideo = nextVideo;
            string file = "\\" + nextVideo + ".mp4";

            FileInfo selectedFile = new FileInfo(
                config.GetConfigItemString(KioskConfig.videoDirectoryConfig).GetValue() + file);

            string url = new Uri(selectedFile.FullName).ToString();

            currentIsIdle = true;
            SetupMediaPlayer(url);
        }


        private void MediaView_MediaOpened(object sender, RoutedEventArgs e)
        {
            mediaReady = true;
            mediaView.Play();

            Debug.WriteLine(currentMediaSourceString);
            Debug.WriteLine(mediaView.Position);
            Debug.WriteLine(mediaView.NaturalDuration);
        }

        private void MediaView_MediaEnded(object sender, RoutedEventArgs e)
        {
            if (currentIsIdle)
            {
                currentMediaState = MediaState.PORTAL_INIT;
                Idle();
            }
            else
            {
                mediaView.Position = TimeSpan.Zero;
                mediaView.Play();
            }
        }

        private void MediaView_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Debug.WriteLine("OnError:" + (e.ErrorException != null ? e.ErrorException.Message : "unknown"));
            if (currentMediaSourceString != null)
            {
                SetupMediaPlayer(currentMediaSourceString);
            }
        }


        private bool IsFullScreen()
        {
            return WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized;
        }

        private void SetFullScreen(bool full)
        {
            if (full)
            {
                WindowStyle = WindowStyle.None;
                ResizeMode = ResizeMode.NoResize;
                WindowState = WindowState.Maximized;
                Topmost = true;
            }
            else
            {
                Topmost = false;
                WindowStyle = WindowStyle.SingleBorderWindow;
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.NoResize;
            }
        }
    }
}
