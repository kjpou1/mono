using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using WebAssembly.Services;
using IHttpHandlerService = WebAssembly.Services.IServices.IHttpHandlerService;

namespace System.Net.Http
{
	/// <summary>
	/// <see cref="WebAssemblyHttpHandler" /> is a specialty message handler based on the
	/// Fetch API for use in WebAssembly environments.
	/// </summary>
	/// <remarks>See https://developer.mozilla.org/en-US/docs/Web/API/Fetch_API</remarks>
	public class WebAssemblyHttpHandler : HttpMessageHandler {

		bool disposed;
		IHttpHandlerService handler;

		internal WebAssembly.Services.IServices.IHttpHandlerService InnerHandler {
			get {
				return handler;
			}
			set {
				if (value == null)
					throw new ArgumentNullException ("InnerHandler");

				handler = value;
			}
		}

		public WebAssemblyHttpHandler () : this (WebAssembly.Runtime.LocateService<IHttpHandlerService> ())
		{ }


		internal WebAssemblyHttpHandler (WebAssembly.Services.IServices.IHttpHandlerService innerHandler)
		{
			if (innerHandler == null)
				throw new ArgumentNullException ("innerHandler");

			InnerHandler = innerHandler;
		}

		public static void ConfigureServices () {
			WebAssembly.Runtime.Register<HttpMessageHandler, WebAssemblyHttpHandler> ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				disposed = true;
				if (InnerHandler != null)
					InnerHandler.Dispose ();
			}

			base.Dispose (disposing);
		}

		protected override Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
			if (InnerHandler == null) {
				throw new InvalidOperationException ("InnerHandler was not assigned to.");
			}
			return InnerHandler.SendAsync (request, cancellationToken);
		}

	}
}
