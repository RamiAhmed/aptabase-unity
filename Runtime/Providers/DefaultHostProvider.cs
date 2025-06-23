using System.Collections.Generic;
using AptabaseSDK.Configuration;
using UnityEngine;

namespace AptabaseSDK.Providers
{
    public class DefaultHostProvider : IHostProvider
    {
        protected static readonly Dictionary<string, string> Hosts = new()
        {
            { "US", "https://us.aptabase.com" },
            { "EU", "https://eu.aptabase.com" },
            { "DEV", "http://localhost:3000" },
            { "SH", "" }
        };

        protected readonly AptabaseSettings _settings;

        public DefaultHostProvider(AptabaseSettings settings)
        {
            _settings = settings;
        }

        public virtual string GetHost()
        {
            var key = _settings.AppKey;

            var parts = key.Split("-");
            if (parts.Length == 3 && Hosts.ContainsKey(parts[1]))
                return GetBaseUrl(parts[1]);

            Debug.LogWarning($"The Aptabase App Key {key} is invalid. Tracking will be disabled");
            return null;
        }

        protected virtual string GetBaseUrl(string region)
        {
            if (region != "SH")
                return Hosts[region];

            if (!string.IsNullOrWhiteSpace(_settings.SelfHostURL))
                return _settings.SelfHostURL;

            Debug.LogWarning(
                "Host parameter must be defined when using Self-Hosted App Key. Tracking will be disabled.");
            return null;
        }
    }
}