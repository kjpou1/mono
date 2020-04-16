using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using ClientWebSocketOptions = WebAssembly.Net.WebSockets.ClientWebSocket.ClientWebSocketOptions;

namespace WebAssembly.Services.IServices {
	public interface IWebSocketHandleService {

		#region Properties

		WebSocketCloseStatus? CloseStatus { get; }
		string CloseStatusDescription { get; }
		string SubProtocol { get; }
		WebSocketState State { get; }
		ClientWebSocketOptions Options { get;  }
		
		#endregion Properties

		void Abort ();
		Task CloseAsync (WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
		Task CloseOutputAsync (WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken);
		Task ConnectAsync (Uri uri, CancellationToken cancellationToken);
		
		void Dispose ();
		Task<WebSocketReceiveResult> ReceiveAsync (ArraySegment<byte> buffer, CancellationToken cancellationToken);
		Task SendAsync (ArraySegment<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
		//ValueTask SendAsync (ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken);
    
	}
}
