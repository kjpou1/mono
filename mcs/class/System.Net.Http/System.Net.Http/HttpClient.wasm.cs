namespace System.Net.Http
{
	public partial class HttpClient
	{
		static HttpMessageHandler CreateDefaultHandler () => new HttpClientHandler ();
	}
}
