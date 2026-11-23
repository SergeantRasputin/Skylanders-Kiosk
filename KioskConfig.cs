using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace Com.Skylanders.Kiosk
{
    
    public class KioskConfig
    {
        private double volume = 1.0;
        private string videoDirectory;
        private string configFileName;

        internal Dictionary<string, ConfigValue> configItems = new Dictionary<string, ConfigValue>();

        internal string GetVideoDirectory() { return videoDirectory; }
        internal double GetVolume() { return volume; }
        internal void SetVolume(double newVolume) { volume = newVolume; }

        internal class ConfigValue
        {
            protected string tag;

            internal ConfigValue() { }
            internal ConfigValue(string tag) { this.tag = tag; }

            internal virtual string GetLine() { return ""; }

            internal string GetTag() { return tag; }
            internal void SetTag(string tag) { this.tag = tag; }

            internal bool ContainsTag(string line) { return line.StartsWith(tag); }
            internal virtual void ReadValue(string line) { }
        }

        internal class ConfigValueDouble : ConfigValue
        {
            private double value;

            internal ConfigValueDouble(double value) { this.value = value; }

            internal override string GetLine()
            {
                return GetTag() + "=" + GetValue().ToString(CultureInfo.InvariantCulture);
            }

            internal override void ReadValue(string line)
            {
                string[] tokens = line.Split('=');
                value = double.Parse(tokens[1].Trim(), CultureInfo.InvariantCulture);
            }

            internal double GetValue() { return value; }
            internal void SetValue(double newValue) { value = newValue; }
        }

        internal class ConfigValueString : ConfigValue
        {
            internal string value;

            internal ConfigValueString(string value) { this.value = value; }

            internal override string GetLine() { return GetTag() + "=" + GetValue(); }

            internal override void ReadValue(string line)
            {
                string[] tokens = line.Split('=');
                value = tokens[1].Trim();
            }

            internal string GetValue() { return value; }
            internal void SetValue(string newValue) { value = newValue; }
        }

        internal class ConfigValueInteger : ConfigValue
        {
            private int value;

            internal ConfigValueInteger(int value) { this.value = value; }

            internal override string GetLine()
            {
                return GetTag() + "=" + GetValue().ToString(CultureInfo.InvariantCulture);
            }

            internal override void ReadValue(string line)
            {
                string[] tokens = line.Split('=');
                value = int.Parse(tokens[1].Trim(), CultureInfo.InvariantCulture);
            }

            internal int GetValue() { return value; }
            internal void SetValue(int newValue) { value = newValue; }
        }

        public static string videoDirectoryConfig = "[VideoDirectory]";
        public static string mediaVolumeConfig = "[MediaVolume]";
        public static string windowSizeXConfig = "[WindowSizeX]";
        public static string windowSizeYConfig = "[WindowSizeY]";
        public static string windowFullScreen = "[FullScreen]";
        public static string displaySkylnaderNames = "[DisplaySkylanderNames]";

        public KioskConfig()
        {
            AddConfigItem(videoDirectoryConfig, new ConfigValueString(""));
            AddConfigItem(mediaVolumeConfig, new ConfigValueDouble(1.0));
            AddConfigItem(windowSizeXConfig, new ConfigValueInteger(1920));
            AddConfigItem(windowSizeYConfig, new ConfigValueInteger(1080));
            AddConfigItem(windowFullScreen, new ConfigValueInteger(1));
            AddConfigItem(displaySkylnaderNames, new ConfigValueInteger(1));
        }

        private void AddConfigItem(string name, ConfigValue value)
        {
            value.SetTag(name);
            configItems[name] = value;
        }

        internal ConfigValueString GetConfigItemString(string tag)
        {
            ConfigValue itm;
            configItems.TryGetValue(tag, out itm);
            return itm as ConfigValueString;
        }

        internal ConfigValueDouble GetConfigItemDouble(string tag)
        {
            ConfigValue itm;
            configItems.TryGetValue(tag, out itm);
            return itm as ConfigValueDouble;
        }

        internal ConfigValueInteger GetConfigItemInteger(string tag)
        {
            ConfigValue itm;
            configItems.TryGetValue(tag, out itm);
            return itm as ConfigValueInteger;
        }

        public void SelectConfigVideoFolder(Window stage)
        {
            ConfigValueString cfgItm = GetConfigItemString(videoDirectoryConfig);

            if (cfgItm != null)
            {
                OpenFolderDialog chooser = new OpenFolderDialog();
                chooser.Title = "Select Video Folder";

                chooser.InitialDirectory = "c:";

                bool? ok = chooser.ShowDialog(stage);

                if (ok == true && !string.IsNullOrEmpty(chooser.FolderName))
                {
                    cfgItm.SetValue(chooser.FolderName);
                    videoDirectory = chooser.FolderName;
                }
            }
        }

        public void LoadConfig(string configFile)
        {
            configFileName = configFile;

            try
            {
                using (StreamReader br = new StreamReader(configFile))
                {
                    string line = br.ReadLine();

                    while (line != null)
                    {
                        foreach (ConfigValue cfgItm in configItems.Values)
                        {
                            if (cfgItm.ContainsTag(line))
                            {
                                cfgItm.ReadValue(line);
                            }
                        }

                        line = br.ReadLine();
                    }
                }
            }
            catch (IOException)
            {

            }
        }

        public void SaveConfig()
        {
            try
            {
                using (StreamWriter fileWriter = new StreamWriter(configFileName))
                {
                    foreach (ConfigValue cfgItm in configItems.Values)
                    {
                        string write = cfgItm.GetLine();
                        fileWriter.Write(write + "\n");
                    }

                    fileWriter.Flush();
                }
            }
            catch (IOException)
            {

            }
        }
    }
}
