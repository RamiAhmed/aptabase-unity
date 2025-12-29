using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace AptabaseSDK
{
    public class WebRequestHelper
    {
        private readonly string _appKey;
        private readonly EnvironmentInfo _env;
        private readonly string _url;

        public WebRequestHelper(string url, string appKey, EnvironmentInfo env)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("[AptabaseAnalytics] URL cannot be null or empty", nameof(url));

            if (string.IsNullOrEmpty(appKey))
                throw new ArgumentException("[AptabaseAnalytics] AppKey cannot be null or empty", nameof(appKey));

            _url = url;
            _appKey = appKey;
            _env = env;
        }

        public async Task<bool> CreateAndSendWebRequestAsync(string contents, CancellationToken cancellationToken)
        {
            return await SendWebRequestAsync(CreateWebRequest(contents), cancellationToken);
        }

        private UnityWebRequest CreateWebRequest(string contents)
        {
            var webRequest = new UnityWebRequest(_url, UnityWebRequest.kHttpVerbPOST);
            webRequest.SetRequestHeader("Content-Type", "application/json");
            webRequest.SetRequestHeader("App-Key", _appKey);
            // webgl needs the default user-agent header. All other platforms we create manually
#if !UNITY_WEBGL
            webRequest.SetRequestHeader("User-Agent", $"{_env.osName}/${_env.osVersion} ${_env.locale}");
#endif

            webRequest.downloadHandler = new DownloadHandlerBuffer();
            webRequest.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));
            return webRequest;
        }

        private static async Task<bool> SendWebRequestAsync(
            UnityWebRequest request,
            CancellationToken cancellationToken)
        {
            var requestOp = request.SendWebRequest();
            while (!requestOp.isDone)
            {
                if (cancellationToken.IsCancellationRequested)
                    return false;

                await Task.Yield();
            }

            var success = requestOp.webRequest.result is UnityWebRequest.Result.Success;
            if (!success)
                Debug.LogWarning(
                    $"[AptabaseAnalytics] Failed to perform web request due to {requestOp.webRequest.responseCode} " +
                    $"and response body {requestOp.webRequest.error}, " +
                    $"result: {requestOp.webRequest.result}.");

            switch (requestOp.webRequest.responseCode)
            {
                case 0:
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Network error occurred. Please check your internet connection.");
                    break;
                }

                case 200: // Success
                case 201: // Created
                case 202: // Accepted
                case 204: // No Content
                {
                    break;
                }

                case 400: // Bad Request
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Bad request sent to server. Check event data for correctness. May also happen if rate limits are exceeded for Aptabase Cloud.");
                    break;
                }

                case 401: // Unauthorized
                {
                    Debug.LogWarning("[AptabaseAnalytics] Unauthorized request. Please check your App Key.");
                    break;
                }

                case 403: // Forbidden
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Access forbidden. Your App Key may not have permission to send events to this endpoint.");
                    break;
                }

                case 404: // Not Found
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Endpoint not found. Please verify your server URL configuration.");
                    break;
                }

                case 408: // Request Timeout
                {
                    Debug.LogWarning("[AptabaseAnalytics] Request timed out. Server took too long to respond.");
                    break;
                }

                case 413: // Payload Too Large
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Request payload too large. Consider reducing batch size or event data size.");
                    break;
                }

                case 429: // Too Many Requests
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Rate limited by server. Consider increasing your flush interval or reducing event volume.");
                    break;
                }

                case 500: // Internal Server Error
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Internal server error occurred. This may be temporary, please try again later.");
                    break;
                }

                case 502: // Bad Gateway
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Bad gateway error. The server received an invalid response from upstream.");
                    break;
                }

                case 503: // Service Unavailable
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Service temporarily unavailable. Server may be under maintenance or overloaded.");
                    break;
                }

                case 504: // Gateway Timeout
                {
                    Debug.LogWarning(
                        "[AptabaseAnalytics] Gateway timeout. The server did not receive a timely response from upstream.");
                    break;
                }

                case >= 500: // Other Server Errors
                {
                    Debug.LogWarning(
                        $"[AptabaseAnalytics] Server error {requestOp.webRequest.responseCode} occurred. This may be temporary, please try again later.");
                    break;
                }

                default:
                {
                    Debug.LogWarning(
                        $"[AptabaseAnalytics] Unexpected response code {requestOp.webRequest.responseCode}. Error: {requestOp.webRequest.error}, result: {requestOp.webRequest.result}");
                    break;
                }
            }

            request.Dispose();
            return success;
        }
    }
}