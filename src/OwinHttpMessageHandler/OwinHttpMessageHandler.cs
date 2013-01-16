namespace OwinHttpMessageHandler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    public class OwinHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<IDictionary<string, object>, Task> _appFunc;
        private readonly Action<IDictionary<string, object>> _modifyEnvironment;

        public OwinHttpMessageHandler(Func<IDictionary<string, object>, Task> appFunc,
                                      Action<IDictionary<string, object>> modifyEnvironment = null)
        {
            if (appFunc == null)
            {
                throw new ArgumentNullException("appFunc");
            }
            _appFunc = appFunc;
            _modifyEnvironment = modifyEnvironment ?? (env => { });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            return ToEnvironmentAsync(request, cancellationToken)
                .ContinueWith(task =>
                              {
                                  IDictionary<string, object> env = task.Result;
                                  _modifyEnvironment(env);
                                  _appFunc(env);
                                  return ToHttpResponseMessage(env, request);
                              });
        }

        public static async Task<IDictionary<string, object>> ToEnvironmentAsync(HttpRequestMessage request,
                                                                                 CancellationToken cancellationToken)
        {
            string query = string.IsNullOrWhiteSpace(request.RequestUri.Query)
                               ? string.Empty
                               : request.RequestUri.Query.Substring(1);
            Dictionary<string, string[]> headers = request.Headers.ToDictionary(pair => pair.Key,
                                                                                pair => pair.Value.ToArray());
            Stream requestBody = request.Content == null ? null : await request.Content.ReadAsStreamAsync();
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
                       {Constants.RequestBodyKey, requestBody},
                       {Constants.RequestHeadersKey, headers},
                       {Constants.RequestPathBaseKey, string.Empty},
                       {Constants.RequestProtocolKey, "HTTP/" + request.Version}
                   };
        }

        public static HttpResponseMessage ToHttpResponseMessage(IDictionary<string, object> env,
                                                                HttpRequestMessage request)
        {
            var responseBody = Get<Stream>(env, Constants.ResponseBodyKey);
            responseBody.Position = 0;
            var response = new HttpResponseMessage
                           {
                               RequestMessage = request,
                               StatusCode = (HttpStatusCode) Get<int>(env, Constants.ResponseStatusCodeKey),
                               ReasonPhrase = Get<string>(env, Constants.ResponseReasonPhraseKey),
                               Content = new StreamContent(responseBody)
                           };
            var headers = Get<IDictionary<string, string[]>>(env, Constants.ResponseHeadersKey);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            return response;
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T) value;
            }
            return default(T);
        }

        public static class Constants
        {
            public const string VersionKey = "owin.Version";
            public const string OwinVersion = "1.0";
            public const string CallCancelledKey = "owin.CallCancelled";

            public const string RequestBodyKey = "owin.RequestBody";
            public const string RequestHeadersKey = "owin.RequestHeaders";
            public const string RequestSchemeKey = "owin.RequestScheme";
            public const string RequestMethodKey = "owin.RequestMethod";
            public const string RequestPathBaseKey = "owin.RequestPathBase";
            public const string RequestPathKey = "owin.RequestPath";
            public const string RequestQueryStringKey = "owin.RequestQueryString";
            public const string RequestProtocolKey = "owin.RequestProtocol";
            public const string HttpResponseProtocolKey = "owin.ResponseProtocol";

            public const string ResponseStatusCodeKey = "owin.ResponseStatusCode";
            public const string ResponseReasonPhraseKey = "owin.ResponseReasonPhrase";
            public const string ResponseHeadersKey = "owin.ResponseHeaders";
            public const string ResponseBodyKey = "owin.ResponseBody";

            public const string ClientCertifiateKey = "ssl.ClientCertificate";
            public const string LoadClientCertAsyncKey = "ssl.LoadClientCertAsync";

            public const string ServerRemoteIpAddressKey = "server.RemoteIpAddress";
            public const string ServerRemotePortKey = "server.RemotePort";
            public const string ServerLocalIpAddressKey = "server.LocalIpAddress";
            public const string ServerLocalPortKey = "server.LocalPort";
            public const string ServerIsLocalKey = "server.IsLocal";
            public const string ServerOnSendingHeadersKey = "server.OnSendingHeaders";
            public const string ServerUserKey = "server.User";
            public const string ServerCapabilities = "server.Capabilities";

            public const string WebSocketVersionKey = "websocket.Version";
            public const string WebSocketVersion = "1.0";
            public const string WebSocketAcceptKey = "websocket.Accept";
            public const string WebSocketSubProtocolKey = "websocket.SubProtocol";

            public const string HostHeader = "Host";
            public const string WwwAuthenticateHeader = "WWW-Authenticate";
            public const string ContentLengthHeader = "Content-Length";
            public const string TransferEncodingHeader = "Transfer-Encoding";
            public const string KeepAliveHeader = "Keep-Alive";
            public const string ConnectionHeader = "Connection";
            public const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        }
    }
}