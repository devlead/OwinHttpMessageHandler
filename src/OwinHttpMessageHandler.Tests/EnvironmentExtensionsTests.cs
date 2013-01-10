namespace OwinHttpMessageHandler.Tests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using OwinHttpMessageHander;
    using Xunit;
    // ReSharper disable InconsistentNaming

    public class EnvironmentExtensionsTests
    {
        private readonly IDictionary<string, object> _sut;

        public EnvironmentExtensionsTests()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8080/path?x=y")
            {
                Content = new StringContent("foo")
            };
            _sut = request.ToEnvironment(new CancellationToken());
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
            Assert.Equal("GET", _sut.Get<string>(Constants.RequestMethodKey));
        }

        [Fact]
        public void Should_have_RequestPath()
        {
            Assert.Equal("/path", _sut.Get<string>(Constants.RequestPathKey));
        }

        [Fact]
        public void Should_have_RequestScheme()
        {
            Assert.Equal("https", _sut.Get<string>(Constants.RequestSchemeKey));
        }

        [Fact]
        public void Should_have_RequestQuery()
        {
            Assert.Equal("x=y", _sut.Get<string>(Constants.RequestQueryStringKey));
        }
      
    }
    // ReSharper restore InconsistentNaming

    public static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key)
        {
            return (T) dict[key];
        }
    }
}