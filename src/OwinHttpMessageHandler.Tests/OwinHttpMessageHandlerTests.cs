using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Owin;
using Owin.Builder;
using Owin.Loader;
using Xunit;

namespace OwinHttpMessageHandler.Tests
{
    public class OwinHttpMessageHandlerTests
    {
        private readonly HttpClient _sut;

        public OwinHttpMessageHandlerTests()
        {
            var appBuilder = new AppBuilder();
            new DefaultLoader().Load(typeof(Startup).FullName)(appBuilder);
            var app = appBuilder.Build();
            _sut = new HttpClient(new OwinHttpMessageHander.OwinHttpMessageHandler(app));
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
    }
}