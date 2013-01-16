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
    using Owin;
    using Owin.Builder;
    using Owin.Loader;
    using Xunit;

    // ReSharper disable InconsistentNaming

    public class OwinHttpMessageHandlerTests
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
                Task<IDictionary<string, object>> environmentAsync = OwinHttpMessageHandler.ToEnvironmentAsync(
                    _request, new CancellationToken());
                environmentAsync.Wait();
                _sut = environmentAsync.Result;
            }

            [Fact]
            public void Should_have_Version()
            {
                Assert.NotNull(_sut[OwinHttpMessageHandler.Constants.VersionKey]);
            }

            [Fact]
            public void Should_have_CallCanceled()
            {
                Assert.NotNull(_sut[OwinHttpMessageHandler.Constants.CallCancelledKey]);
            }

            [Fact]
            public void Should_have_ServerRemoteIpAddress()
            {
                Assert.Equal("127.0.0.1", _sut.Get<string>(OwinHttpMessageHandler.Constants.ServerRemoteIpAddressKey));
            }

            [Fact]
            public void Should_have_ServerRemotePort()
            {
                Assert.Equal("1024", _sut.Get<string>(OwinHttpMessageHandler.Constants.ServerRemotePortKey));
            }

            [Fact]
            public void Should_have_ServerLocalIpAddress()
            {
                Assert.Equal("127.0.0.1", _sut.Get<string>(OwinHttpMessageHandler.Constants.ServerLocalIpAddressKey));
            }

            [Fact]
            public void Should_have_ServerLocalPort()
            {
                Assert.Equal("8080", _sut.Get<string>(OwinHttpMessageHandler.Constants.ServerLocalPortKey));
            }

            [Fact]
            public void Should_have_empty_server_capabilities()
            {
                Assert.NotNull(_sut.Get<IList<IDictionary<string, object>>>(OwinHttpMessageHandler.Constants.ServerCapabilities));
                Assert.Empty(_sut.Get<IList<IDictionary<string, object>>>(OwinHttpMessageHandler.Constants.ServerCapabilities));
            }

            [Fact]
            public void Should_have_IsLocal_true()
            {
                Assert.Equal(true, _sut.Get<bool>(OwinHttpMessageHandler.Constants.ServerIsLocalKey));
            }

            [Fact]
            public void Should_have_RequestMethod()
            {
                Assert.Equal(_request.Method.ToString().ToUpperInvariant(), _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestMethodKey));
            }

            [Fact]
            public void Should_have_RequestPath()
            {
                Assert.Equal(_request.RequestUri.AbsolutePath, _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestPathKey));
            }

            [Fact]
            public void Should_have_RequestQueryString()
            {
                Assert.Equal("x=y", _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestQueryStringKey));
            }

            [Fact]
            public void Should_have_RequestBody()
            {
                var stream = _sut.Get<Stream>(OwinHttpMessageHandler.Constants.RequestBodyKey);
                Assert.NotNull(stream);
                string body = new StreamReader(stream).ReadToEnd();
                Assert.Equal("foo", body);
            }

            [Fact]
            public void Should_have_RequestHeaders()
            {
                var headers = _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.RequestHeadersKey);
                Assert.NotNull(headers);
                Assert.NotEmpty(headers);
            }

            [Fact]
            public void Should_have_RequestPathBase()
            {
                Assert.Equal(string.Empty, _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestPathBaseKey));
            }

            [Fact]
            public void Should_have_RequestProtocol()
            {
                Assert.Equal("HTTP/1.1", _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestProtocolKey));
            }

            [Fact]
            public void Should_have_RequestScheme()
            {
                Assert.Equal("https", _sut.Get<string>(OwinHttpMessageHandler.Constants.RequestSchemeKey));
            }
        }

        public class ToHttpResponseMessageTests
        {
            private readonly HttpResponseMessage _sut;

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
                              {OwinHttpMessageHandler.Constants.ResponseBodyKey, new MemoryStream().Write("foo")},
                              {
                                  OwinHttpMessageHandler.Constants.ResponseHeadersKey, new Dictionary<string, string[]>
                                                                {
                                                                    {OwinHttpMessageHandler.Constants.ContentLengthHeader, new[] {"3"}},
                                                                    {OwinHttpMessageHandler.Constants.ConnectionHeader, new[] {"close"}}
                                                                }
                              },
                              {OwinHttpMessageHandler.Constants.ResponseStatusCodeKey, 302},
                              {OwinHttpMessageHandler.Constants.ResponseReasonPhraseKey, "302 Found"},
                              {OwinHttpMessageHandler.Constants.RequestProtocolKey, "HTTP/1.1"}
                          };
                _sut = OwinHttpMessageHandler.ToHttpResponseMessage(env, request);
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
                Assert.Equal(new Version(1, 1), _sut.Version);
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

        private readonly HttpClient _sut;

        public OwinHttpMessageHandlerTests()
        {
            var appBuilder = new AppBuilder();
            new DefaultLoader().Load(typeof (Startup).FullName)(appBuilder);
            Func<IDictionary<string, object>, Task> app = appBuilder.Build();
            _sut = new HttpClient(new OwinHttpMessageHandler(app));
        }

        [Fact]
        public void When_appfunc_paramater_is_null_Then_should_throw()
        {
            Assert.Throws<ArgumentNullException>(() => { new OwinHttpMessageHandler(null); });
        }

        [Fact]
        public async Task Should_get_status_OK()
        {
            HttpResponseMessage response = await _sut.GetAsync("http://sample.com/OK");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Should_get_status_NotFound()
        {
            HttpResponseMessage response = await _sut.GetAsync("http://sample.com/NotFound");
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task Should_handle_form_data()
        {
            HttpResponseMessage response = await _sut.PostAsync("http://sample.com/greeting",
                               new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                                                         {
                                                             new KeyValuePair<string, string>("Name", "Damian")
                                                         }));
            Assert.Equal("Hello Damian", await response.Content.ReadAsStringAsync());
        }
    }
    // ReSharper restore InconsistentNaming
}