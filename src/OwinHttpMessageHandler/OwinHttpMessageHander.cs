using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CuttingEdge.Conditions;

namespace OwinHttpMessageHander
{
    public class OwinHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<IDictionary<string, object>, Task> _appFunc;
        private readonly Action<IDictionary<string, object>> _modifyEnvironment;

        public OwinHttpMessageHandler(Func<IDictionary<string, object>, Task> appFunc, Action<IDictionary<string, object>> modifyEnvironment = null)
        {
            Condition.Requires(appFunc).IsNotNull();
            _appFunc = appFunc;
            _modifyEnvironment = modifyEnvironment ?? (env => {});
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            IDictionary<string, object> env = request.ToEnvironment(cancellationToken);
            _modifyEnvironment(request.ToEnvironment(cancellationToken));
            return Task.Run(() =>
            {
                _appFunc(env);
                return env.ToHttpResponseMessage(request);
            }, cancellationToken);
        }
    }

    public static class EnvironmentExtensions
    {
        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }

        public static IDictionary<string, object> ToEnvironment(this HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return new Dictionary<string, object>
                      {
                          {Constants.VersionKey, Constants.OwinVersion},
                          {Constants.CallCancelledKey, cancellationToken},
                          {Constants.RequestMethodKey, request.Method},
                          {Constants.RequestSchemeKey, request.RequestUri.Scheme},
                          {Constants.ResponseBodyKey, new MemoryStream()},
                          {Constants.RequestPathKey, request.RequestUri.AbsolutePath}
                      };
        }

        public static HttpResponseMessage ToHttpResponseMessage(this IDictionary<string, object> env, HttpRequestMessage request)
        {
            return new HttpResponseMessage
                   {
                       RequestMessage = request,
                       StatusCode = (HttpStatusCode)Get<int>(env, Constants.ResponseStatusCodeKey),
                       ReasonPhrase = Get<string>(env, Constants.ResponseReasonPhraseKey)
                   };
        }

        private static class Constants
        {
            internal const string VersionKey = "owin.Version";
            internal const string OwinVersion = "1.0";
            internal const string CallCancelledKey = "owin.CallCancelled";

            internal const string RequestBodyKey = "owin.RequestBody";
            internal const string RequestHeadersKey = "owin.RequestHeaders";
            internal const string RequestSchemeKey = "owin.RequestScheme";
            internal const string RequestMethodKey = "owin.RequestMethod";
            internal const string RequestPathBaseKey = "owin.RequestPathBase";
            internal const string RequestPathKey = "owin.RequestPath";
            internal const string RequestQueryStringKey = "owin.RequestQueryString";
            internal const string HttpRequestProtocolKey = "owin.RequestProtocol";
            internal const string HttpResponseProtocolKey = "owin.ResponseProtocol";

            internal const string ResponseStatusCodeKey = "owin.ResponseStatusCode";
            internal const string ResponseReasonPhraseKey = "owin.ResponseReasonPhrase";
            internal const string ResponseHeadersKey = "owin.ResponseHeaders";
            internal const string ResponseBodyKey = "owin.ResponseBody";

            internal const string ClientCertifiateKey = "ssl.ClientCertificate";
            internal const string LoadClientCertAsyncKey = "ssl.LoadClientCertAsync";

            internal const string RemoteIpAddressKey = "server.RemoteIpAddress";
            internal const string RemotePortKey = "server.RemotePort";
            internal const string LocalIpAddressKey = "server.LocalIpAddress";
            internal const string LocalPortKey = "server.LocalPort";
            internal const string IsLocalKey = "server.IsLocal";
            internal const string ServerOnSendingHeadersKey = "server.OnSendingHeaders";
            internal const string ServerUserKey = "server.User";

            internal const string WebSocketVersionKey = "websocket.Version";
            internal const string WebSocketVersion = "1.0";
            internal const string WebSocketAcceptKey = "websocket.Accept";
            internal const string WebSocketSubProtocolKey = "websocket.SubProtocol";

            internal const string HostHeader = "Host";
            internal const string WwwAuthenticateHeader = "WWW-Authenticate";
            internal const string ContentLengthHeader = "Content-Length";
            internal const string TransferEncodingHeader = "Transfer-Encoding";
            internal const string KeepAliveHeader = "Keep-Alive";
            internal const string ConnectionHeader = "Connection";
            internal const string SecWebSocketProtocol = "Sec-WebSocket-Protocol";
        }
    }
}