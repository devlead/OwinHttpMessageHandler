namespace OwinHttpMessageHander
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public static class EnvironmentExtensions
    {
        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T) value;
            }
            return default(T);
        }

        public static async Task<IDictionary<string, object>> ToEnvironmentAsync(this HttpRequestMessage request,
                                                                                 CancellationToken cancellationToken)
        {
            string query = string.IsNullOrWhiteSpace(request.RequestUri.Query)
                               ? string.Empty
                               : request.RequestUri.Query.Substring(1);
            Dictionary<string, string[]> headers = request.Headers.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            return new Dictionary<string, object>
                       {
                           {Constants.VersionKey, Constants.OwinVersion},
                           {Constants.CallCancelledKey, cancellationToken},
                           {Constants.ServerRemoteIpAddressKey, "127.0.0.1"},
                           {Constants.ServerRemotePortKey, "1024"},
                           {Constants.ServerIsLocalKey, true},
                           {Constants.ServerLocalIpAddressKey, "127.0.0.1"},
                           {Constants.ServerLocalPortKey, request.RequestUri.Port.ToString()},
                           {Constants.ServerCapabilities, new List<IDictionary<string, object>>()},
                           {Constants.RequestMethodKey, request.Method.ToString().ToUpperInvariant()},
                           {Constants.RequestSchemeKey, request.RequestUri.Scheme},
                           {Constants.ResponseBodyKey, new MemoryStream()},
                           {Constants.RequestPathKey, request.RequestUri.AbsolutePath},
                           {Constants.RequestQueryStringKey, query},
                           {Constants.RequestBodyKey, await request.Content.ReadAsStreamAsync()},
                           {Constants.RequestHeadersKey, headers},
                           {Constants.RequestPathBaseKey, string.Empty},
                           {Constants.RequestProtocolKey, "HTTP/" + request.Version}
                       };
        }

        public static HttpResponseMessage ToHttpResponseMessage(this IDictionary<string, object> env,
                                                                HttpRequestMessage request)
        {
            return new HttpResponseMessage
                       {
                           RequestMessage = request,
                           StatusCode = (HttpStatusCode) Get<int>(env, Constants.ResponseStatusCodeKey),
                           ReasonPhrase = Get<string>(env, Constants.ResponseReasonPhraseKey)
                       };
        }
    }
}