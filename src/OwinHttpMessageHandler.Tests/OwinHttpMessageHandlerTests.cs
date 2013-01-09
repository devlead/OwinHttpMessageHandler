namespace Owin.Testing.Tests
{
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Builder;
    using Loader;
    using Xunit;

    public class OwinHttpMessageHandlerTests
    {
        private readonly HttpClient _sut;

        public OwinHttpMessageHandlerTests()
        {
            var appBuilder = new AppBuilder();
            new DefaultLoader().Load(typeof(Startup).FullName)(appBuilder);
            var app = appBuilder.Build();
            _sut = new HttpClient(new OwinHttpMessageHandler(app));
        }

        [Fact]
        public async Task Blah()
        {
            HttpResponseMessage response = await _sut.GetAsync("http://sample.com/hello");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}