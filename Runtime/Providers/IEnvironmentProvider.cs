using AptabaseSDK.Data;

namespace AptabaseSDK.Providers
{
    public interface IEnvironmentProvider
    {
        EnvironmentInfo Get();
    }
}