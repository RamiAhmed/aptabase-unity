using UnityEngine;

namespace AptabaseSDK.Data
{
    public class VersionInfo
    {
        private static readonly string SDKVersion = $"Aptabase.Unity@{typeof(VersionInfo).Assembly.GetName().Version}";
        
        public string AppBuildNumber;
        public string AppVersion;

        public VersionInfo(string appVersion = null, string appBuildNumber = null)
        {
            AppVersion = appVersion ?? Application.version;
            AppBuildNumber = appBuildNumber ?? Application.buildGUID;
        }

        public string SdkVersion { get; } = SDKVersion;
    }
}