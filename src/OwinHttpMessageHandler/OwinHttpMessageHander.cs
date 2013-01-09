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
            IDictionary<string, object> env = request.ToEnvironment(cancellationToken);
            _modifyEnvironment(request.ToEnvironment(cancellationToken));
            return Task.Run(() =>
                            {
                                _appFunc(env);
                                return env.ToHttpResponseMessage(request);
                            }, cancellationToken);
        }
    }
}