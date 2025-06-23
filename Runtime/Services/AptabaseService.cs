using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Providers;
using UnityEngine;

namespace AptabaseSDK.Services
{
    public class AptabaseService : IDisposable
    {
        protected readonly IDispatcher _dispatcher;
        protected readonly IEnvironmentProvider _environmentProvider;
        protected readonly IHostProvider _hostProvider;
        protected readonly ISessionIdProvider _sessionIdProvider;
        protected readonly AptabaseSettings _settings;

        protected CancellationTokenSource _cancellationTokenSource;
        protected TimeSpan _pollingInterval;

        public AptabaseService(
            AptabaseSettings settings,
            ISessionIdProvider sessionIdProvider = null,
            IEnvironmentProvider environmentProvider = null,
            IDispatcher dispatcher = null,
            IHostProvider hostProvider = null)
        {
            _settings = settings;

            _environmentProvider = environmentProvider ?? new DefaultEnvironmentProvider();
            _sessionIdProvider = sessionIdProvider ?? new DefaultSessionIdProvider(settings);
            _hostProvider = hostProvider ?? new DefaultHostProvider(settings);

            _dispatcher = dispatcher ??
#if UNITY_WEBGL
                          new WebGLDispatcher(_hostProvider, _environmentProvider, settings);
#else
                          new DefaultDispatcher(_hostProvider, settings);
#endif
        }

        public void Dispose()
        {
            if (_settings.FlushOnDispose)
                Flush(CancellationToken.None).Wait();

            StopPolling();
        }

        public virtual void TrackEvent(string eventName, Dictionary<string, object> eventProps = null)
        {
            var eventData = Data.Event.Get();
            eventData.timestamp = DateTime.UtcNow.ToString("o");
            eventData.sessionId = _sessionIdProvider.GetSessionId();
            eventData.systemProps = _environmentProvider.Get();
            eventData.eventName = eventName;

            if (eventProps != null)
                foreach (var kvp in eventProps)
                    eventData.props.Add(kvp.Key, kvp.Value);

            _dispatcher.Enqueue(eventData);
        }

        public virtual Task Flush(CancellationToken cancellationToken)
        {
            return _dispatcher.Flush(cancellationToken);
        }

        public virtual void StartPolling(uint flushIntervalSeconds = 0)
        {
            StopPolling();

            if (!_settings.AutoFlush)
                return;

            _pollingInterval = TimeSpan.FromSeconds(
                flushIntervalSeconds != 0
                    ? flushIntervalSeconds
                    : _settings.FlushIntervalSeconds);

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(PollingJob, _cancellationTokenSource.Token);
        }

        public virtual void StopPolling()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        protected virtual async Task PollingJob()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    await _dispatcher.Flush(_cancellationTokenSource.Token);
                    await Task.Delay(_pollingInterval, _cancellationTokenSource.Token);
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Analytics flushing cancelled");
                    break;
                }
                catch (Exception ex)
                {
                    Debug.LogError("Error during aptabase analytics polling");
                    Debug.LogError(ex);
                }
            }
        }
    }
}