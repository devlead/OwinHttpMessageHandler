namespace Samples
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.Owin.Builder;
    using Owin;
    using Xunit;

    public class TestingMsOwinStartup
    {
        [Fact]
        public async Task Should_get_OK()
        {
            var app = new AppBuilder();
            new ExampleStartup().Configuration(app);
            var appFunc = app.Build();

            var handler = new OwinHttpMessageHandler(appFunc)
            {
                UseCookies = true,
                AllowAutoRedirect = true
            };
            using(var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            })
            {
                var response = await httpClient.GetAsync("/test");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        public class ExampleStartup
        {
            public void Configuration(IAppBuilder app)
            {
                app.Use((context, next) =>
                {
                    if(context.Request.Uri.AbsolutePath == "/test")
                    {
                        context.Response.StatusCode = 200;
                        context.Response.ReasonPhrase = "OK";
                        return Task.FromResult(0);
                    }
                    return next();
                });
            }
        }
    }
}