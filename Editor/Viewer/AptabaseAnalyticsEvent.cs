using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace AptabaseSDK.Viewer
{
    public class AptabaseAnalyticsEvent
    {
        private static readonly JsonSerializerSettings DefaultSerializerSettings = new()
        {
            Converters = new List<JsonConverter>
            {
                new StringEnumConverter(new DefaultNamingStrategy()),
                // new Float2Converter(),
                // new Float3Converter(),
                // new Float4Converter(),
                // new ByteDotConverter(),
                // new SByteDotConverter(),
                // new ShortDotConverter(),
                // new UShortDotConverter(),
                // new IntDotConverter(),
                // new UIntDotConverter(),
                // new LongDotConverter(),
                // new ULongDotConverter(),
                // new FloatDotConverter(),
                // new DoubleDotConverter(),
                // new DecimalDotConverter()
            },
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.Utc,
            FloatFormatHandling = FloatFormatHandling.DefaultValue,
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            ContractResolver = new ContractResolver
            {
                IgnoreIsSpecifiedMembers = true
            }
        };

        [JsonProperty("timestamp")]
        public string Timestamp { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("session_id")]
        public string SessionId { get; set; }

        [JsonProperty("event_name")]
        public string EventName { get; set; }

        [JsonProperty("string_props")]
        public string EventProperties { get; set; }

        [JsonProperty("numeric_props")]
        public string NumericProperties { get; set; }

        [JsonProperty("os_name")]
        public string OSName { get; set; }

        [JsonProperty("os_version")]
        public string OSVersion { get; set; }

        [JsonProperty("locale")]
        public string Locale { get; set; }

        [JsonProperty("app_version")]
        public string AppVersion { get; set; }

        [JsonProperty("app_build_number")]
        public string AppBuildNumber { get; set; }

        [JsonProperty("engine_name")]
        public string EngineName { get; set; }

        [JsonProperty("engine_version")]
        public string EngineVersion { get; set; }

        [JsonProperty("country_code")]
        public string CountryCode { get; set; }

        [JsonProperty("country_name")]
        public string CountryName { get; set; }

        [JsonProperty("region_name")]
        public string RegionName { get; set; }

        public static object ParseEventProperties(Type type, AptabaseAnalyticsEvent evt)
        {
            var eventProps = JObject.Parse(evt.EventProperties.Replace(@"""""", @"""").Trim('\"'));
            var numericProps = JObject.Parse(evt.NumericProperties.Replace(@"""""", @"""").Trim('\"'));

            eventProps.Merge(numericProps, new()
            {
                MergeArrayHandling = MergeArrayHandling.Union,
                MergeNullValueHandling = MergeNullValueHandling.Ignore
            });

            var obj = JsonConvert.DeserializeObject(eventProps.ToString(), type, DefaultSerializerSettings);
            return obj;
        }

        public override string ToString() => $"{EventName}, {Timestamp}";

        private sealed class ContractResolver : DefaultContractResolver
        {
            protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
            {
                var property = base.CreateProperty(member, memberSerialization);
                if (property.Writable)
                    return property;

                var prop = member as PropertyInfo;
                if (prop == null)
                    return property;

                var hasSetter = prop.GetSetMethod(true) != null;
                if (hasSetter)
                    property.Writable = true;

                return property;
            }
        }
    }
}