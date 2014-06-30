namespace System.Net.Http
{
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Owin;
    using Xunit;

    using AppFunc = System.Func<System.Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    public class CookieTests
    {
        [Fact]
        public async Task When_server_sets_cookie_should_have_in_cookie_container()
        {
            const string cookieName1 = "testcookie1";
            const string cookieName2 = "testcookie2";
            var uri = new Uri("http://localhost/");
            AppFunc appFunc = env =>
            {
                var context = new OwinContext(env);
                context.Response.Cookies.Append(cookieName1, "c1");
                context.Response.Cookies.Append(cookieName2, "c2");
                return Task.FromResult(0);
            };
            var handler = new OwinHttpMessageHandler(appFunc) { UseCookies = true };
            using (var client = new HttpClient(handler))
            {
                await client.GetAsync(uri);
            }

            handler
                .CookieContainer
                .GetCookies(uri)[cookieName1]
                .Should()
                .NotBeNull();

            handler
                .CookieContainer
                .GetCookies(uri)[cookieName1]
                .Value
                .Should()
                .Be("c1");

            handler
                .CookieContainer
                .GetCookies(uri)[cookieName2]
                .Should()
                .NotBeNull();

            handler
                .CookieContainer
                .GetCookies(uri)[cookieName2]
                .Value
                .Should()
                .Be("c2");
        }
    }
}
