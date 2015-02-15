namespace System.Net.Http
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Owin;
    using Xunit;

    using AppFunc = System.Func<
        Collections.Generic.IDictionary<string, object>,
        System.Threading.Tasks.Task>;

    public class AllowAutoRedirectTests
    {
        private OwinHttpMessageHandler _handler;

        public AllowAutoRedirectTests()
        {
            var responders = new Dictionary<string, Action<IOwinContext>>
            {
                { "/redirect-absolute", context =>
                    {
                        context.Response.StatusCode = 302;
                        context.Response.ReasonPhrase = "Found";
                        context.Response.Headers.Add("Location", new [] { "http://localhost/redirect" });
                    }
                },
                { "/redirect-relative", context =>
                    {
                        context.Response.StatusCode = 302;
                        context.Response.ReasonPhrase = "Found";
                        context.Response.Headers.Add("Location", new [] { "redirect" });
                    }
                },
                { "/redirect", context => context.Response.StatusCode = 200 }
            };
            AppFunc appFunc = env =>
            {
                var context = new OwinContext(env);
                responders[context.Request.Path.Value](context);
                return Task.FromResult((object)null);
            };
            _handler = new OwinHttpMessageHandler(appFunc)
            {
                AllowAutoRedirect = true
            };
        }

        [Fact]
        public async Task Can_auto_redirect_with_absolute_location()
        {
            using (var client = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost")
            })
            {
                var response = await client.GetAsync("/redirect-absolute");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.RequestMessage.RequestUri.AbsoluteUri.Should().Be("http://localhost/redirect");
            }
        }

        [Fact]
        public async Task Can_auto_redirect_with_relative_location()
        {
            using (var client = new HttpClient(_handler)
            {
                BaseAddress = new Uri("http://localhost")
            })
            {
                var response = await client.GetAsync("/redirect-relative");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.RequestMessage.RequestUri.AbsoluteUri.Should().Be("http://localhost/redirect");
            }
        }
    }
}