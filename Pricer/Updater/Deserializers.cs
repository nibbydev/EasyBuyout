using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pricer.Updater {
    /// <summary>
    /// Deserializer for Github's release API
    /// </summary>
    public sealed class ReleaseEntry {
        // Releases page
        public string html_url { get; set; }
        // Version tag
        public string tag_name { get; set; }
        // Release name
        public string name { get; set; }
        // Patchnotes
        public string body { get; set; }
        // Attached files
        public List<Asset> assets { get; set; }
    }

    /// <summary>
    /// Deserializer for Github's release API's assets
    /// </summary>
    public sealed class Asset {
        // Attachment's filename
        public string name { get; set; }
        // Size in bytes
        public string size { get; set; }
        // Direct download url
        public string browser_download_url { get; set; }
    }
}
