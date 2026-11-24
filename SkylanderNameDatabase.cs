using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;

namespace Com.Skylanders.Kiosk
{
    public class SkylanderNameDatabase
    {
        public class SkylanderEntry
        {
            public SkylanderEntry() { }

            public SkylanderEntry(int cId, int vId)
            {
                characterId = cId;
                variantId = vId;
            }

            public enum VideoFileStatus
            {
                UNKNOWN,
                MISSING,
                PRESENT
            }

            internal int characterId;
            internal int variantId;
            internal string name;
            internal string category;
            internal VideoFileStatus videoStatus = VideoFileStatus.UNKNOWN;
            internal string videoFilename = "";

            internal int GetCharacterId() { return characterId; }
            internal int GetVariantId() { return variantId; }
            internal string GetName() { return name; }
            internal string GetCategory() { return category; }

            internal VideoFileStatus GetHasVideo() { return videoStatus; }
            internal string GetVideoFilename() { return videoFilename; }
        }

        internal List<SkylanderEntry> skylanders = new List<SkylanderEntry>();

        public SkylanderNameDatabase(string databasePath)
        {
            LoadDatabase(databasePath);
        }

        internal bool LoadDatabase(string databasePath)
        {
            skylanders.Clear();

            try
            {
                Stream inStream = OpenResource(databasePath);
                if (inStream == null)
                {
                    return false;
                }

                using (inStream)
                using (StreamReader br = new StreamReader(inStream))
                {
                    string line = br.ReadLine();

                    while (line != null)
                    {
                        if (line.Length >= 2 && line[0] == '/' && line[1] == '/')
                        {
                            line = br.ReadLine();
                            continue;
                        }

                        string[] tokens = line.Split(',');

                        SkylanderEntry sl = new SkylanderEntry();

                        sl.category = tokens[0];
                        sl.name = tokens[1];
                        sl.characterId = int.Parse(tokens[2], CultureInfo.InvariantCulture);
                        sl.variantId = int.Parse(tokens[3], CultureInfo.InvariantCulture);
                        sl.videoFilename = sl.characterId + "-" + sl.variantId + ".mp4";

                        skylanders.Add(sl);

                        line = br.ReadLine();
                    }
                }
            }
            catch (IOException)
            {
                return false;
            }
            catch (FormatException e)
            {
                throw new Exception(e.Message, e);
            }

            return true;
        }

        private static Stream OpenResource(string databasePath)
        {
            string name = databasePath.TrimStart('/');
            Assembly asm = Assembly.GetExecutingAssembly();

            Stream s = asm.GetManifestResourceStream(name);
            if (s != null) return s;

            foreach (string resName in asm.GetManifestResourceNames())
            {
                if (resName.EndsWith(name, StringComparison.OrdinalIgnoreCase))
                {
                    return asm.GetManifestResourceStream(resName);
                }
            }

            return null;
        }

        internal SkylanderEntry GetSkylanderEntry(int characerId, int variantId)
        {
            foreach (SkylanderEntry se in skylanders)
            {
                if (se.characterId == characerId && se.variantId == variantId)
                {
                    return se;
                }
            }

            return null;
        }

        internal void CheckForVideoFiles(string directory)
        {
            foreach (SkylanderEntry se in skylanders)
            {
                FileInfo selectedFile = new FileInfo(directory + "\\" + se.characterId + "-" + se.variantId + ".mp4");
                se.videoStatus = !selectedFile.Exists ? SkylanderEntry.VideoFileStatus.MISSING : SkylanderEntry.VideoFileStatus.PRESENT;
            }
        }
    }
}
