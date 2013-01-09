namespace Owin.Testing.Tests
{
	public class Startup
	{
		public void Configuration(IAppBuilder builder)
		{
			builder.UseGate((request, response) => response.Write("world"));
		}
	}
}