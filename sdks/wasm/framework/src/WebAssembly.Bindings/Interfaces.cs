using System;


namespace WebAssembly {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		interface IJSObject {
		int JSHandle { get; }
		int Length { get; }
	}
}


namespace WebAssembly.Core {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		interface ITypedArray {
		int BytesPerElement { get; }
		string Name { get; }
		int ByteLength { get; }
		ArrayBuffer Buffer { get; }

		void Set (Array array);
		void Set (Array array, int offset);

		void Set (ITypedArray typedArray);
		void Set (ITypedArray typedArray, int offset);
		TypedArrayTypeCode GetTypedArrayType ();
	}

	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		interface ITypedArray<T, U> where U : struct {

		T Slice ();
		T Slice (int begin);
		T Slice (int begin, int end);

		T SubArray ();
		T SubArray (int begin);
		T SubArray (int begin, int end);

	}
}
