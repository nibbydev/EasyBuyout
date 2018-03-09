using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;

namespace Pricer {
    /// <summary>
    /// Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window {
        private WebClient webClient;

        public UpdateWindow(WebClient webClient) {
            InitializeComponent();
            this.webClient = webClient;
        }

        /// <summary>
        /// Downloads list of releases from Github API and returns latest
        /// </summary>
        /// <returns>Latest release object</returns>
        private ReleaseObject GetLatestRelease() {
            try {
                string jsonString = webClient.DownloadString(Settings.programReleaseAPI);
                List<ReleaseObject> releaseList = new JavaScriptSerializer().Deserialize<List<ReleaseObject>>(jsonString);

                if (releaseList == null) return null;
                else return releaseList[0];
            } catch (Exception ex) {
                Console.WriteLine(ex);
            }

            return null;
        }

        /// <summary>
        /// Get latest release and show updater window if version is newer
        /// </summary>
        public void Run() {
            // Can't be null when making calls to github
            webClient.Headers.Add("user-agent", "!null");

            ReleaseObject latest = GetLatestRelease();

            if (latest == null) {
                MainWindow.Log("[Updater] Error getting update info...", 2);
                return;
            } else if (!CompareVersions(latest.tag_name)) {
                return;
            }

            Dispatcher.Invoke(() => {
                Label_NewVersion.Content = latest.tag_name;
                Label_CurrentVersion.Content = Settings.programVersion;
                HyperLink_URL.NavigateUri = new Uri(latest.html_url);
                HyperLink_URL_Direct.NavigateUri = new Uri(latest.assets[0].browser_download_url);
                MainWindow.Log("[Updater] New version available", 1);
                ShowDialog();
            });
        }

        /// <summary>
        /// Compares input and version in settings
        /// </summary>
        /// <param name="input">Version string (eg "v2.3.1")</param>
        /// <returns>True if current version is old</returns>
        private bool CompareVersions(string input) {
            string[] splitNew = input.Substring(1).Split('.');
            string[] splitOld = Settings.programVersion.Substring(1).Split('.');

            int oldLen = splitOld.Length;
            int newLen = splitNew.Length;

            int totLen;
            if (newLen > oldLen) totLen = newLen;
            else totLen = oldLen;

            for (int i = 0; i < totLen; i++) {
                int resultNew = 0, resultOld = 0;

                if (newLen > i) Int32.TryParse(splitNew[i], out resultNew);
                if (oldLen > i) Int32.TryParse(splitOld[i], out resultOld);

                if (resultNew > resultOld) return true;
            }

            return false;
        }

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

    sealed class ReleaseObject {
        public string html_url { get; set; }
        public string tag_name { get; set; }
        public string name { get; set; }
        public List<AssetObject> assets { get; set; }
    }

    sealed class AssetObject {
        public string name { get; set; }
        public string size { get; set; }
        public string browser_download_url { get; set; }
    }
}
