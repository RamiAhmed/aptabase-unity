using AptabaseSDK.Data;

namespace AptabaseSDK.Providers
{
    public class DefaultEnvironmentProvider : IEnvironmentProvider
    {
        private EnvironmentInfo _cachedEnvironmentInfo;

        public EnvironmentInfo Get()
        {
            return _cachedEnvironmentInfo ??= new EnvironmentInfo();
        }
    }
}