using System.Globalization;
using UnityEngine;

namespace AptabaseSDK.Data
{
    public class Environment
    {
        public string AppBuildNumber;
        public string AppVersion;
        
        public string EngineName;
        public string EngineVersion;

        public bool IsDebug;
        
        public string Locale;

        public string OsName;
        public string OsVersion;
        public string SdkVersion;

        public Environment(
            VersionInfo versionInfo = null,
            OperatingSystemInfo osInfo = null,
            bool? isDebug = null)
        {
            EngineName = nameof(Unity);
            EngineVersion = Application.unityVersion;

            Locale = CultureInfo.CurrentCulture.Name;

            osInfo ??= OperatingSystemInfo.GetOperatingSystemInfo();
            OsName = osInfo.OsName;
            OsVersion = osInfo.OsVersion;

            versionInfo ??= new VersionInfo();
            AppVersion = versionInfo.AppVersion;
            AppBuildNumber = versionInfo.AppBuildNumber;
            SdkVersion = versionInfo.SdkVersion;

            IsDebug = (isDebug.HasValue && isDebug.Value) || Debug.isDebugBuild || Application.isEditor;
        }
    }
}