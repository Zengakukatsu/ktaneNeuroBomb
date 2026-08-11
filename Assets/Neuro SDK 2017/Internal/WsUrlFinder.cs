
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

namespace NeuroSdk.Internal
{
    internal static class WsUrlFinder
    {
        public static IEnumerator FindWsUrl(Action<string> callback)
        {
            string url = "ws://127.0.0.1:8000";
            TryGetWsUrlFromQuery(ref url);
            yield return TryGetWsUrlFromServer(url, result => url = result);
            TryGetWsUrlFromEnvironment(ref url);

            callback(url ?? "");
        }

        private static void TryGetWsUrlFromQuery(ref string url)
        {
            try
            {
                if (Application.absoluteURL.IndexOf("?", StringComparison.Ordinal) == -1) return;

                string[] urlSplits = Application.absoluteURL.Split('?');
                if (urlSplits.Length <= 1) return;

                string[] urlParamSplits = urlSplits[1].Split(new[] { "WebSocketURL=" }, StringSplitOptions.None);
                if (urlParamSplits.Length <= 1) return;

                string param = urlParamSplits[1].Split('&')[0];
                if (string.IsNullOrEmpty(param)) return;

                url = param;
            }
            catch
            {
                // ignore
            }
        }

        private static IEnumerator TryGetWsUrlFromServer(string url, Action<string> callback)
        {
            if (url != null && url != "") yield break;

            UnityWebRequest request;
            try
            {
                Uri uri = new Uri(Application.absoluteURL);
                string requestUrl = string.Format("{0}://{1}:{2}/$env/NEURO_SDK_WS_URL", uri.Scheme, uri.Host, uri.Port);
                request = UnityWebRequest.Get(requestUrl);
            }
            catch
            {
                yield break;
            }

            yield return request.SendWebRequest();

#pragma warning disable CS0618 // Type or member is obsolete
            if (request != null &&
                request.isDone &&
                !request.isHttpError &&
                !request.isNetworkError)
#pragma warning restore CS0618 // Type or member is obsolete
            {
                callback(request.downloadHandler.text);
            }
        }

        private static void TryGetWsUrlFromEnvironment(ref string
         url)
        {
            if (url != null && url != "") return;

            url = Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.Process);
            if (url != null && url != "") return;

            url = Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.User);
            if (url != null && url != "") return;
                  
            url = Environment.GetEnvironmentVariable("NEURO_SDK_WS_URL", EnvironmentVariableTarget.Machine);
        }
    }
}
