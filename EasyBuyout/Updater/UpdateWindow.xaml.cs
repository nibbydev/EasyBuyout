using System;
using System.Collections.Generic;
using System.Net;
using System.Web.Script.Serialization;
using EasyBuyout.Settings;

namespace EasyBuyout.Updater {
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow {
        private readonly Action<string, MainWindow.Flair> _log;
        private readonly WebClient _webClient;
        private readonly Config _config;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="config"></param>
        /// <param name="webClient"></param>
        /// <param name="log"></param>
        public UpdateWindow(Config config, WebClient webClient, Action<string, MainWindow.Flair> log) {
            _webClient = webClient;
            _log = log;
            _config = config;
            InitializeComponent();
        }

        /// <summary>
        /// Get latest release and show updater window if version is newer
        /// </summary>
        public void Run() {
            var releaseEntries = DownloadReleaseList();
            if (releaseEntries == null) {
                _log("Error checking new releases", MainWindow.Flair.Error);
                return;
            }

            // Compare versions of all releaseEntries and get newer ones
            var newReleases = CompareVersions(releaseEntries);

            // If there was a newer version available
            if (newReleases.Count <= 0) return;

            _log("New version available", MainWindow.Flair.Info);

            var patchNotes = "";
            foreach (var releaseEntry in newReleases) {
                patchNotes += $"{releaseEntry.tag_name}\n{releaseEntry.body}\n\n";
            }

            // Update UpdateWindow's elements
            Dispatcher.Invoke(() => {
                Label_NewVersion.Content = newReleases[0].tag_name;
                Label_CurrentVersion.Content = _config.ProgramVersion;

                HyperLink_URL.NavigateUri = new Uri(newReleases[0].html_url);
                HyperLink_URL_Direct.NavigateUri = new Uri(newReleases[0].assets[0].browser_download_url);

                TextBox_PatchNotes.AppendText(patchNotes);

                ShowDialog();
            });
        }

        /// <summary>
        /// Downloads releases from Github
        /// </summary>
        /// <returns>List of ReleaseEntry objects or null on failure</returns>
        private List<ReleaseEntry> DownloadReleaseList() {
            // WebClient can only handle one connection per instance.
            // It is bad practice to have multiple WebClients.
            while (_webClient.IsBusy) System.Threading.Thread.Sleep(10);

            try {
                var jsonString = _webClient.DownloadString(_config.GithubReleaseApi);
                return new JavaScriptSerializer().Deserialize<List<ReleaseEntry>>(jsonString);
            } catch (Exception ex) {
                _log(ex.ToString(), MainWindow.Flair.Error);
                return null;
            }
        }

        /// <summary>
        /// Compares provided entryList's release versions against own version
        /// </summary>
        /// <returns>Filtered (or empty) list of ReleaseEntry objects that are newer than own version</returns>
        private List<ReleaseEntry> CompareVersions(List<ReleaseEntry> releaseEntries) {
            var returnEntries = new List<ReleaseEntry>(releaseEntries.Count);

            foreach (var releaseEntry in releaseEntries) {
                var splitNew = releaseEntry.tag_name.Substring(1).Split('.');
                var splitOld = _config.ProgramVersion.Substring(1).Split('.');

                var minLen = splitNew.Length > splitOld.Length ? splitOld.Length : splitNew.Length;

                for (var i = 0; i < minLen; i++) {
                    int.TryParse(splitNew[i], out var newVer);
                    int.TryParse(splitOld[i], out var oldVer);

                    if (newVer > oldVer) {
                        returnEntries.Add(releaseEntry);
                        break;
                    }

                    if (newVer < oldVer) {
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
        /// Opens up the web browser when URL is clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HyperLink_URL_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
