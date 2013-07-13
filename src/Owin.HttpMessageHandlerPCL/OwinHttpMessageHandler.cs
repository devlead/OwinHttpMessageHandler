namespace Owin
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class OwinHttpMessageHandler : HttpMessageHandler
    {
        private readonly Action<IDictionary<string, object>> _afterInvoke;
        private readonly Func<IDictionary<string, object>, Task> _appFunc;
        private readonly Action<IDictionary<string, object>> _beforeInvoke;
        private readonly CookieContainer _cookieContainer = new CookieContainer();

        public OwinHttpMessageHandler(Func<IDictionary<string, object>, Task> appFunc,
                                      Action<IDictionary<string, object>> beforeInvoke = null,
                                      Action<IDictionary<string, object>> afterInvoke = null)
        {
            if (appFunc == null)
            {
                throw new ArgumentNullException("appFunc");
            }
            _appFunc = appFunc;
            _beforeInvoke = beforeInvoke ?? (env => { });
            _afterInvoke = afterInvoke ?? (env => { });
        }

        public bool UseCookies { get; set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                                     CancellationToken cancellationToken)
        {
            if (UseCookies)
            {
                string cookieHeader = _cookieContainer.GetCookieHeader(request.RequestUri);
                if (!string.IsNullOrEmpty(cookieHeader))
                {
                    request.Headers.Add("Cookie", cookieHeader);
                }
            }
            IDictionary<string, object> env = await ToEnvironmentAsync(request, cancellationToken);
            _beforeInvoke(env);
            await _appFunc(env);
            _afterInvoke(env);
            return ToHttpResponseMessage(env, request, UseCookies ? _cookieContainer : null);
        }

        public static async Task<IDictionary<string, object>> ToEnvironmentAsync(HttpRequestMessage request,
                                                                                 CancellationToken cancellationToken)
        {
            string query = string.IsNullOrWhiteSpace(request.RequestUri.Query)
                               ? string.Empty
                               : request.RequestUri.Query.Substring(1);
            var httpHeaders = new List<HttpHeaders> { request.Headers };
            if (request.Content != null)
            {
                httpHeaders.Add(request.Content.Headers);
            }
            Dictionary<string, string[]> headers = httpHeaders.SelectMany(_ => _)
                                                              .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            Stream requestBody = request.Content == null ? null : await request.Content.ReadAsStreamAsync();
            return new Dictionary<string, object>
                   {
                       {OwinConstants.VersionKey, OwinConstants.OwinVersion},
                       {OwinConstants.CallCancelledKey, cancellationToken},
                       {OwinConstants.ServerRemoteIpAddressKey, "127.0.0.1"},
                       {OwinConstants.ServerRemotePortKey, "1024"},
                       {OwinConstants.ServerIsLocalKey, true},
                       {OwinConstants.ServerLocalIpAddressKey, "127.0.0.1"},
                       {OwinConstants.ServerLocalPortKey, request.RequestUri.Port.ToString()},
                       {OwinConstants.ServerCapabilities, new List<IDictionary<string, object>>()},
                       {OwinConstants.RequestMethodKey, request.Method.ToString().ToUpperInvariant()},
                       {OwinConstants.RequestSchemeKey, request.RequestUri.Scheme},
                       {OwinConstants.ResponseBodyKey, new MemoryStream()},
                       {OwinConstants.RequestPathKey, request.RequestUri.AbsolutePath},
                       {OwinConstants.RequestQueryStringKey, query},
                       {OwinConstants.RequestBodyKey, requestBody},
                       {OwinConstants.RequestHeadersKey, headers},
                       {OwinConstants.RequestPathBaseKey, string.Empty},
                       {OwinConstants.RequestProtocolKey, "HTTP/" + request.Version},
                       {OwinConstants.ResponseHeadersKey, new Dictionary<string, string[]>()}
                   };
        }

        public static HttpResponseMessage ToHttpResponseMessage(IDictionary<string, object> env, HttpRequestMessage request,
                                                                CookieContainer cookieContainer = null)
        {
            var responseBody = Get<Stream>(env, OwinConstants.ResponseBodyKey);
            responseBody.Position = 0;
            var response = new HttpResponseMessage
            {
                RequestMessage = request,
                StatusCode = (HttpStatusCode)Get<int>(env, OwinConstants.ResponseStatusCodeKey),
                ReasonPhrase = Get<string>(env, OwinConstants.ResponseReasonPhraseKey),
                Content = new StreamContent(responseBody)
            };
            var headers = Get<IDictionary<string, string[]>>(env, OwinConstants.ResponseHeadersKey);
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    response.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    response.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }
            if (cookieContainer != null)
            {
                IEnumerable<string> setCookieHeaders = Get<IDictionary<string, string[]>>(env, OwinConstants.ResponseHeadersKey)
                    .Where(kvp => kvp.Key == "Set-Cookie")
                    .SelectMany(kvp => kvp.Value);
                foreach (string setCookieHeader in setCookieHeaders)
                {
                    cookieContainer.SetCookies(request.RequestUri, setCookieHeader);
                }
            }
            return response;
        }

        private static T Get<T>(IDictionary<string, object> env, string key)
        {
            object value;
            if (env.TryGetValue(key, out value))
            {
                return (T)value;
            }
            return default(T);
        }

        public static class OwinConstants
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

            public const string ServerRemoteIpAddressKey = "server.RemoteIpAddress";
            public const string ServerRemotePortKey = "server.RemotePort";
            public const string ServerLocalIpAddressKey = "server.LocalIpAddress";
            public const string ServerLocalPortKey = "server.LocalPort";
            public const string ServerIsLocalKey = "server.IsLocal";
            public const string ServerOnSendingHeadersKey = "server.OnSendingHeaders";
            public const string ServerUserKey = "server.User";
            public const string ServerCapabilities = "server.Capabilities";

            public const string HostHeader = "Host";
            public const string WwwAuthenticateHeader = "WWW-Authenticate";
            public const string ContentLengthHeader = "Content-Length";
            public const string ContentTypeHeader = "Content-Type";
            public const string TransferEncodingHeader = "Transfer-Encoding";
            public const string KeepAliveHeader = "Keep-Alive";
            public const string ConnectionHeader = "Connection";
        }
    }
}