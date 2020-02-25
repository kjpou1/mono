using System;
using System.Runtime.InteropServices;

namespace WebAssembly {

	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		class AnyRef {

		public int JSHandle { get; internal set; }
		internal GCHandle Handle;

		internal AnyRef (int js_handle)
		{
			//Console.WriteLine ($"AnyRef: {js_handle}");
			this.JSHandle = js_handle;
			this.Handle = GCHandle.Alloc (this);
		}

		internal AnyRef (IntPtr js_handle)
		{
			this.JSHandle = (int)js_handle;
			this.Handle = GCHandle.Alloc (this);
		}
	}
}
