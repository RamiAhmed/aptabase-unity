using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Providers;
using UnityEngine;
using Event = AptabaseSDK.Data.Event;

namespace AptabaseSDK.Services
{
    public class AptabaseService : IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEnvironmentProvider _environmentProvider;
        private readonly ISessionIdProvider _sessionIdProvider;
        private readonly AptabaseSettings _settings;

        private CancellationTokenSource _cancellationTokenSource;
        private TimeSpan _pollingInterval;

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

            var hp = hostProvider ?? new DefaultHostProvider(settings);
            _dispatcher = dispatcher ??
#if UNITY_WEBGL
                          new WebGLDispatcher(hp, _environmentProvider, settings);
#else
                          new DefaultDispatcher(hp, settings);
#endif
        }

        public void Dispose()
        {
            if (_settings.FlushOnDispose)
                Flush(CancellationToken.None).Wait();

            StopPolling();
        }

        public void TrackEvent(string eventName, Dictionary<string, object> eventProps = null)
        {
            var eventData = Event.Get();
            eventData.timestamp = DateTime.UtcNow.ToString("o");
            eventData.sessionId = _sessionIdProvider.GetSessionId();
            eventData.systemProps = _environmentProvider.Get();
            eventData.eventName = eventName;

            if (eventProps != null)
                foreach (var kvp in eventProps)
                    eventData.props.Add(kvp.Key, kvp.Value);

            _dispatcher.Enqueue(eventData);
        }

        public Task Flush(CancellationToken cancellationToken)
        {
            return _dispatcher.Flush(cancellationToken);
        }

        public void StartPolling(uint flushIntervalSeconds = 0)
        {
            if (!_settings.AutoFlush)
                return;

            StopPolling();

            _pollingInterval = TimeSpan.FromSeconds(
                flushIntervalSeconds != 0
                    ? flushIntervalSeconds
                    : _settings.FlushIntervalSeconds);

            _cancellationTokenSource = new CancellationTokenSource();
            Task.Run(PollingJob, _cancellationTokenSource.Token);
        }

        public void StopPolling()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }

        private async Task PollingJob()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
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