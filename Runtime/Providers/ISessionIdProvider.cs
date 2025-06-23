namespace AptabaseSDK.Providers
{
    public interface ISessionIdProvider
    {
        string GetSessionId();
        void ResetSessionId();
        void SetSessionId(string sessionId);
    }
}