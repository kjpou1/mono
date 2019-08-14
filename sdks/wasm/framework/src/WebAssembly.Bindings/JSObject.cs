﻿using System;

namespace WebAssembly {

	/// <summary>
	///   JSObjects are wrappers for a native JavaScript object, and
	///   they retain a reference to the JavaScript object for the lifetime of this C# object.
	/// </summary>
	public class JSObject : AnyRef, IJSObject, IDisposable {
		internal object RawObject;

		// Right now this is used for Delegates
		internal WeakReference WeakRawObject;

		// to detect redundant calls
		public bool IsDisposed { get; internal set; }

		public JSObject () : this (Runtime.New<Object> (), true)
		{
			var result = Runtime.BindCoreObject (JSHandle, (int)(IntPtr)AnyRefHandle, out int exception);
			if (exception != 0)
				throw new JSException ($"JSObject Error binding: {result.ToString ()}");

		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:WebAssembly.JSObject"/> class.
		/// </summary>
		/// <param name="js_handle">Js handle.</param>
		internal JSObject (IntPtr js_handle, bool ownsHandle) : base (js_handle, ownsHandle)
		{
			//Console.WriteLine ($"JSObject IntPtr: {js_handle} / ownshandle {ownsHandle}");
		}

		internal JSObject (int js_handle, bool ownsHandle) : base ((IntPtr)js_handle, ownsHandle)
		{
			//Console.WriteLine ($"JSObject int: {js_handle} / ownshandle {ownsHandle}");
		}

		internal JSObject (int js_handle, object raw_obj) : base (js_handle, false)
		{
			//Console.WriteLine ($"JSObject: {js_handle} / ownshandle {false} / rawobject {raw_obj}");
			RawObject = raw_obj;
		}

		/// <returns>
		///   <para>
		///     The return value can either be a primitive (string, int, double), a <see
		///     <see cref="T:WebAssembly.JSObject"/> for JavaScript objects, a 
		///     <see cref="T:System.Threading.Tasks.Task"/>(object) for JavaScript promises, an array of
		///     a byte, int or double (for Javascript objects typed as ArrayBuffer) or a 
		///     <see cref="T:System.Func"/> to represent JavaScript functions.  The specific version of
		///     the Func that will be returned depends on the parameters of the Javascript function
		///     and return value.
		///   </para>
		///   <para>
		///     The value of a returned promise (The Task(object) return) can in turn be any of the above
		///     valuews.
		///   </para>
		/// </returns>
		public object Invoke (string method, params object [] args)
		{
			var res = Runtime.InvokeJSWithArgs (JSHandle, method, args, out int exception);
			if (exception != 0)
				throw new JSException ((string)res);
			return res;
		}

		/// <summary>
		///   Returns the named property from the object, or throws a JSException on error.
		/// </summary>
		/// <param name="name">The name of the property to lookup</param>
		/// <remarks>
		///   This method can raise a <see cref="T:WebAssembly.JSException"/> if fetching the property in Javascript raises an exception.
		/// </remarks>
		/// <returns>
		///   <para>
		///     The return value can either be a primitive (string, int, double), a 
		///     <see cref="T:WebAssembly.JSObject"/> for JavaScript objects, a 
		///     <see cref="T:System.Threading.Tasks.Task"/>(object) for JavaScript promises, an array of
		///     a byte, int or double (for Javascript objects typed as ArrayBuffer) or a 
		///     <see cref="T:System.Func"/> to represent JavaScript functions.  The specific version of
		///     the Func that will be returned depends on the parameters of the Javascript function
		///     and return value.
		///   </para>
		///   <para>
		///     The value of a returned promise (The Task(object) return) can in turn be any of the above
		///     valuews.
		///   </para>
		/// </returns>
		public object GetObjectProperty (string name)
		{

			var propertyValue = Runtime.GetObjectProperty (JSHandle, name, out int exception);

			if (exception != 0)
				throw new JSException ((string)propertyValue);

			return propertyValue;

		}

		/// <summary>
		///   Sets the named property to the provided value.
		/// </summary>
		/// <remarks>
		/// </remarks>
		/// <param name="name">The name of the property to lookup</param>
		/// <param name="value">The value can be a primitive type (int, double, string, bool), an
		/// array that will be surfaced as a typed ArrayBuffer (byte[], sbyte[], short[], ushort[],
		/// float[], double[]) </param>
		/// <param name="createIfNotExists">Defaults to <see langword="true"/> and creates the property on the javascript object if not found, if set to <see langword="false"/> it will not create the property if it does not exist.  If the property exists, the value is updated with the provided value.</param>
		/// <param name="hasOwnProperty"></param>
		public void SetObjectProperty (string name, object value, bool createIfNotExists = true, bool hasOwnProperty = false)
		{

			var setPropResult = Runtime.SetObjectProperty (JSHandle, name, value, createIfNotExists, hasOwnProperty, out int exception);
			if (exception != 0)
				throw new JSException ($"Error setting {name} on (js-obj js '{JSHandle}' mono '{(IntPtr)AnyRefHandle} raw '{RawObject != null})");

		}

		/// <summary>
		/// Gets or sets the length.
		/// </summary>
		/// <value>The length.</value>
		public int Length {
			get => Convert.ToInt32 (GetObjectProperty ("length"));
			set => SetObjectProperty ("length", value, false);
		}

		/// <summary>
		/// Returns a boolean indicating whether the object has the specified property as its own property (as opposed to inheriting it).
		/// </summary>
		/// <returns><c>true</c>, if the object has the specified property as own property, <c>false</c> otherwise.</returns>
		/// <param name="prop">The String name or Symbol of the property to test.</param>
		public bool HasOwnProperty (string prop) => (bool)Invoke ("hasOwnProperty", prop);

		/// <summary>
		/// Returns a boolean indicating whether the specified property is enumerable.
		/// </summary>
		/// <returns><c>true</c>, if the specified property is enumerable, <c>false</c> otherwise.</returns>
		/// <param name="prop">The String name or Symbol of the property to test.</param>
		public bool PropertyIsEnumerable (string prop) => (bool)Invoke ("propertyIsEnumerable", prop);

		protected bool FreeHandle ()
		{
			return Runtime.ReleaseJSObject (this);
		}

		public override bool Equals (System.Object obj)
		{
			if (obj == null || GetType () != obj.GetType ()) {
				return false;
			}
			return JSHandle == (obj as JSObject).JSHandle;
		}

		public override int GetHashCode ()
		{
			return JSHandle;
		}

		// We should not provide a finalizer - SafeHandle's critical finalizer will call ReleaseHandle inside a CER for us.
		override protected bool ReleaseHandle ()
		{
			
			bool ret = false;

#if DEBUG_HANDLE
			Console.WriteLine ($"Release Handle handle:{handle}");
			try {
#endif
			    ret = FreeHandle ();

#if DEBUG_HANDLE
			} catch (Exception exception) {
				Console.WriteLine ($"ReleaseHandle: {exception.Message}");
				ret = true;  // Avoid a second assert.
				throw;
			} finally {
				if (!ret) {
					Console.WriteLine ($"ReleaseHandle failed. handle:{handle}");
				}
			}
			//Runtime.DumpExistingObjects();
#endif
			return ret;
		}

		public override string ToString ()
		{
			return $"(js-obj js '{JSHandle}' mono '{(IntPtr)AnyRefHandle}' raw '{RawObject != null}' weak_raw '{WeakRawObject != null}')";
		}

	}
}
