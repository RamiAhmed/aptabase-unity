using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.TinyJson;
using UnityEngine;
using UnityEngine.Pool;

namespace AptabaseSDK
{
    public class Dispatcher : IDispatcher
    {
        private const string EVENTS_ENDPOINT = "/api/v0/events";

        private const int MAX_BATCH_SIZE = 25;

        private static string _apiURL;
        private static WebRequestHelper _webRequestHelper;
        private static string _appKey;
        private static EnvironmentInfo _environment;
        private readonly Queue<Event> _events;
        private readonly List<Event> _failedEvents;

        private bool _flushInProgress;

        public Dispatcher(string appKey, string baseURL, EnvironmentInfo env)
        {
            //create event queue
            _events = new Queue<Event>();
            _failedEvents = new List<Event>(10);

            //web request setup information
            _apiURL = $"{baseURL}{EVENTS_ENDPOINT}";
            _appKey = appKey;
            _environment = env;
            _webRequestHelper = new WebRequestHelper();
        }

        public void Enqueue(Event data)
        {
            _events.Enqueue(data);
        }

        public async Task Flush(CancellationToken cancellationToken)
        {
            if (_flushInProgress || _events.Count <= 0)
                return;

            _flushInProgress = true;
            _failedEvents.Clear();

            //flush all events
            do
            {
                var eventsToSend = ListPool<Event>.Get();

                try
                {
                    var eventsCount = Mathf.Min(MAX_BATCH_SIZE, _events.Count);
                    for (var i = 0; i < eventsCount; i++)
                        eventsToSend.Add(_events.Dequeue());

                    var result = await SendEvents(eventsToSend, cancellationToken);
                    if (!result)
                        _failedEvents.AddRange(eventsToSend);
                }
                catch
                {
                    _failedEvents.AddRange(eventsToSend);
                }
                finally
                {
                    foreach (var evt in eventsToSend.Except(_failedEvents))
                        DictionaryPool<string, object>.Release(evt.props);

                    ListPool<Event>.Release(eventsToSend);
                }
            } while (_events.Count > 0 && !cancellationToken.IsCancellationRequested);

            if (_failedEvents.Count > 0)
                Enqueue(_failedEvents);

            _flushInProgress = false;
        }

        private void Enqueue(List<Event> data)
        {
            foreach (var eventData in data)
                _events.Enqueue(eventData);
        }

        private static async Task<bool> SendEvents(List<Event> events, CancellationToken cancellationToken)
        {
            var webRequest = _webRequestHelper.CreateWebRequest(_apiURL, _appKey, _environment, events.ToJson());
            var result = await _webRequestHelper.SendWebRequestAsync(webRequest, cancellationToken);
            return result;
        }
    }
}