Owin.HttpMessageHandler
=

An implementation of [System.Net.Http.HttpMessageHandler] that translates an [HttpRequestMessage] into an [OWIN] compatible environment dictionary, calls the supplied AppFunc and translates the result to an [HttpResponseMessage]. This allows you to call an OWIN application / component using [HttpClient] without actually hitting the network stack. Useful for testing and embedded scenarios.

This is distributed as two nuget packages:

```Owin.HttpMessageHandler``` contains two portable class libraries supporting the same platforms as HttpClient, including .Net 4+, Silverlight 4+ and Windows Phone 7.5+.

```Owin.HttpMessageHandler.Sources``` contains just the source code if you want include it in your app or library without taking on a package dependency.

Using
-
```csharp
Func<IDictionary<string, object>, Task> appFunc;
...
var httpClient = new HttpClient(new OwinHttpMessageHandler(appFunc));
```

By default, the OWIN enviroment is defined to look as though the source of the request is local. You can adjust the OWIN environment by passing in a closure:

```csharp
Func<IDictionary<string, object>, Task> appFunc;
...
var httpClient = new HttpClient(new OwinHttpMessageHandler(app, env =>
    {
        env[Constants.ServerRemoteIpAddressKey] ="10.1.1.1.";
    }));
```

More information on [Http Message Handlers]

Licence : [MIT]

Follow me [@randompunter]

  [System.Net.Http.HttpMessageHandler]: http://msdn.microsoft.com/en-us/library/system.net.http.httpmessagehandler.aspx
  [HttpRequestMessage]: http://msdn.microsoft.com/en-us/library/system.net.http.httprequestmessage.aspx
  [OWIN]: http://owin.org/
  [HttpResponseMessage]: http://msdn.microsoft.com/en-us/library/system.net.http.httpresponsemessage.aspx
  [HttpClient]: http://msdn.microsoft.com/en-us/library/system.net.http.httpclient.aspx
  [Http Message Handlers]: http://www.asp.net/web-api/overview/working-with-http/http-message-handlers
  [MIT]: http://opensource.org/licenses/MIT
  [@randompunter]: http://twitter.com/randompunter
