#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebAssembly.Core;
using WebAssembly.Host;

using IHttpHandlerService = WebAssembly.Services.IServices.IHttpHandlerService;

namespace WebAssembly.Services {

	public class WebAssemblyHttpHandlerService : IHttpHandlerService, IDisposable {

		private static readonly JSObject? s_fetch = (JSObject)WebAssembly.Runtime.GetGlobalObject ("fetch");
		private static readonly JSObject? s_window = (JSObject)WebAssembly.Runtime.GetGlobalObject ("window");

		/// <summary>
		/// Gets whether the current Browser supports streaming responses
		/// </summary>
		private static bool StreamingSupported { get; }

		public WebAssemblyHttpHandlerService ()
		{ }

		static WebAssemblyHttpHandlerService ()
		{
			using (var streamingSupported = new Function ("return typeof Response !== 'undefined' && 'body' in Response.prototype && typeof ReadableStream === 'function'"))
				StreamingSupported = (bool)streamingSupported.Call ();
		}

		public async Task<HttpResponseMessage> SendAsync (HttpRequestMessage request, CancellationToken cancellationToken)
		{
                        try {
                                var requestObject = new JSObject ();

                                if (request.Properties.TryGetValue ("WebAssemblyFetchOptions", out object? fetchOptionsValue) &&
                                    fetchOptionsValue is IDictionary<string, object> fetchOptions) {
                                        foreach (KeyValuePair<string, object> item in fetchOptions) {
                                                requestObject.SetObjectProperty (item.Key, item.Value);
                                        }
                                }

                                requestObject.SetObjectProperty ("method", request.Method.Method);

                                // We need to check for body content
                                if (request.Content != null) {
                                        if (request.Content is StringContent) {
                                                requestObject.SetObjectProperty ("body", await request.Content.ReadAsStringAsync ().ConfigureAwait (continueOnCapturedContext: true));
                                        } else {
                                                using (Uint8Array uint8Buffer = Uint8Array.From (await request.Content.ReadAsByteArrayAsync ().ConfigureAwait (continueOnCapturedContext: true))) {
                                                        requestObject.SetObjectProperty ("body", uint8Buffer);
                                                }
                                        }
                                }

                                // Process headers
                                // Cors has its own restrictions on headers.
                                // https://developer.mozilla.org/en-US/docs/Web/API/Headers
                                using (HostObject jsHeaders = new HostObject ("Headers")) {
                                        foreach (KeyValuePair<string, IEnumerable<string>> header in request.Headers) {
                                                foreach (string value in header.Value) {
                                                        jsHeaders.Invoke ("append", header.Key, value);
                                                }
                                        }
                                        if (request.Content != null) {
                                                foreach (KeyValuePair<string, IEnumerable<string>> header in request.Content.Headers) {
                                                        foreach (string value in header.Value) {
                                                                jsHeaders.Invoke ("append", header.Key, value);
                                                        }
                                                }
                                        }
                                        requestObject.SetObjectProperty ("headers", jsHeaders);
                                }


                                WasmHttpReadStream? wasmHttpReadStream = null;

                                JSObject abortController = new HostObject ("AbortController");
                                JSObject signal = (JSObject)abortController.GetObjectProperty ("signal");
                                requestObject.SetObjectProperty ("signal", signal);
                                signal.Dispose ();

                                using (CancellationTokenSource abortCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
                                        CancellationTokenRegistration abortRegistration = abortCts.Token.Register ((Action)(() => {
                                                if (abortController.JSHandle != -1) {
                                                        abortController.Invoke ("abort");
                                                        abortController?.Dispose ();
                                                }
                                                wasmHttpReadStream?.Dispose ();
                                        }));

                                        var args = new WebAssembly.Core.Array ();
                                        if (request.RequestUri != null) {
                                                args.Push (request.RequestUri.ToString ());
                                                args.Push (requestObject);
                                        }

                                        requestObject.Dispose ();

                                        var response = s_fetch?.Invoke ("apply", s_window, args) as Task<object>;
                                        args.Dispose ();
                                        if (response == null)
                                                throw new Exception ("Internal error marshalling the response Promise from `fetch`.");

                                        JSObject t = (JSObject)await response.ConfigureAwait (continueOnCapturedContext: true);

                                        //var status = new WasmFetchResponse (t, abortController, abortCts, abortRegistration);
                                        var status = new WasmFetchResponse (t, abortController, abortRegistration);

                                        HttpResponseMessage httpResponse = new HttpResponseMessage ((HttpStatusCode)Enum.Parse (typeof (HttpStatusCode), status.Status.ToString ()));

                                        bool streamingEnabled = request.Properties.TryGetValue ("WebAssemblyEnableStreamingResponse", out object? streamingEnabledValue) && (bool)(streamingEnabledValue ?? false);

                                        httpResponse.Content = StreamingSupported && streamingEnabled
                                            ? new StreamContent (wasmHttpReadStream = new WasmHttpReadStream (status))
                                            : (HttpContent)new BrowserHttpContent (status);

                                        // Fill the response headers
                                        // CORS will only allow access to certain headers.
                                        // If a request is made for a resource on another origin which returns the CORs headers, then the type is cors.
                                        // cors and basic responses are almost identical except that a cors response restricts the headers you can view to
                                        // `Cache-Control`, `Content-Language`, `Content-Type`, `Expires`, `Last-Modified`, and `Pragma`.
                                        // View more information https://developers.google.com/web/updates/2015/03/introduction-to-fetch#response_types
                                        //
                                        // Note: Some of the headers may not even be valid header types in .NET thus we use TryAddWithoutValidation
                                        using (JSObject respHeaders = status.Headers) {
                                                if (respHeaders != null) {
                                                        using (var entriesIterator = (JSObject)respHeaders.Invoke ("entries")) {
                                                                JSObject? nextResult = null;
                                                                try {
                                                                        nextResult = (JSObject)entriesIterator.Invoke ("next");
                                                                        while (!(bool)nextResult.GetObjectProperty ("done")) {
                                                                                using (var resultValue = (WebAssembly.Core.Array)nextResult.GetObjectProperty ("value")) {
                                                                                        var name = (string)resultValue [0];
                                                                                        var value = (string)resultValue [1];
                                                                                        if (!httpResponse.Headers.TryAddWithoutValidation (name, value))
                                                                                                httpResponse.Content.Headers.TryAddWithoutValidation (name, value);
                                                                                }
                                                                                nextResult?.Dispose ();
                                                                                nextResult = (JSObject)entriesIterator.Invoke ("next");
                                                                        }
                                                                } finally {
                                                                        nextResult?.Dispose ();
                                                                }
                                                        }
                                                }
                                        }
                                        return httpResponse;
                                }

                        } catch (JSException jsExc) {
                                throw new System.Net.Http.HttpRequestException (jsExc.Message);
                        }
                }

                private class WasmFetchResponse : IDisposable {
                        private readonly JSObject _fetchResponse;
                        private readonly JSObject _abortController;
                        //private readonly CancellationTokenSource _abortCts;
                        private readonly CancellationTokenRegistration _abortRegistration;

                        //public WasmFetchResponse (JSObject fetchResponse, JSObject abortController, CancellationTokenSource abortCts, CancellationTokenRegistration abortRegistration)
                        public WasmFetchResponse (JSObject fetchResponse, JSObject abortController, CancellationTokenRegistration abortRegistration)
                        {
                                _fetchResponse = fetchResponse ?? throw new ArgumentNullException (nameof (fetchResponse));
                                _abortController = abortController ?? throw new ArgumentNullException (nameof (abortController));
                                //_abortCts = abortCts;
                                _abortRegistration = abortRegistration;
                        }

                        public bool IsOK => (bool)_fetchResponse.GetObjectProperty ("ok");
                        public bool IsRedirected => (bool)_fetchResponse.GetObjectProperty ("redirected");
                        public int Status => (int)_fetchResponse.GetObjectProperty ("status");
                        public string StatusText => (string)_fetchResponse.GetObjectProperty ("statusText");
                        public string ResponseType => (string)_fetchResponse.GetObjectProperty ("type");
                        public string Url => (string)_fetchResponse.GetObjectProperty ("url");
                        public bool IsBodyUsed => (bool)_fetchResponse.GetObjectProperty ("bodyUsed");
                        public JSObject Headers => (JSObject)_fetchResponse.GetObjectProperty ("headers");
                        public JSObject Body => (JSObject)_fetchResponse.GetObjectProperty ("body");

                        public Task<object> ArrayBuffer () => (Task<object>)_fetchResponse.Invoke ("arrayBuffer");
                        public Task<object> Text () => (Task<object>)_fetchResponse.Invoke ("text");
                        public Task<object> JSON () => (Task<object>)_fetchResponse.Invoke ("json");

                        public void Dispose ()
                        {
                                // Dispose of unmanaged resources.
                                Dispose (true);
                        }

                        // Protected implementation of Dispose pattern.
                        protected virtual void Dispose (bool disposing)
                        {
                                if (disposing) {
                                        // Free any other managed objects here.
                                        //
                                        //_abortCts.Cancel ();
                                        _abortRegistration.Dispose ();
                                }

                                // Free any unmanaged objects here.
                                //
                                _fetchResponse?.Dispose ();
                                _abortController?.Dispose ();
                        }

                }

                private sealed class BrowserHttpContent : HttpContent {
                        private byte []? _data;
                        private readonly WasmFetchResponse _status;

                        public BrowserHttpContent (WasmFetchResponse status)
                        {
                                _status = status ?? throw new ArgumentNullException (nameof (status));
                        }

                        private async Task<byte []> GetResponseData ()
                        {
                                if (_data != null) {
                                        return _data;
                                }

                                using (ArrayBuffer dataBuffer = (ArrayBuffer)await _status.ArrayBuffer ().ConfigureAwait (continueOnCapturedContext: true)) {
                                        using (Uint8Array dataBinView = new Uint8Array (dataBuffer)) {
                                                _data = dataBinView.ToArray ();
                                                _status.Dispose ();
                                        }
                                }

                                return _data;
                        }

                        protected override async Task<Stream> CreateContentReadStreamAsync ()
                        {
                                byte [] data = await GetResponseData ().ConfigureAwait (continueOnCapturedContext: true);
                                return new MemoryStream (data, writable: false);
                        }

                        protected override async Task SerializeToStreamAsync (Stream stream, TransportContext? context)
                        {
                                byte [] data = await GetResponseData ().ConfigureAwait (continueOnCapturedContext: true);
                                await stream.WriteAsync (data, 0, data.Length).ConfigureAwait (continueOnCapturedContext: true);
                        }

                        protected override bool TryComputeLength (out long length)
                        {
                                if (_data != null) {
                                        length = _data.Length;
                                        return true;
                                }

                                length = 0;
                                return false;
                        }

                        protected override void Dispose (bool disposing)
                        {
                                _status?.Dispose ();
                                base.Dispose (disposing);
                        }
                }

                private sealed class WasmHttpReadStream : Stream {
                        private WasmFetchResponse? _status;
                        private JSObject? _reader;

                        private byte []? _bufferedBytes;
                        private int _position;

                        public WasmHttpReadStream (WasmFetchResponse status)
                        {
                                _status = status;
                        }

                        public override bool CanRead => true;
                        public override bool CanSeek => false;
                        public override bool CanWrite => false;
                        public override long Length => throw new NotSupportedException ();
                        public override long Position {
                                get => throw new NotSupportedException ();
                                set => throw new NotSupportedException ();
                        }

                        public override async Task<int> ReadAsync (byte [] buffer, int offset, int count, CancellationToken cancellationToken)
                        {
                                if (buffer == null) {
                                        throw new ArgumentNullException (nameof (buffer));
                                }
                                if (offset < 0) {
                                        throw new ArgumentOutOfRangeException (nameof (offset));
                                }
                                if (count < 0 || buffer.Length - offset < count) {
                                        throw new ArgumentOutOfRangeException (nameof (count));
                                }

                                if (_reader == null) {
                                        // If we've read everything, then _reader and _status will be null
                                        if (_status == null) {
                                                return 0;
                                        }

                                        try {
                                                using (JSObject body = _status.Body) {
                                                        _reader = (JSObject)body.Invoke ("getReader");
                                                }
                                        } catch (JSException) {
                                                cancellationToken.ThrowIfCancellationRequested ();
                                                throw;
                                        }
                                }

                                if (_bufferedBytes != null && _position < _bufferedBytes.Length) {
                                        return ReadBuffered ();
                                }

                                try {
                                        var t = (Task<object>)_reader.Invoke ("read");
                                        using (var read = (JSObject)await t.ConfigureAwait (continueOnCapturedContext: true)) {
                                                if ((bool)read.GetObjectProperty ("done")) {
                                                        _reader.Dispose ();
                                                        _reader = null;

                                                        _status?.Dispose ();
                                                        _status = null;
                                                        return 0;
                                                }

                                                _position = 0;
                                                // value for fetch streams is a Uint8Array
                                                using (Uint8Array binValue = (Uint8Array)read.GetObjectProperty ("value"))
                                                        _bufferedBytes = binValue.ToArray ();
                                        }
                                } catch (JSException) {
                                        cancellationToken.ThrowIfCancellationRequested ();
                                        throw;
                                }

                                return ReadBuffered ();

                                int ReadBuffered ()
                                {
                                        int n = _bufferedBytes.Length - _position;
                                        if (n > count)
                                                n = count;
                                        if (n <= 0)
                                                return 0;

                                        Buffer.BlockCopy (_bufferedBytes, _position, buffer, offset, n);
                                        _position += n;

                                        return n;
                                }
                        }

                        protected override void Dispose (bool disposing)
                        {
                                _reader?.Dispose ();
                                _status?.Dispose ();
                        }

                        public override void Flush ()
                        {
                        }

                        public override int Read (byte [] buffer, int offset, int count)
                        {
                                throw new NotSupportedException ("Synchronous reads are not supported, use ReadAsync instead");
                        }

                        public override long Seek (long offset, SeekOrigin origin)
                        {
                                throw new NotSupportedException ();
                        }

                        public override void SetLength (long value)
                        {
                                throw new NotSupportedException ();
                        }

                        public override void Write (byte [] buffer, int offset, int count)
                        {
                                throw new NotSupportedException ();
                        }
                }

                #region IDisposable Support
                private bool disposedValue = false; // To detect redundant calls

		protected virtual void Dispose (bool disposing)
		{
			if (!disposedValue) {
				if (disposing) {
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				disposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~WebAssemblyHttpHandlerService()
		// {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose ()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose (true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
