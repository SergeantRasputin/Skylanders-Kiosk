using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows.Documents;
using System.Text.Json;
using System.Reflection.Metadata.Ecma335;

namespace Com.Skylanders.Kiosk
{
    public class SkylanderPortal
    {
        static int portalVendorId = 0x1430;
        static int portalProductId = 0x150;

        IntPtr deviceHandle = IntPtr.Zero;
        bool portalConnected = false;

        int lastSkylanderIndex = -1;
        int lastStatusRequest = -1;
        int lastReadSkylandersRequest = 0;

        long lastMonitor = 0;

        internal bool PortalConnected { get { return portalConnected; } }

        private static long NowMillis()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public class SkylanderOnPortal
        {
            internal int characterId = -1;
            internal int variantId = -1;
            internal int index = -1;
            internal int areaOffset = 0;

            internal int GetYearCode() { return (variantId >> 0xC); }
            internal int GetIsReposed() { return ((variantId >> 0xB) & 0x1); }
            internal int GetIsInGameVariant() { return ((variantId >> 0xA) & 0x1); }
            internal int GetHasLightCoreTech() { return ((variantId >> 0x9) & 0x1); }
            internal int GetIsSuperCharger() { return ((variantId >> 0xb) & 0x1); }
            internal int GetDecoId() { return variantId & 0xFF; }

            internal int GetMoney()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0x8, 0x24));
                return (decryptedBlock[0x4] << 8) | decryptedBlock[0x3];
            }
            internal int GetExperience()
            {
                byte[] decryptedBlock2011 = DecryptBlock(DetermineBlockToRead(0x8, 0x24));
                byte[] decryptedBlock20122013 = DecryptBlock(DetermineBlockToRead(0x11, 0x2D));
                int total2011 = (decryptedBlock2011[0x2] << 8) | (decryptedBlock2011[0x1] << 8) | (decryptedBlock2011[0x0]);
                int total2012 = (decryptedBlock20122013[0x4] << 8 | decryptedBlock20122013[0x3]);
                int Total2013 = (decryptedBlock20122013[0xB] << 24) | (decryptedBlock20122013[0xA] << 16) | (decryptedBlock20122013[0x9] << 8) | decryptedBlock20122013[0x8];
                return total2011 + total2012 + Total2013;
            }
            internal int GetCumulativeTime()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0x8, 0x24));
                return (((decryptedBlock[0x8] << 8) | decryptedBlock[0x7] << 8) | decryptedBlock[0x6] << 8 | decryptedBlock[0x5]);
            }
            internal int GetPlatform()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0x9, 0x25));
                Debug.WriteLine(decryptedBlock[0x3]);
                return (decryptedBlock[0x3]);
            }
            internal int GetHatId()
            {
                byte[] decryptedBlock2011 = DecryptBlock(DetermineBlockToRead(0x9, 0x25));
                int hatId2011 = (decryptedBlock2011[0x5] << 8 | decryptedBlock2011[0x4]);

                byte[] decryptedBlock2012 = DecryptBlock(DetermineBlockToRead(0x11, 0x2D));
                int hatId2012 = (decryptedBlock2012[0x5]);

                byte[] decryptedBlock20132014 = DecryptBlock(DetermineBlockToRead(0x11, 0x2D));
                int hatId20132014 = (decryptedBlock20132014[0xC]);

                byte[] decryptedBlock2015 = DecryptBlock(DetermineBlockToRead(0x11, 0x2D));
                int hatId2015 = (decryptedBlock2015[0xE]);

                if (hatId2011 > 0)
                {
                    return hatId2011;
                }
                if (hatId2012 > 0)
                {
                    return hatId2012;
                }
                if (hatId20132014 > 0)
                {
                    return hatId20132014;
                }
                else
                {
                    return hatId2015 + 256;
                }
            }
            internal string GetNickname()
            {
                byte[] decryptedBlockHalf1 = DecryptBlock(DetermineBlockToRead(0xA, 0x26));
                string nickname = System.Text.Encoding.Unicode.GetString(decryptedBlockHalf1);
                return nickname.TrimEnd('\0');
            }
            internal string GetLastUsedDate()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0xD, 0x29));
                
                int minute = (decryptedBlock[0x0]);
                int hour = (decryptedBlock[0x1]);
                int day = decryptedBlock[0x2];
                int month = decryptedBlock[0x3];
                int year = (decryptedBlock[0x5] << 8 | decryptedBlock[0x4]);
                return ("Last Used: " + hour + ":" + minute + " - " + day + "/" + month + "/" + year);
            }
            internal int GetHeroPoints()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0xD, 0x29));
                return (decryptedBlock[0xB] << 8 | decryptedBlock[0xA]);
            }
            internal string GetLastBuildDate()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0xD, 0x29));
                int year = 2000 + (decryptedBlock[0xC]);
                int month = (decryptedBlock[0xD]);
                int day = (decryptedBlock[0xE]);
                return ("Last Build Date: " + day + "/" + month + "/" + year);
            }
            internal int GetTimesOwnerChanged()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0xD, 0x29));
                return decryptedBlock[0xF];
            }
            internal string GetLastResetDate()
            {
                byte[] decryptedBlock = DecryptBlock(DetermineBlockToRead(0xE, 0x2A));
                int minute = (decryptedBlock[0x0]);
                int hour = (decryptedBlock[0x1]);
                int day = decryptedBlock[0x2];
                int month = decryptedBlock[0x3];
                int year = (decryptedBlock[0x5] << 8 | decryptedBlock[0x4]);
                return ("Last Reset: " + hour + ":" + minute + " - " + day + "/" + month + "/" + year);
            }

            internal byte DetermineAreaInUse()
            {
                byte[] decryptedBlock8 = DecryptBlock(0x8);
                byte[] decryptedBlock24 = DecryptBlock(0x24);

                if (decryptedBlock8[0x9] == 255 && decryptedBlock24[0x9] == 0)
                {
                    Debug.WriteLine("24");
                    return 0x24;
                }
                if (decryptedBlock8[0x9] == 0 && decryptedBlock24[0x9] == 255)
                {
                    Debug.WriteLine("8");
                    return 0x8;
                }
                if (decryptedBlock8[0x9] > decryptedBlock24[0x9])
                {
                    Debug.WriteLine("8");
                    return 0x8;
                }
                else
                {
                    Debug.WriteLine("24");
                    return 0x24;
                }
            }
            internal byte DetermineBlockToRead(byte block1, byte block2)
            {
                if(DetermineAreaInUse() == 0x24)
                {
                    return block2;
                }
                else
                {
                    return block1;
                }
            }

            internal byte[][] dataBlocks = CreateBlocks();

            private static byte[][] CreateBlocks()
            {
                byte[][] b = new byte[64][];
                for (int i = 0; i < 64; i++) b[i] = new byte[16];
                return b;
            }

            internal byte[] DecryptBlock(int block)
            {
                byte[] key = new byte[86];

                byte[] hashConst = new byte[] {
                    //I think this part is cool
                    0x20, 0x43, 0x6F, 0x70, 0x79, 0x72, 0x69, 0x67, 0x68, 0x74, 0x20, 0x28, 0x43, 0x29, 0x20, 0x32, // " Copyright (C) 2"
                    0x30, 0x31, 0x30, 0x20, 0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x2E, 0x20, // "010 Activision. "
                    0x41, 0x6C, 0x6C, 0x20, 0x52, 0x69, 0x67, 0x68, 0x74, 0x73, 0x20, 0x52, 0x65, 0x73, 0x65, 0x72, // "All Rights Reser"
                    0x76, 0x65, 0x64, 0x2E, 0x20 };                                                                  // "ved. "

                Array.Copy(dataBlocks[0], 0, key, 0, 16);
                Array.Copy(dataBlocks[1], 0, key, 16, 16);
                key[32] = (byte)block;
                Array.Copy(hashConst, 0, key, 33, 53);

                byte[] digest;
                using (MD5 md = MD5.Create())
                {
                    digest = md.ComputeHash(key);
                }

                using (Aes cipher = Aes.Create())
                {
                    cipher.Mode = CipherMode.ECB;             
                    cipher.Padding = PaddingMode.None;        
                    cipher.Key = digest;                      

                    using (ICryptoTransform dec = cipher.CreateDecryptor())
                    {
                        return dec.TransformFinalBlock(dataBlocks[block], 0, 16);
                    }
                }
            }
        }


        public List<SkylanderOnPortal> skylandersOnPortal = new List<SkylanderOnPortal>();


        public delegate bool ReadPortalCheckOperation(byte[] write, byte[] read);

        ReadPortalCheckOperation defaultCheckOperation =
            (write, read) => (write[0] == read[0]);

        public SkylanderPortal()
        {
            HidApi.hid_init();
        }

        public bool Connect()
        {
            if (portalConnected == true)
                return true;

            deviceHandle = HidApi.hid_open((ushort)portalVendorId, (ushort)portalProductId, IntPtr.Zero);

            if (deviceHandle != IntPtr.Zero)
            {
                portalConnected = true;
                HidApi.hid_set_nonblocking(deviceHandle, 1);

                Debug.WriteLine("Portal::Connected");
                return portalConnected;
            }

            Debug.WriteLine("Portal::Connect (No Portal Found)");
            return false;
        }

        public void Disconnect()
        {
            CloseHandle();
            portalConnected = false;
        }

        private void CloseHandle()
        {
            if (deviceHandle != IntPtr.Zero)
            {
                HidApi.hid_close(deviceHandle);
                deviceHandle = IntPtr.Zero;
            }
        }

        public void Shutdown()
        {
            CloseHandle();
            HidApi.hid_exit();
        }

        private void HandleDeviceError()
        {
        }

        bool WriteCommand(byte[] writeCommand, byte[] readCommand, ReadPortalCheckOperation checkOperation, int maxTries)
        {
            int tries = 0;

            if (portalConnected)
            {
                while (!checkOperation(writeCommand, readCommand))
                {
                    if (tries > maxTries)
                    {
                        return false;
                    }

                    byte[] outReport = new byte[0x21];
                    outReport[0] = 0x00;
                    Array.Copy(writeCommand, 0, outReport, 1, 0x20);
                    int w = HidApi.hid_write(deviceHandle, outReport, (UIntPtr)outReport.Length);
                    if (w < 0)
                    {
                        tries++;
                        continue;
                    }

                    int n = HidApi.hid_read(deviceHandle, readCommand, (UIntPtr)readCommand.Length);
                    if (n < 0)
                    {
                        HandleDeviceError();
                        return false;
                    }

                    tries++;
                }

                return true;
            }

            Debug.Write("Write Failed For Command: ");
            foreach (byte b in writeCommand)
            {
                Debug.Write(string.Format("{0:X2} ", b));
            }
            Debug.WriteLine("");

            return false;
        }

        public bool Activate()
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'A';
            writeCommand[1] = 0x1;

            return WriteCommand(writeCommand, readCommand, defaultCheckOperation, 500);
        }

        public bool Deactivate()
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'A';
            writeCommand[1] = 0x0;

            return WriteCommand(writeCommand, readCommand, defaultCheckOperation, 500);
        }

        public bool Reset()
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'R';

            return WriteCommand(writeCommand, readCommand, defaultCheckOperation, 500);
        }

        public bool UpdateStatus()
        {
            if (portalConnected == false)
            {
                return false;
            }

            int status = Status();

            bool changed = status != lastStatusRequest;

            lastStatusRequest = status;

            return changed;
        }

        public int UpdateSkylandersOnPortal()
        {
            int changes = lastReadSkylandersRequest ^ lastStatusRequest;
            byte[] dataBlocks = new byte[]
            {
                0, 1, 0x24, 0x8, 0x9, 0x25, 0x11, 0x2D, 0xA, 0x26, 0xC, 
                0x28, 0xD, 0x29, 0xE, 0x2A, 0x12, 0x2E, 0x14, 0x30
            };

            for (int i = 0; i < 16; i++)
            {
                int bitmask = 1 << (i * 2);
                if ((changes & bitmask) == bitmask)
                {
                    SkylanderOnPortal skylanderToRemove = null;

                    if ((lastReadSkylandersRequest & bitmask) == bitmask)
                    {
                        foreach (SkylanderOnPortal skylander in skylandersOnPortal)
                        {
                            if (skylander.index == i)
                            {
                                skylanderToRemove = skylander;
                            }
                        }

                        if (skylanderToRemove != null)
                        {
                            skylandersOnPortal.Remove(skylanderToRemove);
                        }
                    }
                    else
                    {
                        //reading a figure
                        SkylanderOnPortal skylander = new SkylanderOnPortal();
                        //the wall (what blocks need to be read)
                        for (int j = 0; j < dataBlocks.Length; j++)
                        {
                            ReadSkylander(i, j, skylander.dataBlocks[j]);
                            Thread.Sleep(50);
                        }

                        int money = skylander.GetMoney();
                        
                        byte[] character = new byte[4];
                        character[0] = 0;
                        character[1] = 0;
                        character[2] = skylander.dataBlocks[1][1];
                        character[3] = skylander.dataBlocks[1][0];

                        int characterIdOnPortal = (character[2] << 8) | character[3];

                        byte[] variant = new byte[4];
                        variant[0] = 0;
                        variant[1] = 0;
                        variant[2] = skylander.dataBlocks[1][13];
                        variant[3] = skylander.dataBlocks[1][12];

                        int variantIdOnPortal = (variant[2] << 8) | variant[3];

                        skylander.index = i;
                        skylander.characterId = characterIdOnPortal;
                        skylander.variantId = variantIdOnPortal;

                        int game = skylander.GetYearCode();
                        int decoId = skylander.GetDecoId();

                        byte[] nameBlock1 = skylander.DecryptBlock(0xA);
                        byte[] nameBlock2 = skylander.DecryptBlock(0xC);

                        byte[] all = new byte[30];
                        Array.Copy(nameBlock1, 0, all, 0, 16);
                        Array.Copy(nameBlock2, 0, all, 16, 14);

                        string name = Encoding.Unicode.GetString(all);

                        byte[] firstUsedBlock = skylander.DecryptBlock(0xE);

                        Debug.WriteLine("Name (Block 0xA) = " + name);

                        skylandersOnPortal.Add(skylander);
                    }
                }
            }

            if (skylandersOnPortal.Count != 0)
            {
                lastSkylanderIndex = skylandersOnPortal[0].index;
            }

            lastReadSkylandersRequest = lastStatusRequest;

            return skylandersOnPortal.Count;
        }

        bool ReadSkylander(int index, int block, byte[] blockData)
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'Q';
            writeCommand[1] = (byte)(0x10 + index);
            writeCommand[2] = (byte)(block);

            ReadPortalCheckOperation characterReadCheckOperation =
                (write, read) => ((read[0] == write[0] && (read[1] == write[1] || read[1] == 0x01) && read[2] == write[2]));

            if (WriteCommand(writeCommand, readCommand, characterReadCheckOperation, 500))
            {
                Debug.WriteLine("");

                foreach (byte b in readCommand)
                    Debug.Write(string.Format("{0:X2} ", b));

                Debug.WriteLine("");

                Array.Copy(readCommand, 3, blockData, 0, 16);

                return true;
            }

            return false;
        }

        public int Status()
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'S';

            if (WriteCommand(writeCommand, readCommand, defaultCheckOperation, 500))
            {
                byte[] id = new byte[4];
                id[0] = readCommand[4];
                id[1] = readCommand[3];
                id[2] = readCommand[2];
                id[3] = readCommand[1];

                int index = (id[0] << 24) | (id[1] << 16) | (id[2] << 8) | id[3];
                return index;
            }

            Debug.WriteLine("Portal Status Request FAILED");
            return -1;
        }

        public bool Colour(byte r, byte g, byte b)
        {
            byte[] writeCommand = new byte[0x20];
            byte[] readCommand = new byte[0x20];

            writeCommand[0] = (byte)'C';
            writeCommand[1] = r;
            writeCommand[2] = g;
            writeCommand[3] = b;

            return WriteCommand(writeCommand, readCommand, defaultCheckOperation, 500);
        }
        public void MonitorConnection()
        {
            long now = NowMillis();
            if (now - lastMonitor < 1000)
                return;
            lastMonitor = now;

            bool present = IsDevicePresent();

            if (present && portalConnected == false)
            {
                Connect();
                Reset();
                Activate();
            }
            else if (!present && portalConnected)
            {
                Debug.WriteLine("Portal::Disconnected");
                CloseHandle();
                portalConnected = false;
                skylandersOnPortal.Clear();
            }
        }

        private bool IsDevicePresent()
        {
            IntPtr list = HidApi.hid_enumerate((ushort)portalVendorId, (ushort)portalProductId);
            bool present = (list != IntPtr.Zero);
            if (list != IntPtr.Zero)
            {
                HidApi.hid_free_enumeration(list);
            }
            return present;
        }
    }
}
