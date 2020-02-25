using System;
namespace WebAssembly.Host {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		interface IHostObject {

	}
}
