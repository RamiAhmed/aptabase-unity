using UnityEngine;

namespace AptabaseSDK.Data
{
    public class OperatingSystemInfo
    {
        public string OsName;
        public string OsVersion;

        public static OperatingSystemInfo GetOperatingSystemInfo()
        {
            var operatingSystem = new OperatingSystemInfo
            {
                OsVersion = SystemInfo.operatingSystem
            };

            switch (Application.platform)
            {
                case RuntimePlatform.Android:
                    operatingSystem.OsName = "Android";
                    var index = operatingSystem.OsName.IndexOf('(');
                    if (index >= 0)
                    {
                        var trimmedVersion = operatingSystem.OsName[..index].Trim();
                        operatingSystem.OsName = trimmedVersion;
                    }

                    break;
                case RuntimePlatform.IPhonePlayer:
                    var model = SystemInfo.deviceModel.ToLower();
                    operatingSystem.OsName = model.Contains("ipad") ? "iPadOS" : "iOS";
                    break;
                case RuntimePlatform.LinuxPlayer:
                    operatingSystem.OsName = "Linux";
                    break;
                case RuntimePlatform.OSXPlayer:
                    operatingSystem.OsName = "macOS";
                    break;
                case RuntimePlatform.WebGLPlayer:
                    operatingSystem.OsName = string.Empty;
                    operatingSystem.OsName = string.Empty;
                    break;
                case RuntimePlatform.WindowsPlayer:
                    operatingSystem.OsName = "Windows";
                    break;
                default:
                    operatingSystem.OsName = Application.platform.ToString();
                    break;
            }

            return operatingSystem;
        }
    }
}