namespace Owin.HttpMessageHandler
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
    using FluentAssertions;
    using Microsoft.Owin.Hosting.Builder;
    using Owin;
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
                _sut[OwinHttpMessageHandler.Constants.Owin.VersionKey].Should().NotBeNull();
            }

            [Fact]
            public void Should_have_CallCanceled()
            {
                _sut[OwinHttpMessageHandler.Constants.Owin.CallCancelledKey].Should().NotBeNull();
            }

            [Fact]
            public void Should_have_ServerRemoteIpAddress()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Server.RemoteIpAddressKey).Should().Be("127.0.0.1");
            }

            [Fact]
            public void Should_have_ServerRemotePort()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Server.RemotePortKey).Should().Be("1024");
            }

            [Fact]
            public void Should_have_ServerLocalIpAddress()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Server.LocalIpAddressKey).Should().Be("127.0.0.1");
            }

            [Fact]
            public void Should_have_ServerLocalPort()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Server.LocalPortKey).Should().Be("8080");
            }

            [Fact]
            public void Should_have_empty_server_capabilities()
            {
                _sut.Get<IList<IDictionary<string, object>>>(OwinHttpMessageHandler.Constants.Server.ServerCapabilities)
                    .Should()
                    .BeEmpty();
            }

            [Fact]
            public void Should_have_IsLocal_true()
            {
                _sut.Get<bool>(OwinHttpMessageHandler.Constants.Server.IsLocalKey).Should().BeTrue();
            }

            [Fact]
            public void Should_have_RequestMethod()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestMethodKey).Should().Be(_request.Method.ToString().ToUpperInvariant());
            }

            [Fact]
            public void Should_have_RequestPath()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestPathKey).Should().Be(_request.RequestUri.AbsolutePath);
            }

            [Fact]
            public void Should_have_RequestQueryString()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestQueryStringKey).Should().Be("x=y");
            }

            [Fact]
            public void Should_have_RequestBody()
            {
                var stream = _sut.Get<Stream>(OwinHttpMessageHandler.Constants.Owin.RequestBodyKey);
                new StreamReader(stream).ReadToEnd().Should().Be("foo");
            }

            [Fact]
            public void Should_have_RequestHeaders()
            {
                _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.Owin.RequestHeadersKey)
                        .Should()
                        .NotBeEmpty();
            }

            [Fact]
            public void Should_have_ResponseHeaders()
            {
                _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.Owin.ResponseHeadersKey)
                    .Should()
                    .NotBeNull();
            }

            [Fact]
            public void Should_have_ContentTypeHeader()
            {
                _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.Owin.RequestHeadersKey)
                    .Should().ContainKey(OwinHttpMessageHandler.Constants.Headers.ContentType);
            }

            [Fact]
            public void Should_have_RequestHostHeader()
            {
                _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.Owin.RequestHeadersKey)
                    .Should().ContainKey(OwinHttpMessageHandler.Constants.Headers.Host);

                _sut.Get<IDictionary<string, string[]>>(OwinHttpMessageHandler.Constants.Owin.RequestHeadersKey)[OwinHttpMessageHandler.Constants.Headers.Host]
                    .Single().Should().Be("example.com:8080");
            }

            [Fact]
            public void Should_have_RequestPathBase()
            {
               _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestPathBaseKey).Should().NotBeNull();
            }

            [Fact]
            public void Should_have_RequestProtocol()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestProtocolKey).Should().Be("HTTP/1.1");
            }

            [Fact]
            public void Should_have_RequestScheme()
            {
                _sut.Get<string>(OwinHttpMessageHandler.Constants.Owin.RequestSchemeKey).Should().Be("https");
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
                              {OwinHttpMessageHandler.Constants.Owin.ResponseBodyKey, new MemoryStream().Write("foo")},
                              {
                                  OwinHttpMessageHandler.Constants.Owin.ResponseHeadersKey, new Dictionary<string, string[]>
                                                                {
                                                                    {OwinHttpMessageHandler.Constants.Headers.ContentLength, new[] {"3"}},
                                                                    {OwinHttpMessageHandler.Constants.Headers.Connection, new[] {"close"}}
                                                                }
                              },
                              {OwinHttpMessageHandler.Constants.Owin.ResponseStatusCodeKey, 302},
                              {OwinHttpMessageHandler.Constants.Owin.ResponseReasonPhraseKey, "302 Found"},
                              {OwinHttpMessageHandler.Constants.Owin.RequestProtocolKey, "HTTP/1.1"}
                          };
                _sut = OwinHttpMessageHandler.ToHttpResponseMessage(env, request);
            }

            [Fact]
            public void Should_have_StatusCode()
            {
                _sut.StatusCode.Should().Be(HttpStatusCode.Found);
            }

            [Fact]
            public void Should_have_ReasonPhrase()
            {
                _sut.ReasonPhrase.Should().Be("302 Found");
            }

            [Fact]
            public void Should_have_RequestMessage()
            {
                _sut.RequestMessage.Should().NotBeNull();
            }

            [Fact]
            public void Should_have_version()
            {
                _sut.Version.Should().Be(new Version(1, 1));
            }

            [Fact]
            public void Should_have_Headers()
            {
                _sut.Headers.Should().NotBeEmpty();
            }

            [Fact]
            public void Should_have_ContentHeaders()
            {
                _sut.Content.Headers.Should().NotBeEmpty();
            }

            [Fact]
            public void Should_have_Content()
            {
                _sut.Content.Should().NotBeNull();
            }
        }

        private readonly HttpClient _sut;
        private Action<IDictionary<string, object>> _beforeInvoke = _ => { };

        public OwinHttpMessageHandlerTests()
        {
            var appBuilder = new AppBuilderFactory().Create();
            new Startup().Configuration(appBuilder);
            var appFunc = (Func<IDictionary<string, object>, Task>)appBuilder.Build(typeof(Func<IDictionary<string, object>, Task>));
            _sut = new HttpClient(new OwinHttpMessageHandler(appFunc, beforeInvoke: env => _beforeInvoke(env)) { UseCookies = true });
        }

        [Fact]
        public void When_appfunc_paramater_is_null_Then_should_throw()
        {
            Action act = () => { new OwinHttpMessageHandler(null); };
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public async Task Should_get_status_OK()
        {
            (await _sut.GetAsync("http://sample.com/OK")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_get_status_NotFound()
        {
            (await _sut.GetAsync("http://sample.com/NotFound")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Should_call_OnSendingHeaders()
        {
            bool onSendingHeadersCalled = false;
            _beforeInvoke = env =>
            {
                var action = env.Get<Action<Action<object>, object>>(OwinHttpMessageHandler.Constants.Server.OnSendingHeadersKey);
                action(_ => onSendingHeadersCalled = true, null);
            };
            await _sut.GetAsync("http://sample.com/OK");
            onSendingHeadersCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Should_handle_form_data()
        {
            HttpResponseMessage response = await _sut.PostAsync("http://sample.com/greeting",
                               new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                                                         {
                                                             new KeyValuePair<string, string>("Name", "Damian")
                                                         }));
            (await response.Content.ReadAsStringAsync()).Should().Be("Hello Damian");
        }
    }
    // ReSharper restore InconsistentNaming
}