namespace OwinHttpMessageHander
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using CuttingEdge.Conditions;

    public class OwinHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<IDictionary<string, object>, Task> _appFunc;
        private readonly Action<IDictionary<string, object>> _modifyEnvironment;

        public OwinHttpMessageHandler(Func<IDictionary<string, object>, Task> appFunc,
                                      Action<IDictionary<string, object>> modifyEnvironment = null)
        {
            Condition.Requires(appFunc).IsNotNull();
            _appFunc = appFunc;
            _modifyEnvironment = modifyEnvironment ?? (env => { });
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
                                                               CancellationToken cancellationToken)
        {
            return request.ToEnvironmentAsync(cancellationToken)
                          .ContinueWith(task =>
                                            {
                                                var env = task.Result;
                                                _appFunc(env);
                                                return env.ToHttpResponseMessage(request);
                                            });
        }
    }
}