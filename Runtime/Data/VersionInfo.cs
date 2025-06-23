using UnityEngine;

namespace AptabaseSDK.Data
{
    public class VersionInfo
    {
        private const string SDKVersion = "Aptabase.Unity@0.2.4"; // TODO: better way to manage this versioning?

        public VersionInfo(string appVersion = null, string appBuildNumber = null)
        {
            AppVersion = appVersion ?? Application.version;
            AppBuildNumber = appBuildNumber ?? Application.buildGUID;
        }

        public string SdkVersion { get; } = SDKVersion;
        public string AppVersion;
        public string AppBuildNumber;
    }
}