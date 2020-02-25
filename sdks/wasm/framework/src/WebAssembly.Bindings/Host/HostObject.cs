using System;
namespace WebAssembly.Host {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		class HostObject : HostObjectBase {
		public HostObject (string hostName, params object[] _params) : base (Runtime.New(hostName, _params))  
		{ }
	}
}
