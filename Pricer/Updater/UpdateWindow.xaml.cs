using Pricer.Updater;
using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using System.Windows;

namespace Pricer {
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window {
        private readonly WebClient webClient;

        public UpdateWindow(WebClient webClient) {
            this.webClient = webClient;
            InitializeComponent();
        }

        /// <summary>
        /// Get latest release and show updater window if version is newer
        /// </summary>
        public void Run() {
            // Can't be null when making calls to github
            webClient.Headers.Add("user-agent", "!null");

            // Fix for https DonwloadString bug (https://stackoverflow.com/questions/28286086/default-securityprotocol-in-net-4-5)
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            // Make webrequest
            List<ReleaseEntry> releaseEntries = DownloadReleaseList();
            if (releaseEntries == null) {
                MainWindow.Log("[Updater] Error getting update info...", 2);
                return;
            }

            // Compare versions of all releaseEntries and get newer ones
            List<ReleaseEntry> newReleases = CompareVersions(releaseEntries);

            // If there was a newer version available
            if (newReleases.Count > 0) {
                MainWindow.Log("[Updater] New version available", 1);

                string patchNotes = "";
                foreach (ReleaseEntry releaseEntry in newReleases) {
                    patchNotes += releaseEntry.tag_name + "\n" + releaseEntry.body + "\n\n";
                }

                // Update UpdateWindow's elements
                Dispatcher.Invoke(() => {
                    Label_NewVersion.Content = newReleases[0].tag_name;
                    Label_CurrentVersion.Content = Settings.programVersion;

                    HyperLink_URL.NavigateUri = new Uri(newReleases[0].html_url);
                    HyperLink_URL_Direct.NavigateUri = new Uri(newReleases[0].assets[0].browser_download_url);

                    TextBox_PatchNotes.AppendText(patchNotes);

                    ShowDialog();
                });
            }
        }

        /// <summary>
        /// Downloads releases from Github's API
        /// </summary>
        /// <returns>List of ReleaseEntry objects or null on failure</returns>
        private List<ReleaseEntry> DownloadReleaseList() {
            // WebClient can only handle one connection per instance.
            // It is bad practice to have multiple WebClients.
            while (webClient.IsBusy) System.Threading.Thread.Sleep(10);

            try {
                string jsonString = webClient.DownloadString(Settings.githubReleaseAPI);
                return new JavaScriptSerializer().Deserialize<List<ReleaseEntry>>(jsonString);
            } catch (Exception ex) {
                Console.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Compares provided entryList's release versions against own version
        /// </summary>
        /// <returns>Filtered (or empty) list of ReleaseEntry objects that are newer than own version</returns>
        private List<ReleaseEntry> CompareVersions(List<ReleaseEntry> releaseEntries) {
            List<ReleaseEntry> returnEntries = new List<ReleaseEntry>(releaseEntries.Count);

            foreach (ReleaseEntry releaseEntry in releaseEntries) {
                string[] splitNew = releaseEntry.tag_name.Substring(1).Split('.');
                string[] splitOld = Settings.programVersion.Substring(1).Split('.');

                int minLen = splitNew.Length > splitOld.Length ? splitOld.Length : splitNew.Length;

                for (int i = 0; i < minLen; i++) {
                    int newVer = 0, oldVer = 0;

                    Int32.TryParse(splitNew[i], out newVer);
                    Int32.TryParse(splitOld[i], out oldVer);

                    if (newVer > oldVer) {
                        returnEntries.Add(releaseEntry);
                        break;
                    } else if (newVer < oldVer) {
                        break;
                    }
                }
            }

            return returnEntries;
        }

        //-----------------------------------------------------------------------------------------------------------
        // Events
        //-----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Opens up the webbrowser when URL is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HyperLink_URL_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
