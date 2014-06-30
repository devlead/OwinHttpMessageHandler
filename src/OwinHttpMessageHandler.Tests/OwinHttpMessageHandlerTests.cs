namespace System.Net.Http
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Microsoft.Owin;
    using Xunit;

    using AppFunc = System.Func<Collections.Generic.IDictionary<string, object>, System.Threading.Tasks.Task>;

    // ReSharper disable InconsistentNaming

    public class OwinHttpMessageHandlerTests
    {
        private readonly HttpClient _sut;
        private bool _onSendingHeadersCalled;

        public OwinHttpMessageHandlerTests()
        {
            var _responders = new Dictionary<string, Action<IOwinContext>>
            {
                {"/OK", context => context.Response.StatusCode = 200},
                {"/NotFound", context => context.Response.StatusCode = 404},
                {"/greeting", context =>
                    {
                        var form = context.Request.ReadFormAsync().Result;
                        context.Response.Write("Hello " + form["Name"]);
                    }
                }
            };
            AppFunc appFunc = env =>
            {
                var context = new OwinContext(env);
                context.Response.OnSendingHeaders(_ => _onSendingHeadersCalled = true, null);
                _responders[context.Request.Path.Value](context);
                return Task.FromResult((object) null);
            };

            _sut = new HttpClient(new OwinHttpMessageHandler(appFunc) { UseCookies = true });
        }

        [Fact]
        public void When_appfunc_paramater_is_null_Then_should_throw()
        {
            Action act = () => { var handler = new OwinHttpMessageHandler(null); };
            act.ShouldThrow<ArgumentNullException>();
        }

        [Fact]
        public async Task Should_get_status_OK()
        {
            (await _sut.GetAsync("http://sample.com/OK")).StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task Should_get_status_NotFound()
        {
            (await _sut.GetAsync("http://sample.com/NotFound")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Should_call_OnSendingHeaders()
        {
            await _sut.GetAsync("http://sample.com/OK");
            _onSendingHeadersCalled.Should().BeTrue();
        }

        [Fact]
        public async Task Should_handle_form_data()
        {
            HttpResponseMessage response = await _sut.PostAsync("http://sample.com/greeting",
                               new FormUrlEncodedContent(new List<KeyValuePair<string, string>>
                                                         {
                                                             new KeyValuePair<string, string>("Name", "Damian")
                                                         }));
            (await response.Content.ReadAsStringAsync()).Should().Be("Hello Damian");
        }
    }
    // ReSharper restore InconsistentNaming
}