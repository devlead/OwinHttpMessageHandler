namespace Owin.HttpMessageHandler
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Owin;

    public class Startup
    {
        public void Configuration(IAppBuilder builder)
        {
            builder.Use<TestApp>();
        }

        private class TestApp : OwinMiddleware
        {
            private readonly Dictionary<string, Action<IOwinContext>> _responders = new Dictionary<string, Action<IOwinContext>>();

            public TestApp(OwinMiddleware next)
                : base(next)
            {
                _responders.Add("/OK", context => context.Response.StatusCode = 200);
                _responders.Add("/NotFound", context => context.Response.StatusCode = 404);
                _responders.Add("/greeting", context =>
                {
                    var form = context.Request.ReadFormAsync().Result;
                    context.Response.Write("Hello " + form["Name"]);
                });
            }

            public override Task Invoke(IOwinContext context)
            {
                _responders[context.Request.Path.Value](context);
                return Task.FromResult((object)null);
            }
        }
    }
}