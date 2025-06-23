using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace Utils
{
    public static class WebRequestUtil
    {
        public static async Task<bool> CreateAndSendWebRequestAsync(
            string url,
            string appKey,
            string contents,
            CancellationToken cancellationToken)
        {
            var webRequest = CreateWebRequest(url, appKey, contents);
            return await SendWebRequestAsync(webRequest, cancellationToken);
        }

        public static UnityWebRequest CreateWebRequest(string url, string appKey, string contents)
        {
            var request = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST);
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("App-Key", appKey);

            request.downloadHandler = new DownloadHandlerBuffer();
            request.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(contents));

            return request;
        }

        public static async Task<bool> SendWebRequestAsync(UnityWebRequest request, CancellationToken cancellationToken)
        {
            var requestOp = request.SendWebRequest();
            while (!requestOp.isDone && !cancellationToken.IsCancellationRequested)
                await Task.Yield();

            var success = requestOp.webRequest.result == UnityWebRequest.Result.Success;
            if (!success)
                Debug.LogWarning(
                    $"Failed to perform web request due to {requestOp.webRequest.responseCode} and response body {requestOp.webRequest.error}, result: {requestOp.webRequest.result}");

            request.Dispose();
            return success;
        }
    }
}