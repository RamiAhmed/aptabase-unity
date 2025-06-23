using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Services;
using UnityEngine;

namespace AptabaseSDK.Utils
{
    public static class AptabaseUtil
    {
        private static AptabaseService _service;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void AutoInitialize()
        {
            var settings = Resources.Load<AptabaseSettings>(nameof(AptabaseSettings));
            if (settings != null && settings.Mode == AptabaseSettings.RunMode.Automatic)
                Initialize(settings);
        }
        
        public static void Initialize(AptabaseSettings settings)
        {
            if (_service != null)
            {
                Debug.LogError($"{nameof(AptabaseUtil)} is already initialized.");
                return;
            }

            _service = new AptabaseService(settings);
            _service.StartPolling();

            Application.quitting += OnQuit;
        }

        private static void OnQuit()
        {
            _service?.Dispose();
            _service = null;
        }

        public static void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
        {
            _service.TrackEvent(eventName, eventData);
        }

        public static Task Flush(CancellationToken cancellationToken)
        {
            return _service.Flush(cancellationToken);
        }
    }
}