namespace System.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;

    public class OwinHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<IDictionary<string, object>, Task> _appFunc;
        private readonly CookieContainer _cookieContainer = new CookieContainer();

        public OwinHttpMessageHandler(Func<IDictionary<string, object>, Task> appFunc)
        {
            if (appFunc == null)
            {
                throw new ArgumentNullException("appFunc");
            }
            _appFunc = appFunc;
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
            Action sendingHeaders = () => { };
            env.Add(Constants.Server.OnSendingHeadersKey, new Action<Action<object>, object>((callback, state) =>
            {
                var previous = sendingHeaders;
                sendingHeaders = () =>
                {
                    previous();
                    callback(state);
                };
            }));
            await _appFunc(env);
            sendingHeaders();
            return ToHttpResponseMessage(env, request, UseCookies ? _cookieContainer : null);
        }

        public static async Task<IDictionary<string, object>> ToEnvironmentAsync(HttpRequestMessage request,
                                                                                 CancellationToken cancellationToken)
        {
            string query = string.IsNullOrWhiteSpace(request.RequestUri.Query)
                               ? string.Empty
                               : request.RequestUri.Query.Substring(1);
            var httpHeaders = new List<HttpHeaders> {request.Headers};
            if (request.Content != null)
            {
                httpHeaders.Add(request.Content.Headers);
            }
            Dictionary<string, string[]> headers = httpHeaders.SelectMany(_ => _)
                                                              .ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
            // Host header required for http 1.1
            if (request.Version >= new Version(1, 1))
            {
                string host = request.RequestUri.Host;
                if (request.RequestUri.Port != 80)
                {
                    host += ":" + request.RequestUri.Port;
                }
                headers.Add(Constants.Headers.Host, new[] {host});
            }

            Stream requestBody = request.Content == null ? Stream.Null : await request.Content.ReadAsStreamAsync();
            return new Dictionary<string, object>
                   {
                       {Constants.Owin.VersionKey, Constants.Owin.Version},
                       {Constants.Owin.CallCancelledKey, cancellationToken},
                       {Constants.Server.RemoteIpAddressKey, "127.0.0.1"},
                       {Constants.Server.RemotePortKey, "1024"},
                       {Constants.Server.IsLocalKey, true},
                       {Constants.Server.LocalIpAddressKey, "127.0.0.1"},
                       {Constants.Server.LocalPortKey, request.RequestUri.Port.ToString()},
                       {Constants.Server.ServerCapabilities, new List<IDictionary<string, object>>()},
                       {Constants.Owin.RequestMethodKey, request.Method.ToString().ToUpperInvariant()},
                       {Constants.Owin.RequestSchemeKey, request.RequestUri.Scheme},
                       {Constants.Owin.ResponseBodyKey, new MemoryStream()},
                       {Constants.Owin.RequestPathKey, request.RequestUri.AbsolutePath},
                       {Constants.Owin.RequestQueryStringKey, query},
                       {Constants.Owin.RequestBodyKey, requestBody},
                       {Constants.Owin.RequestHeadersKey, headers},
                       {Constants.Owin.RequestPathBaseKey, string.Empty},
                       {Constants.Owin.RequestProtocolKey, "HTTP/" + request.Version},
                       {Constants.Owin.ResponseHeadersKey, new Dictionary<string, string[]>()}
                   };
        }

        public static HttpResponseMessage ToHttpResponseMessage(IDictionary<string, object> env, HttpRequestMessage request,
                                                                CookieContainer cookieContainer = null)
        {
            var responseBody = Get<Stream>(env, Constants.Owin.ResponseBodyKey);
            responseBody.Position = 0;
            var statusCode = Get<int>(env, Constants.Owin.ResponseStatusCodeKey);
            var response = new HttpResponseMessage
                           {
                               RequestMessage = request,
                               StatusCode = statusCode == 0 ? HttpStatusCode.OK : (HttpStatusCode)statusCode,
                               ReasonPhrase = Get<string>(env, Constants.Owin.ResponseReasonPhraseKey),
                               Content = new StreamContent(responseBody)
                           };
            var headers = Get<IDictionary<string, string[]>>(env, Constants.Owin.ResponseHeadersKey);
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
                IEnumerable<string> setCookieHeaders = Get<IDictionary<string, string[]>>(env, Constants.Owin.ResponseHeadersKey)
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
                return (T) value;
            }
            return default(T);
        }

        public static class Constants
        {
            public static class Owin
            {
                public const string VersionKey = "owin.Version";
                public const string Version = "1.0";
                public const string CallCancelledKey = "owin.CallCancelled";

                public const string RequestBodyKey = "owin.RequestBody";
                public const string RequestHeadersKey = "owin.RequestHeaders";
                public const string RequestSchemeKey = "owin.RequestScheme";
                public const string RequestMethodKey = "owin.RequestMethod";
                public const string RequestPathBaseKey = "owin.RequestPathBase";
                public const string RequestPathKey = "owin.RequestPath";
                public const string RequestQueryStringKey = "owin.RequestQueryString";
                public const string RequestProtocolKey = "owin.RequestProtocol";

                public const string ResponseStatusCodeKey = "owin.ResponseStatusCode";
                public const string ResponseReasonPhraseKey = "owin.ResponseReasonPhrase";
                public const string ResponseHeadersKey = "owin.ResponseHeaders";
                public const string ResponseBodyKey = "owin.ResponseBody";
            }

            public static class Server 
            {
                public const string RemoteIpAddressKey = "server.RemoteIpAddress";
                public const string RemotePortKey = "server.RemotePort";
                public const string LocalIpAddressKey = "server.LocalIpAddress";
                public const string LocalPortKey = "server.LocalPort";
                public const string IsLocalKey = "server.IsLocal";
                public const string OnSendingHeadersKey = "server.OnSendingHeaders";
                public const string ServerCapabilities = "server.Capabilities";
            }

            public static class Headers
            {
                public const string Host = "Host";
                public const string ContentLength = "Content-Length";
                public const string ContentType = "Content-Type";
                public const string Connection = "Connection";
            }
        }
    }
}