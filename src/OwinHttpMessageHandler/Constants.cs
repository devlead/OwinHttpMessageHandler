namespace OwinHttpMessageHander
{
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