using AptabaseSDK.Data;

namespace AptabaseSDK.Providers
{
    public class DefaultEnvironmentProvider : IEnvironmentProvider
    {
        private Environment _cachedEnvironment;

        public Environment Get()
        {
            return _cachedEnvironment ??= new Environment();
        }
    }
}