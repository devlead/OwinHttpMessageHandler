namespace OwinHttpMessageHandler.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Owin;
    using Owin.Builder;
    using Owin.Loader;
    using OwinHttpMessageHander;
    using Xunit;

    public class OwinHttpMessageHandlerTests
    {
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
    }
}