namespace AptabaseSDK.Providers
{
    public class DefaultEnvironmentProvider : IEnvironmentProvider
    {
        protected Data.Environment _cachedEnvironment;

        public virtual Data.Environment Get()
        {
            return _cachedEnvironment ??= new Data.Environment();
        }
    }
}