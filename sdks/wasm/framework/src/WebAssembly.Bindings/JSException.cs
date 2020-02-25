using System;
namespace WebAssembly {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		class JSException : Exception {
		public JSException (string msg) : base (msg) { }
	}
}
