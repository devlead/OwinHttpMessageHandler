namespace System.Net.Http
{
    using System.Collections.Generic;
    using System.Linq;
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

        [Fact]
        public async Task Setting_cookie_when_headers_are_sent_then_should_have_cookie_in_container()
        {
            const string cookieName1 = "testcookie1";

            var uri = new Uri("http://localhost/");
            AppFunc appFunc = async env =>
            {
                var context = new OwinContext(env);
                context.Response.OnSendingHeaders(_ =>
                {
                    context.Response.Cookies.Append(cookieName1, "c1");
                }, null);
                await context.Response.WriteAsync("Test");
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
        }

        [Fact]
        public async Task Setting_cookie_when_headers_are_sent_then_should_have_cookie_in_container23()
        {
            const string cookieName1 = "testcookie1";

            var uri = new Uri("http://localhost/login");


            AppFunc appFunc = async env =>
            {
                var context = new OwinContext(env);

                context.Response.Headers.Append("Location", "/");

                await context.Response.WriteAsync("Test");
            };


            AppFunc cookie =  env =>
            {
                var context = new OwinContext(env);
                context.Response.OnSendingHeaders(_ =>
                {
                    context.Response.Cookies.Append("auth", "123");
                }, null);

                return appFunc(env);
            };

            AppFunc myMw = env =>
            {
                var context = new OwinContext(env);
                context.Response.OnSendingHeaders(_ =>
                {
                    if ((string)env["owin.RequestPath"] == "/login" && (string)env["owin.RequestMethod"] == "POST")
                    {
                        var responseHeaders = (IDictionary<string, string[]>)env["owin.ResponseHeaders"];
                        if (responseHeaders.ContainsKey("Set-Cookie"))
                        {
                            var setcookies = responseHeaders["Set-Cookie"].ToList();
                            var authcookie = setcookies.FirstOrDefault(x => x.StartsWith("auth"));
                            if (authcookie != null)
                            {
                                var authcookieValue = authcookie.Split(new[] { '=' }, StringSplitOptions.RemoveEmptyEntries)[1].Split(new[] { ';' })[0];
                                //Make a secure token. This is not a good example
                                var csrfToken = new String(authcookieValue.Reverse().ToArray());
                                setcookies.Add("XSRF-TOKEN=" + csrfToken + ";path=/");
                                responseHeaders["Set-Cookie"] = setcookies.ToArray();
                            }
                        }
                    }
                }, null);

                return cookie(env);
            };

            var handler = new OwinHttpMessageHandler(myMw) { UseCookies = true };
            using (var client = new HttpClient(handler))
            {
                await client.PostAsync(uri, new StringContent(""));
            }

            handler
                .CookieContainer
                .GetCookies(uri).Count
                .Should()
                .Be(2);
        }
    }
}
