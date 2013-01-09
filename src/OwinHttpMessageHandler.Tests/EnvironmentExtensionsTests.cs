namespace OwinHttpMessageHandler.Tests
{
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using OwinHttpMessageHander;
    using Xunit;

    public class EnvironmentExtensionsTests
    {
        [Fact]
        public void Can_convert_request_to_environment()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://example.com:8080/path?x=y")
                          {
                              Content = new StringContent("foo")
                          };
            IDictionary<string, object> env = request.ToEnvironment(new CancellationToken());

            Assert.NotNull(env[Constants.VersionKey]);
            Assert.NotNull(env[Constants.CallCancelledKey]);
            Assert.Equal("127.0.0.1", env.Get<string>(Constants.ServerRemoteIpAddressKey));
            Assert.Equal("1024", env.Get<string>(Constants.ServerRemotePortKey));
            Assert.Equal("127.0.0.1", env.Get<string>(Constants.ServerLocalIpAddressKey));
            Assert.Equal("8080", env.Get<string>(Constants.ServerLocalPortKey));
            Assert.NotNull(env.Get<IList<IDictionary<string, object>>>(Constants.ServerCapabilities));
            Assert.Equal(true, env.Get<bool>(Constants.ServerIsLocalKey));
            Assert.Equal("GET", env.Get<string>(Constants.RequestMethodKey));
            Assert.Equal("https", env.Get<string>(Constants.RequestSchemeKey));
            Assert.Equal("/path", env.Get<string>(Constants.RequestPathKey));
            Assert.Equal("x=y", env.Get<string>(Constants.RequestQueryStringKey));
        }
    }

    public static class DictionaryExtensions
    {
        public static T Get<T>(this IDictionary<string, object> dict, string key)
        {
            return (T) dict[key];
        }
    }
}