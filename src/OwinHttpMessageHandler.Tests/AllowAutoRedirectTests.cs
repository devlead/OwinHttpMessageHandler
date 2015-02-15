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
        [Fact]
        public async Task Can_auto_redirect()
        {
            var responders = new Dictionary<string, Action<IOwinContext>>
            {
                { "/", context =>
                    {
                        context.Response.StatusCode = 302;
                        context.Response.ReasonPhrase = "Found";
                        context.Response.Headers.Add("Location", new [] { "http://localhost/redirect" });
                    }},
                { "/redirect", context => context.Response.StatusCode = 200 }
            };
            AppFunc appFunc = env =>
            {
                var context = new OwinContext(env);
                responders[context.Request.Path.Value](context);
                return Task.FromResult((object)null);
            };
            var handler = new OwinHttpMessageHandler(appFunc)
            {
                AllowAutoRedirect = true
            };

            using (var client = new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost")
            })
            {
                var response = await client.GetAsync("/");

                response.StatusCode.Should().Be(HttpStatusCode.OK);
                response.RequestMessage.RequestUri.AbsoluteUri.Should().Be("http://localhost/redirect");
            }
        }
    }
}