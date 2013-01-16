namespace OwinHttpMessageHandler.Tests
{
    using System;
    using System.Collections.Generic;
    using Gate;
    using Owin;

    public class Startup
	{
        private readonly Dictionary<string, Action<Request, Response>> _responders = new Dictionary<string, Action<Request, Response>>();

	    public Startup()
	    {
	        _responders.Add("/OK", (request, response) => response.StatusCode = 200);
	        _responders.Add("/NotFound", (request, response) => response.StatusCode = 404);
	        _responders.Add("/greeting", (request, response) =>
	                                     {
	                                         IDictionary<string, string> form = request.ReadForm();
                                             response.Write("Hello "+ form["Name"]);
	                                     });
	    }

		public void Configuration(IAppBuilder builder)
		{
			builder.UseGate((request, response) => _responders[request.Path](request, response));
		}
	}
}