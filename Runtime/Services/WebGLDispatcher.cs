using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AptabaseSDK.Configuration;
using AptabaseSDK.Data;
using AptabaseSDK.Providers;
using AptabaseSDK.TinyJson;
using Utils;

namespace AptabaseSDK.Services
{
    public class WebGLDispatcher : DefaultDispatcher
    {
        private readonly IEnvironmentProvider _environmentProvider;

        public WebGLDispatcher(
            IHostProvider hostProvider,
            IEnvironmentProvider environmentProvider,
            AptabaseSettings settings)
            : base(hostProvider, settings, 1)
        {
            _environmentProvider = environmentProvider;
        }

        public override void Enqueue(AptabaseEvent data)
        {
            base.Enqueue(data);
            _ = Flush(CancellationToken.None);
        }

        protected override Task<bool> TrySendEvents(List<AptabaseEvent> events, CancellationToken cancellationToken)
        {
            var request = WebRequestUtil.CreateWebRequest(
                ApiUrl,
                Settings.AppKey,
                events.ToJson());

// webgl needs the default user-agent header. All other platforms we create manually
#if !UNITY_WEBGL
            var env = _environmentProvider.Get();
            request.SetRequestHeader("User-Agent", $"{env.OsName}/${env.OsVersion} ${env.Locale}");
#endif

            return WebRequestUtil.SendWebRequestAsync(request, cancellationToken);
        }
    }
}