using System;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace WebAssembly.Services.IServices {
	public interface IHttpHandlerService {
		Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken);
		void Dispose ();
	}
}