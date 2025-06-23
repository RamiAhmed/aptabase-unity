using System;
using AptabaseSDK.Configuration;

namespace AptabaseSDK.Providers
{
    public class DefaultSessionIdProvider : ISessionIdProvider
    {
        protected readonly TimeSpan _timeout;

        protected DateTime _lastUpdate;
        protected string _sessionId;

        public DefaultSessionIdProvider(AptabaseSettings settings)
            : this(TimeSpan.FromMinutes(settings.SessionTimeoutMinutes))
        {
        }

        public DefaultSessionIdProvider(TimeSpan timeout)
        {
            _timeout = timeout;

            _lastUpdate = DateTime.UtcNow;
            _sessionId = Guid.NewGuid().ToString();
        }

        public virtual string GetSessionId()
        {
            if (DateTime.UtcNow - _lastUpdate > _timeout)
                _sessionId = Guid.NewGuid().ToString();

            _lastUpdate = DateTime.UtcNow;
            return _sessionId;
        }

        public virtual void ResetSessionId()
        {
            _sessionId = Guid.NewGuid().ToString();
            _lastUpdate = DateTime.UtcNow;
        }

        public virtual void SetSessionId(string sessionId)
        {
            _sessionId = sessionId;
            _lastUpdate = DateTime.UtcNow;
        }
    }
}