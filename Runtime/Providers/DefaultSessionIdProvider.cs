using System;
using AptabaseSDK.Configuration;

namespace AptabaseSDK.Providers
{
    public class DefaultSessionIdProvider : ISessionIdProvider
    {
        private readonly TimeSpan _timeout;

        private DateTime _lastUpdate;
        private string _sessionId;

        public DefaultSessionIdProvider(AptabaseSettings settings)
            : this(TimeSpan.FromMinutes(settings.SessionTimeoutMinutes))
        {
        }

        public DefaultSessionIdProvider(TimeSpan timeout)
        {
            _timeout = timeout;

            ResetSessionId();
        }

        public string GetSessionId()
        {
            if (DateTime.UtcNow - _lastUpdate > _timeout)
                _sessionId = Guid.NewGuid().ToString();

            _lastUpdate = DateTime.UtcNow;
            return _sessionId;
        }

        public void ResetSessionId()
        {
            _sessionId = Guid.NewGuid().ToString();
            _lastUpdate = DateTime.UtcNow;
        }

        public void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
            _lastUpdate = DateTime.UtcNow;
        }
    }
}