using System;
using System.Diagnostics;

namespace AptabaseSDK
{
    public sealed class ActionTimer : IDisposable
    {
        private readonly string _message;
        private readonly Stopwatch _sw;

        public ActionTimer(string message)
        {
            _message = message;
            _sw = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _sw.Stop();
            UnityEngine.Debug.Log(_message + $" Elapsed: {_sw.Elapsed}");
        }
    }
}