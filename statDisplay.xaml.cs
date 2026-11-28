using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Com.Skylanders.Kiosk
{
    public partial class statDisplay : Window
    {
        private DispatcherTimer refreshTimer;
        public statDisplay(SkylanderPortal mySkylanderPortal)
        {
            InitializeComponent();
            DisplayStats(mySkylanderPortal);
            refreshTimer = new DispatcherTimer();
            refreshTimer.Interval = TimeSpan.FromMilliseconds(200);
            refreshTimer.Tick += (sender, e) => RefreshTimer_Tick(mySkylanderPortal, 50);
            refreshTimer.Start();
        }
        private int lastStatusId = -1;

        private void DisplayStats(SkylanderPortal mySkylanderPortal)
        {
            foreach (var skylander in mySkylanderPortal.skylandersOnPortal)
            {
                Stats.Text = "\nNickname: " + skylander.GetNickname() + Environment.NewLine +
                             "Gold: " + skylander.GetMoney() + Environment.NewLine +
                             "Experience: " + skylander.GetExperience()+ Environment.NewLine +
                             "Time on the portal: " + skylander.GetCumulativeTime() + Environment.NewLine +
                             "Last place on the portal: " + skylander.GetLastUsedDate() + Environment.NewLine +
                             "PLatform used on: " + skylander.GetPlatform() + Environment.NewLine +
                             "Last place on the portal: " + skylander.GetLastUsedDate() + Environment.NewLine + Environment.NewLine + Environment.NewLine +
                             "Advanced Information" + Environment.NewLine +
                             "Character ID: " + skylander.characterId + Environment.NewLine +
                             "Variant ID: " + skylander.variantId + Environment.NewLine +
                             "Hat ID: " + skylander.GetHatId() + Environment.NewLine +
                             "Deco ID: " + skylander.GetDecoId() + Environment.NewLine +
                             "Area Sequence: " + skylander.DetermineAreaInUse() + Environment.NewLine;

            }
        }

        private void RefreshTimer_Tick(SkylanderPortal mySkylanderPortal, int number)
        {
            if (mySkylanderPortal == null) return;
            int currentStatusId = mySkylanderPortal.Status();
            if (currentStatusId != lastStatusId)
            {
                lastStatusId = currentStatusId;
                DisplayStats(mySkylanderPortal);
            }
        }




    }
}
