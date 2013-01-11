namespace OwinHttpMessageHandler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using OwinHttpMessageHander;
    using Xunit;

    // ReSharper disable InconsistentNaming

    public class EnvironmentExtensionsTests
    {
        public class ToEnvironmentTests
        {
            private readonly HttpRequestMessage _request;
            private readonly IDictionary<string, object> _sut;

            public ToEnvironmentTests()
            {
                var content = new StringContent("foo");
                _request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8080/path?x=y")
                               {
                                   Content = content
                               };
                _request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                Task<IDictionary<string, object>> environmentAsync = _request.ToEnvironmentAsync(new CancellationToken());
                environmentAsync.Wait();
                _sut = environmentAsync.Result;
            }

            [Fact]
            public void Should_have_Version()
            {
                Assert.NotNull(_sut[Constants.VersionKey]);
            }

            [Fact]
            public void Should_have_CallCanceled()
            {
                Assert.NotNull(_sut[Constants.CallCancelledKey]);
            }

            [Fact]
            public void Should_have_ServerRemoteIpAddress()
            {
                Assert.Equal("127.0.0.1", _sut.Get<string>(Constants.ServerRemoteIpAddressKey));
            }

            [Fact]
            public void Should_have_ServerRemotePort()
            {
                Assert.Equal("1024", _sut.Get<string>(Constants.ServerRemotePortKey));
            }

            [Fact]
            public void Should_have_ServerLocalIpAddress()
            {
                Assert.Equal("127.0.0.1", _sut.Get<string>(Constants.ServerLocalIpAddressKey));
            }

            [Fact]
            public void Should_have_ServerLocalPort()
            {
                Assert.Equal("8080", _sut.Get<string>(Constants.ServerLocalPortKey));
            }

            [Fact]
            public void Should_have_empty_server_capabilities()
            {
                Assert.NotNull(_sut.Get<IList<IDictionary<string, object>>>(Constants.ServerCapabilities));
                Assert.Empty(_sut.Get<IList<IDictionary<string, object>>>(Constants.ServerCapabilities));
            }

            [Fact]
            public void Should_have_IsLocal_true()
            {
                Assert.Equal(true, _sut.Get<bool>(Constants.ServerIsLocalKey));
            }

            [Fact]
            public void Should_have_RequestMethod()
            {
                Assert.Equal(_request.Method.ToString().ToUpperInvariant(), _sut.Get<string>(Constants.RequestMethodKey));
            }

            [Fact]
            public void Should_have_RequestPath()
            {
                Assert.Equal(_request.RequestUri.AbsolutePath, _sut.Get<string>(Constants.RequestPathKey));
            }

            [Fact]
            public void Should_have_RequestQueryString()
            {
                Assert.Equal("x=y", _sut.Get<string>(Constants.RequestQueryStringKey));
            }

            [Fact]
            public void Should_have_RequestBody()
            {
                var stream = _sut.Get<Stream>(Constants.RequestBodyKey);
                Assert.NotNull(stream);
                string body = new StreamReader(stream).ReadToEnd();
                Assert.Equal("foo", body);
            }

            [Fact]
            public void Should_have_RequestHeaders()
            {
                var headers = _sut.Get<IDictionary<string, string[]>>(Constants.RequestHeadersKey);
                Assert.NotNull(headers);
                Assert.NotEmpty(headers);
            }

            [Fact]
            public void Should_have_RequestPathBase()
            {
                Assert.Equal(string.Empty, _sut.Get<string>(Constants.RequestPathBaseKey));
            }

            [Fact]
            public void Should_have_RequestProtocol()
            {
                Assert.Equal("HTTP/1.1", _sut.Get<string>(Constants.RequestProtocolKey));
            }

            [Fact]
            public void Should_have_RequestScheme()
            {
                Assert.Equal("https", _sut.Get<string>(Constants.RequestSchemeKey));
            }
        }

        public class ToHttpResponseMessageTests
        {
            private HttpResponseMessage _sut;

            public ToHttpResponseMessageTests()
            {
                var content = new StringContent("foo");
                var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8080/path?x=y")
                               {
                                   Content = content
                               };
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
                var env = new Dictionary<string, object>
                       {
                           {Constants.ResponseBodyKey, new MemoryStream().Write("foo")},
                           {Constants.ResponseHeadersKey, new Dictionary<string, string[]>
                                                              {
                                                                  { Constants.ContentLengthHeader, new string[] { "3" } },
                                                                  { Constants.ConnectionHeader, new string[] { "close" } }
                                                              }},
                           {Constants.ResponseStatusCodeKey, 302},
                           {Constants.ResponseReasonPhraseKey, "302 Found"},
                           {Constants.RequestProtocolKey, "HTTP/1.1"}
                       };
                _sut = env.ToHttpResponseMessage(request);
            }

            [Fact]
            public void Should_have_StatusCode()
            {
                Assert.Equal(HttpStatusCode.Found, _sut.StatusCode);
            }

            [Fact]
            public void Should_have_ReasonPhrase()
            {
                Assert.Equal("302 Found", _sut.ReasonPhrase);
            }

            [Fact]
            public void Should_have_RequestMessage()
            {
                Assert.NotNull(_sut.RequestMessage);
            }

            [Fact]
            public void Should_have_version()
            {
                Assert.Equal(new Version(1,1), _sut.Version);
            }

            [Fact]
            public void Should_have_Headers()
            {
                Assert.NotEmpty(_sut.Headers);
            }

            [Fact]
            public void Should_have_ContentHeaders()
            {
                Assert.NotEmpty(_sut.Content.Headers);
            }

            [Fact]
            public void Should_have_Content()
            {
                Assert.NotNull(_sut.Content);
            }
        }
    }

    // ReSharper restore InconsistentNaming

    public static class ExtensionHelpers
    {
        public static T Get<T>(this IDictionary<string, object> dictionary, string key)
        {
            return (T) dictionary[key];
        }

        public static Stream Write(this Stream stream, string s)
        {
            new StreamWriter(stream).Write(s);
            return stream;
        }
    }
}