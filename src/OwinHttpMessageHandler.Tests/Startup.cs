namespace OwinHttpMessageHandler.Tests
{
    using System;
    using System.Collections.Generic;
    using Owin;
    using Owin.Types;

    public class Startup
    {
        private readonly Dictionary<string, Action<OwinRequest, OwinResponse>> _responders =
            new Dictionary<string, Action<OwinRequest, OwinResponse>>();

        public Startup()
        {
            _responders.Add("/OK", (request, response) => response.StatusCode = 200);
            _responders.Add("/NotFound", (request, response) => response.StatusCode = 404);
            _responders.Add("/greeting", (request, response) =>
                                         {
                                             IDictionary<string, string> form = request.ReadForm();
                                             response.Write("Hello " + form["Name"]);
                                         });
        }

        public void Configuration(IAppBuilder builder)
        {
            builder.UseFilter(request =>  _responders[request.Path](request, new OwinResponse(request)));
        }
    }
}