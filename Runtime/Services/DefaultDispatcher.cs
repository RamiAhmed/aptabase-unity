using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Data;
using AptabaseSDK.Providers;
using AptabaseSDK.TinyJson;
using UnityEngine;
using Utils;

namespace AptabaseSDK.Services
{
    public class DefaultDispatcher : IDispatcher
    {
        private const string EventsEndpoint = "/api/v0/events";

        private readonly int _maxBatchSize;
        private readonly Queue<AptabaseEvent> _events;
        private readonly List<AptabaseEvent> _failedEvents;
        private readonly List<AptabaseEvent> _pendingEvents;

        protected readonly string ApiUrl;
        protected readonly AptabaseSettings Settings;

        private bool _flushInProgress;

        public DefaultDispatcher(
            IHostProvider hostProvider,
            AptabaseSettings settings,
            int maxBatchSize = 25)
        {
            Settings = settings;
            _maxBatchSize = maxBatchSize;

            _events = new Queue<AptabaseEvent>(50);
            _failedEvents = new List<AptabaseEvent>(20);
            _pendingEvents = new List<AptabaseEvent>(_maxBatchSize);

            ApiUrl = $"{hostProvider.GetHost()}{EventsEndpoint}";
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            if (_flushInProgress || _events.Count == 0)
                return;

            _flushInProgress = true;

            do
            {
                try
                {
                    // Dequeue events up to MaxBatchSize
                    var eventsCount = Mathf.Min(_maxBatchSize, _events.Count);
                    for (var i = 0; i < eventsCount; i++)
                    {
                        var evt = _events.Dequeue();
                        _pendingEvents.Add(evt);
                    }

                    if (await TrySendEvents(_pendingEvents, cancellationToken))
                        continue;

                    // If sending failed, add pending events to failed list
                    _failedEvents.AddRange(_pendingEvents);
                    _pendingEvents.Clear();
                }
                catch (OperationCanceledException)
                {
                    Debug.Log("Analytics flush operation was cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    _failedEvents.AddRange(_pendingEvents);
                    _pendingEvents.Clear();

                    Debug.LogWarning($"Error during analytics flush, retrying failed {_pendingEvents.Count} events");
                    Debug.LogWarning(ex);
                }
                finally
                {
                    // Dispose of pending events to free up resources
                    foreach (var evt in _pendingEvents)
                        evt.Dispose();

                    _pendingEvents.Clear();
                }
            } while (_events.Count > 0 && !cancellationToken.IsCancellationRequested);

            // Re-enqueue failed events for retry
            foreach (var evt in _failedEvents)
                _events.Enqueue(evt);

            _failedEvents.Clear();
            _flushInProgress = false;
        }

        public virtual void Enqueue(AptabaseEvent data)
        {
            _events.Enqueue(data);
        }

        protected virtual Task<bool> TrySendEvents(List<AptabaseEvent> events, CancellationToken cancellationToken)
        {
            return WebRequestUtil.CreateAndSendWebRequestAsync(
                ApiUrl,
                Settings.AppKey,
                events.ToJson(),
                cancellationToken);
        }
    }
}