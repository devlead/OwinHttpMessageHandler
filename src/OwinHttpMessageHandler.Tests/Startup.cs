using System;
using System.Collections.Generic;
using Gate;
using Owin;

namespace OwinHttpMessageHandler.Tests
{
	public class Startup
	{
        private readonly Dictionary<string, Action<Request, Response>> _responders = new Dictionary<string, Action<Request, Response>>();

	    public Startup()
	    {
	        _responders.Add("/OK", (request, response) => response.StatusCode = 200);
	        _responders.Add("/NotFound", (request, response) => response.StatusCode = 404);
	    }

		public void Configuration(IAppBuilder builder)
		{
			builder.UseGate((request, response) => _responders[request.Path](request, response));
		}
	}
}