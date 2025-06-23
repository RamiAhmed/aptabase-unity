using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Providers;
using AptabaseSDK.TinyJson;
using UnityEngine;
using Utils;

namespace AptabaseSDK.Services
{
    public class DefaultDispatcher : IDispatcher
    {
        protected const string EventsEndpoint = "/api/v0/events";
        protected const int MaxBatchSize = 25;

        protected readonly string _apiUrl;
        protected readonly Queue<Data.Event> _events;
        protected readonly List<Data.Event> _failedEvents;
        protected readonly int _maxBatchSize;
        protected readonly List<Data.Event> _pendingEvents;
        protected readonly AptabaseSettings _settings;

        protected bool _flushInProgress;

        public DefaultDispatcher(
            IHostProvider hostProvider,
            AptabaseSettings settings,
            int maxBatchSize = MaxBatchSize)
        {
            _settings = settings;
            _maxBatchSize = maxBatchSize;

            _events = new Queue<Data.Event>(40);
            _failedEvents = new List<Data.Event>(10);
            _pendingEvents = new List<Data.Event>(MaxBatchSize);

            _apiUrl = $"{hostProvider.GetHost()}{EventsEndpoint}";
        }

        public virtual async Task Flush(CancellationToken cancellationToken)
        {
            if (_flushInProgress || _events.Count == 0)
                return;

            _flushInProgress = true;
            _failedEvents.Clear();

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

                    if (await TrySendPendingEvents(cancellationToken))
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

            _flushInProgress = false;
        }

        public virtual void Enqueue(Data.Event data)
        {
            _events.Enqueue(data);
        }

        protected virtual Task<bool> TrySendPendingEvents(CancellationToken cancellationToken)
        {
            return WebRequestUtil.CreateAndSendWebRequestAsync(
                _apiUrl,
                _settings.AppKey,
                _pendingEvents.ToJson(),
                cancellationToken);
        }
    }
}