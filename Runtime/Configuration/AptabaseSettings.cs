using Behaviours;
using UnityEngine;

namespace AptabaseSDK.Configuration
{
    [CreateAssetMenu(fileName = nameof(AptabaseSettings) + ".asset", menuName = "Aptabase/Settings")]
    public class AptabaseSettings : ScriptableObject
    {
        public enum RunMode
        {
            /// <summary>
            ///     Automatically run Aptabase in the background.
            /// </summary>
            Automatic,

            /// <summary>
            ///     Run Aptabase in the background using <see cref="AptabaseRunBehaviour" />.
            /// </summary>
            MonoBehaviour,

            /// <summary>
            ///     Use your own custom logic to run Aptabase.
            /// </summary>
            Manual
        }

        [Header("Aptabase")]
        [Tooltip(
            "Automatic will run Aptabase in the background automatically, MonoBehaviour will require a AptabaseRunBehaviour, and Manual will require you to handle running Aptabase yourself.")]
        public RunMode Mode = RunMode.Automatic;

        [Tooltip("How long to keep an inactive session alive before creating a new session ID.")] 
        [Min(1)]
        public int SessionTimeoutMinutes = 60;

        [Header("Hosting Settings")]
        [Tooltip("The App Key for your Aptabase project. This is required to send data to your project.")]
        public string AppKey = "A-EU-0000000000";

        [Tooltip("The URL for self-hosting Aptabase. If you are using the hosted version, leave this empty.")]
        public string SelfHostURL;

        [Header("Flush (Send) Settings")]
        [Tooltip(
            "Whether to automatically flush events after they are enqueued (i.e. allow AptabaseService to poll). If false, you will need to call Flush manually.")]
        public bool AutoFlush = true;

        [Tooltip(
            "Whether to flush events when the service is disposed. If false, you may want to call Flush manually before shutting down the application to avoid any loss of data.")]
        public bool FlushOnDispose = true;

        [Tooltip("The interval in seconds to flush events.")] 
        [Min(1)] 
        [Space]
        [SerializeField]
        private int _flushIntervalSeconds = 60;

        [Tooltip("The interval in seconds to flush events in the Unity Editor while developing.")] 
        [Min(1)]
        [SerializeField]
        private int _flushIntervalSecondsInEditor = 2;

        public int FlushIntervalSeconds =>
#if UNITY_EDITOR
            _flushIntervalSecondsInEditor;
#else
            _flushIntervalSeconds;
#endif
    }
}