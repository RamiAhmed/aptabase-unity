using System;
using System.Collections.Generic;
using UnityEngine.Pool;

namespace AptabaseSDK.Data
{
    public class AptabaseEvent : IDisposable
    {
        // TODO: Pascal case fields?

        public string eventName;
        public string sessionId;
        public string timestamp;
        
        public Dictionary<string, object> props;
        public EnvironmentInfo systemProps;

        public static AptabaseEvent Get()
        {
            var evt = GenericPool<AptabaseEvent>.Get();
            evt.props = DictionaryPool<string, object>.Get();
            
            return evt;
        }

        public void Dispose()
        {
            systemProps = null;
            eventName = null;
            sessionId = null;
            timestamp = null;
            
            props.Clear();
            DictionaryPool<string, object>.Release(props);
            GenericPool<AptabaseEvent>.Release(this);
        }
    }
}