using System;
using System.Collections.Generic;
using System.Reflection;

namespace AptabaseSDK.Viewer
{
    public class AptabaseParsedAnalyticsEvent
    {
        public static readonly string[] AptabaseHeaders =
        {
            nameof(AptabaseAnalyticsEvent.CountryCode),
            nameof(AptabaseAnalyticsEvent.CountryName),
            nameof(AptabaseAnalyticsEvent.RegionName),
            nameof(AptabaseAnalyticsEvent.Locale),
            nameof(AptabaseAnalyticsEvent.OSName),
            nameof(AptabaseAnalyticsEvent.OSVersion),
            nameof(AptabaseAnalyticsEvent.EngineName),
            nameof(AptabaseAnalyticsEvent.EngineVersion),
            nameof(AptabaseAnalyticsEvent.SessionId),
            nameof(AptabaseAnalyticsEvent.UserId)
        };

        private PropertyInfo[] _cachedProperties;
        private object _parsedEvent;

        public AptabaseAnalyticsEvent RawEvent { get; set; }

        public object ParsedEvent
        {
            get => _parsedEvent;
            set
            {
                _parsedEvent = value;
                _cachedProperties = _parsedEvent?.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    ?? Array.Empty<PropertyInfo>();
            }
        }

        public void Populate(Dictionary<string, object> data)
        {
            foreach (var prop in _cachedProperties)
            {
                if (prop.GetValue(_parsedEvent) is not null)
                    data.Add(prop.Name, prop.GetValue(ParsedEvent));
            }
            
            data.Add(nameof(AptabaseAnalyticsEvent.CountryCode), RawEvent.CountryCode);
            data.Add(nameof(AptabaseAnalyticsEvent.CountryName), RawEvent.CountryName);
            data.Add(nameof(AptabaseAnalyticsEvent.RegionName), RawEvent.RegionName);
            data.Add(nameof(AptabaseAnalyticsEvent.Locale), RawEvent.Locale);
            data.Add(nameof(AptabaseAnalyticsEvent.OSName), RawEvent.OSName);
            data.Add(nameof(AptabaseAnalyticsEvent.OSVersion), RawEvent.OSVersion);
            data.Add(nameof(AptabaseAnalyticsEvent.EngineName), RawEvent.EngineName);
            data.Add(nameof(AptabaseAnalyticsEvent.EngineVersion), RawEvent.EngineVersion);
            data.Add(nameof(AptabaseAnalyticsEvent.SessionId), RawEvent.SessionId);
            data.Add(nameof(AptabaseAnalyticsEvent.UserId), RawEvent.UserId);
        }
    }
}