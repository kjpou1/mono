

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using WebAssembly.Services.IServices;
using WebAssembly.Services.WebSockets;
using ClientWebSocketOptions = WebAssembly.Net.WebSockets.ClientWebSocket.ClientWebSocketOptions;

namespace WebAssembly.Net.WebSockets {

	/// <summary>
	/// Provides a client for connecting to WebSocket services.
	/// </summary>
	public sealed class ClientWebSocket : WebSocket {

		readonly IWebSocketHandleService websocketHandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> class.
		/// </summary>
		public ClientWebSocket () : this(WebAssembly.Runtime.LocateService<IWebSocketHandleService> ())
		{ }

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> class.
		/// </summary>
		public ClientWebSocket (IWebSocketHandleService websocketHandle)
		{
			this.websocketHandle = websocketHandle;
		}

		#region Properties

		public ClientWebSocketOptions Options => websocketHandle.Options;

		/// <summary>
		/// Gets the WebSocket state of the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> instance.
		/// </summary>
		/// <value>The state.</value>
		public override WebSocketState State => websocketHandle.State;

		/// <summary>
		/// Gets the reason why the close handshake was initiated on <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> instance.
		/// </summary>
		/// <value>The close status.</value>
		public override WebSocketCloseStatus? CloseStatus => websocketHandle.CloseStatus;

		/// <summary>
		/// Gets a description of the reason why the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> instance was closed.
		/// </summary>
		/// <value>The close status description.</value>
		public override string CloseStatusDescription => websocketHandle.CloseStatusDescription;

		/// <summary>
		/// Gets the supported WebSocket sub-protocol for the <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/>s instance.
		/// </summary>
		/// <value>The sub protocol.</value>
		public override string SubProtocol => websocketHandle.SubProtocol;


		#endregion Properties

		/// <summary>
		/// Connect to a WebSocket server as an asynchronous operation.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="uri">URI.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public Task ConnectAsync (Uri uri, CancellationToken cancellationToken) => websocketHandle.ConnectAsync (uri, cancellationToken);
		
		public override void Dispose ()
		{
			websocketHandle?.Dispose ();
		}

		/// <summary>
		/// Send data on <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> as an asynchronous operation.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="buffer">Buffer.</param>
		/// <param name="messageType">Message type.</param>
		/// <param name="endOfMessage">If set to <c>true</c> end of message.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override Task SendAsync (ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken) =>
			websocketHandle.SendAsync (buffer, messageType, endOfMessage, cancellationToken);

		/// <summary>
		/// Receives data on <see cref="T:WebAssembly.Net.WebSockets.ClientWebSocket"/> as an asynchronous operation.
		/// </summary>
		/// <returns>The async.</returns>
		/// <param name="buffer">Buffer.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		public override Task<WebSocketReceiveResult> ReceiveAsync (ArraySegment<byte> buffer, CancellationToken cancellationToken) =>
			websocketHandle.ReceiveAsync (buffer, cancellationToken);

		/// <summary>
		/// Aborts the connection and cancels any pending IO operations.
		/// </summary>
		public override void Abort () => websocketHandle.Abort ();

		public override Task CloseAsync (WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
			websocketHandle.CloseAsync (closeStatus, statusDescription, cancellationToken);

		public override Task CloseOutputAsync (WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken) =>
			throw new NotImplementedException ();

		public sealed class ClientWebSocketOptions {
			private bool isReadOnly; // After ConnectAsync is called the options cannot be modified.
			private readonly IList<string> requestedSubProtocols;

			internal ClientWebSocketOptions ()
			{
				requestedSubProtocols = new List<string> ();
			}

			#region HTTP Settings

			// Note that some headers are restricted like Host.
			public void SetRequestHeader (string headerName, string headerValue)
			{
				throw new PlatformNotSupportedException ();
			}

			public bool UseDefaultCredentials {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public System.Net.ICredentials Credentials {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public System.Net.IWebProxy Proxy {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public X509CertificateCollection ClientCertificates {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public System.Net.Security.RemoteCertificateValidationCallback RemoteCertificateValidationCallback {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public System.Net.CookieContainer Cookies {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			#endregion HTTP Settings

			#region WebSocket Settings

			public void AddSubProtocol (string subProtocol)
			{
				ThrowIfReadOnly ();

				// Duplicates not allowed.
				foreach (string item in requestedSubProtocols) {
					if (string.Equals (item, subProtocol, StringComparison.OrdinalIgnoreCase)) {
						throw new ArgumentException ($"Duplicate protocal '{subProtocol}' not allowed", nameof (subProtocol));
					}
				}
				requestedSubProtocols.Add (subProtocol);
			}

			internal IList<string> RequestedSubProtocols { get { return requestedSubProtocols; } }

			public TimeSpan KeepAliveInterval {
				get => throw new PlatformNotSupportedException ();
				set => throw new PlatformNotSupportedException ();
			}

			public void SetBuffer (int receiveBufferSize, int sendBufferSize)
			{
				throw new NotImplementedException ();
			}

			public void SetBuffer (int receiveBufferSize, int sendBufferSize, ArraySegment<byte> buffer)
			{
				throw new PlatformNotSupportedException ();
			}

			#endregion WebSocket settings

			#region Helpers

			internal void SetToReadOnly ()
			{
				isReadOnly = true;
			}

			private void ThrowIfReadOnly ()
			{
				if (isReadOnly) {
					throw new InvalidOperationException ("WebSocket has already been started.");
				}
			}

			#endregion Helpers
		}
	}

}
