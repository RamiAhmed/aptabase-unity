using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Services;
using UnityEngine;

namespace Behaviours
{
    [DisallowMultipleComponent]
    public class AptabaseRunBehaviour : MonoBehaviour
    {
        [Tooltip("Drag and drop your AptabaseSettings asset here.")]
        public AptabaseSettings Settings;

        public static AptabaseRunBehaviour Instance { get; private set; }

        public AptabaseService Service { get; private set; }

        private void Awake()
        {
            if (Settings == null)
                throw new MissingReferenceException($"{this} missing {nameof(Settings)}");

            if (Instance != null)
                throw new Exception($"Only one instance of {nameof(AptabaseRunBehaviour)} is allowed in the scene.");

            if (Settings.Mode != AptabaseSettings.RunMode.MonoBehaviour)
                throw new Exception(
                    $"AptabaseSettings.Mode must be set to {AptabaseSettings.RunMode.MonoBehaviour} to use {nameof(AptabaseRunBehaviour)}.");

            Service = new AptabaseService(Settings);
            Instance = this;
        }

        private void OnDestroy()
        {
            Service?.Dispose();
            Instance = null;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                Service?.StartPolling();
            else
                Service?.StopPolling();
        }

        public void TrackEvent(string eventName, Dictionary<string, object> eventData = null)
        {
            Service.TrackEvent(eventName, eventData);
        }

        public Task Flush()
        {
            return Flush(destroyCancellationToken);
        }

        public Task Flush(CancellationToken cancellationToken)
        {
            return Service.Flush(cancellationToken);
        }
    }
}