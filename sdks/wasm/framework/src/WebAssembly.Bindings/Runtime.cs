﻿#nullable enable

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WebAssembly.Services;
using WebAssembly.Services.WebSockets;
using WebAssembly.Services.IServices;

namespace WebAssembly {
	/// <summary>
	///   Provides access to the Mono/WebAssembly runtime to perform tasks like invoking JavaScript functions and retrieving global variables.
	/// </summary>
	public sealed class Runtime {
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern string InvokeJS (string str, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		static extern object CompileFunction (string str, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object InvokeJSWithArgs (int js_obj_handle, string method, object? [] _params, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object GetObjectProperty (int js_obj_handle, string propertyName, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object SetObjectProperty (int js_obj_handle, string propertyName, object value, bool createIfNotExists, bool hasOwnProperty, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object GetByIndex (int js_obj_handle, int index, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object SetByIndex (int js_obj_handle, int index, object? value, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object GetGlobalObject (string? globalName, out int exceptional_result);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object ReleaseHandle (int js_obj_handle, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object ReleaseObject (int js_obj_handle, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object NewObjectJS (int js_obj_handle, object [] _params, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object BindCoreObject (int js_obj_handle, int gc_handle, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object BindHostObject (int js_obj_handle, int gc_handle, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object New (string className, object [] _params, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object TypedArrayToArray (int js_obj_handle, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object TypedArrayCopyTo (int js_obj_handle, int array_ptr, int begin, int end, int bytes_per_element, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object TypedArrayFrom (int array_ptr, int begin, int end, int bytes_per_element, int type, out int exceptional_result);
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal static extern object TypedArrayCopyFrom (int js_obj_handle, int array_ptr, int begin, int end, int bytes_per_element, out int exceptional_result);

		/// <summary>
		/// Execute the provided string in the JavaScript context
		/// </summary>
		/// <returns>The js.</returns>
		/// <param name="str">String.</param>
		public static string InvokeJS (string str)
		{
			var res = InvokeJS (str, out int exception);
			if (exception != 0)
				throw new JSException (res);
			return res;
		}

		/// <summary>
		/// Compiles a JavaScript function from the function data passed.
		/// The code snippet is not a function definition. Instead it must create and return a function instance.
		/// </summary>
		/// <returns>A <see cref="T:WebAssembly.Core.Function"/> class</returns>
		/// <param name="str">String.</param>
		public static WebAssembly.Core.Function? CompileFunction (string snippet)
		{
			var res = CompileFunction (snippet, out int exception);
			if (exception != 0)
				throw new JSException ((string)res);
			return res as WebAssembly.Core.Function;
		}

		static Dictionary<int, JSObject?> bound_objects = new Dictionary<int, JSObject?> ();
		static Dictionary<object, JSObject?> raw_to_js = new Dictionary<object, JSObject?> ();
		static BindingsContainer bindingsContainer = new BindingsContainer ();

		static Runtime ()
		{
			ConfigureServices ();
		}

		static void ConfigureServices ()
		{
			bindingsContainer.RegisterType<IHttpHandlerService, WebAssemblyHttpHandlerService> ();
			bindingsContainer.RegisterType<IWebSocketHandleService, WebSocketHandleService> ();
		}

		/// <summary>
		/// Creates a new JavaScript object of the specified type
		/// </summary>
		/// <returns>The new.</returns>
		/// <param name="_params">Parameters.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static int New<T> (params object [] _params)
		{
			var res = New (typeof(T).Name, _params, out int exception);
			if (exception != 0)
				throw new JSException ((string)res);
			return (int)res;
		}

		public static int New (string hostClassName, params object [] _params)
		{
			var res = New (hostClassName, _params, out int exception);
			if (exception != 0)
				throw new JSException ((string)res);
			return (int)res;
		}

		/// <summary>
		/// Creates a new JavaScript object 
		/// </summary>
		/// <returns>The JSO bject.</returns>
		/// <param name="js_func_ptr">Js func ptr.</param>
		/// <param name="_params">Parameters.</param>
		public static JSObject? NewJSObject (JSObject? js_func_ptr = null, params object [] _params)
		{
			var res = NewObjectJS (js_func_ptr?.JSHandle ?? 0, _params, out int exception);
			if (exception != 0)
				throw new JSException ((string)res);
			return res as JSObject;
		}

		internal static int BindJSObject (int js_id, Type mappedType)
		{
			lock (bound_objects) {
				if (!bound_objects.TryGetValue (js_id, out JSObject? obj)) {
					if (mappedType != null) {
						return BindJSType (js_id, mappedType);
					} else {
						bound_objects [js_id] = obj = new JSObject ((IntPtr)js_id);
					}
				}

				return obj == null ? 0 : (int)(IntPtr)obj.Handle;
			}
		}

		internal static int BindCoreCLRObject (int js_id, int gcHandle)
		{
			//Console.WriteLine ($"Registering CLR Object {js_id} with handle {gcHandle}");
			GCHandle h = (GCHandle)(IntPtr)gcHandle;
			JSObject? obj = h.Target as JSObject;
			lock (bound_objects) {
				if (bound_objects.TryGetValue (js_id, out var existingObj)) {
					if (existingObj?.Handle != h && h.IsAllocated)
						throw new JSException ($"Multiple handles pointing at js_id: {js_id}");

					obj = existingObj;
				} else
					bound_objects [js_id] = obj;

				return obj == null ? 0 : (int)(IntPtr)obj.Handle;
			}
		}

		internal static int BindJSType (int js_id, Type mappedType)
		{
			lock (bound_objects) {
				if (!bound_objects.TryGetValue (js_id, out JSObject? obj)) {
					var jsobjectnew = mappedType.GetConstructor (BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding,
						    null, new Type [] { typeof (IntPtr) }, null);
					bound_objects [js_id] = obj = (JSObject)jsobjectnew.Invoke (new object [] { (IntPtr)js_id });
				}
				return obj == null ? 0 : (int)(IntPtr)obj.Handle;
			}
		}

		internal static int UnBindJSObject (int js_id)
		{
			lock (bound_objects) {
				if (bound_objects.TryGetValue (js_id, out var obj)) {
					bound_objects.Remove (js_id);
					return obj == null ? 0 : (int)(IntPtr)obj.Handle;
				}

				return 0;
			}
		}

		internal static void UnBindJSObjectAndFree (int js_id)
		{
			lock (bound_objects) {
				if (bound_objects.TryGetValue (js_id, out var obj)) {
					if (bound_objects [js_id] != null) {
						//bound_objects [js_id].RawObject = null;
						bound_objects.Remove (js_id);
					}
					if (obj != null) {
						obj.JSHandle = -1;
						obj.IsDisposed = true;
						obj.RawObject = null;
						obj.Handle.Free ();
					}
				}
			}

		}


		internal static void UnBindRawJSObjectAndFree (int gcHandle)
		{

			GCHandle h = (GCHandle)(IntPtr)gcHandle;
			JSObject obj = (JSObject)h.Target;
			lock (raw_to_js) {
				if (obj != null && obj.RawObject != null) {
					raw_to_js.Remove (obj.RawObject);

					int exception;
					ReleaseHandle (obj.JSHandle, out exception);
					if (exception != 0)
						throw new JSException ($"Error releasing handle on (js-obj js '{obj.JSHandle}' mono '{(IntPtr)obj.Handle} raw '{obj.RawObject != null})");

					// Calling Release Handle above only removes the reference from the JavaScript side but does not 
					// release the bridged JSObject associated with the raw object so we have to do that ourselves.
					obj.JSHandle = -1;
					obj.IsDisposed = true;
					obj.RawObject = null;

					obj.Handle.Free ();
				}
			}

		}

		public static void FreeObject (object obj)
		{
			lock (raw_to_js) {
				if (raw_to_js.TryGetValue (obj, out JSObject? jsobj)) {
					//raw_to_js [obj].RawObject = null;
					raw_to_js.Remove (obj);
					if (jsobj != null) {
						int exception;
						Runtime.ReleaseObject (jsobj.JSHandle, out exception);
						if (exception != 0)
							throw new JSException ($"Error releasing object on (raw-obj)");

						jsobj.JSHandle = -1;
						jsobj.RawObject = null;
						jsobj.IsDisposed = true;
						jsobj.Handle.Free ();
					}

				} else {
					throw new JSException ($"Error releasing object on (obj)");
				}
			}
		}

		internal static object CreateTaskSource (int js_id)
		{
			return new TaskCompletionSource<object> ();
		}

		internal static void SetTaskSourceResult (TaskCompletionSource<object> tcs, object result)
		{
			tcs.SetResult (result);
		}

		internal static void SetTaskSourceFailure (TaskCompletionSource<object> tcs, string reason)
		{
			tcs.SetException (new JSException (reason));
		}

		internal static int GetTaskAndBind (TaskCompletionSource<object> tcs, int js_id)
		{
			return BindExistingObject (tcs.Task, js_id);
		}

		internal static int BindExistingObject (object raw_obj, int js_id)
		{
			JSObject? obj = raw_obj as JSObject;

			lock (raw_to_js) {
				if (obj == null && !raw_to_js.TryGetValue (raw_obj, out obj))
					raw_to_js [raw_obj] = obj = new JSObject (js_id, raw_obj);

				return obj == null ? 0 : (int)(IntPtr)obj.Handle;
			}
		}

		internal static int GetJSObjectId (object raw_obj)
		{
			JSObject? obj = raw_obj as JSObject;

			lock (raw_to_js) {
				if (obj == null && !raw_to_js.TryGetValue (raw_obj, out obj))
					return -1;

				return obj != null ? obj.JSHandle : -1;
			}
		}

		internal static object? GetMonoObject (int gc_handle)
		{
			GCHandle h = (GCHandle)(IntPtr)gc_handle;
			JSObject? o = (JSObject)h.Target;
			if (o != null && o.RawObject != null)
				return o.RawObject;
			return o;
		}

		internal static object BoxInt (int i)
		{
			return i;
		}
		internal static object BoxDouble (double d)
		{
			return d;
		}

		internal static object BoxBool (int b)
		{
			return b == 0 ? false : true;
		}

		internal static bool IsSimpleArray (object a)
		{
			return a is Array arr && arr.Rank == 1 && arr.GetLowerBound(0) == 0;
		}

		static object? GetCoreType (string coreObj)
		{
			Assembly asm = typeof (Runtime).Assembly;
			Type? type = asm.GetType (coreObj);
			return type;

		}

		[StructLayout (LayoutKind.Explicit)]
		internal struct IntPtrAndHandle {
			[FieldOffset (0)]
			internal IntPtr ptr;

			[FieldOffset (0)]
			internal RuntimeMethodHandle handle;
		}

		//FIXME this probably won't handle generics
		static string GetCallSignature (IntPtr method_handle)
		{
			IntPtrAndHandle tmp = default (IntPtrAndHandle);
			tmp.ptr = method_handle;

			var mb = MethodBase.GetMethodFromHandle (tmp.handle);
			if (mb == null)
				return string.Empty;

			ParameterInfo [] parms = mb.GetParameters ();
			var parmsLength = parms.Length;
			if (parmsLength == 0)
				return string.Empty;

			var res = new char [parmsLength];

			for (int c = 0; c < parmsLength; c++) {
				var t = parms [c].ParameterType;
				switch (Type.GetTypeCode (t)) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Boolean:
					// Enums types have the same code as their underlying numeric types
					if (t.IsEnum)
						res [c] = 'j';
					else
						res [c] = 'i';
					break;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					// Enums types have the same code as their underlying numeric types
					if (t.IsEnum)
						res [c] = 'k';
					else
						res [c] = 'l';
					break;
				case TypeCode.Single:
					res [c] = 'f';
					break;
				case TypeCode.Double:
					res [c] = 'd';
					break;
				case TypeCode.String:
					res [c] = 's';
					break;
				default:
					if (t == typeof (IntPtr)) {
						res [c] = 'i';
					} else if (t == typeof (Uri)) {
						res [c] = 'u';
					} else {
						if (t.IsValueType)
							throw new NotSupportedException ("ValueType arguments are not supported.");
						res [c] = 'o';
					}
					break;
				}
			}
			return new string (res);

		}

		static object? ObjectToEnum (IntPtr method_handle, int parm, object obj)
		{
			IntPtrAndHandle tmp = default (IntPtrAndHandle);
			tmp.ptr = method_handle;

			var mb = MethodBase.GetMethodFromHandle (tmp.handle);
			var parmType = mb.GetParameters () [parm].ParameterType;
			if (parmType.IsEnum)
				return Runtime.EnumFromExportContract (parmType, obj);
			else
				return null;

		}

		//static MethodInfo? s_taskResult;
		//static PropertyInfo? s_propInfo;
		static void SetupJSContinuation (Task task, JSObject cont_obj)
		{
			if (task.IsCompleted)
				Complete ();
			else
				task.GetAwaiter ().OnCompleted (Complete);

			void Complete ()
			{
				try {
					if (task.Exception == null) {
						object? result;
						Type task_type = task.GetType ();
						if (task_type == typeof (Task)) {
							result = Array.Empty<object> ();
						}
						else {
							//MethodInfo? taskResult = task_type.GetMethod ("get_Result");
							result = task_type.GetMethod("get_Result")?.Invoke(task, Array.Empty<object>());
						}
						cont_obj.Invoke ("resolve", result);
					} else {
						cont_obj.Invoke ("reject", task.Exception.ToString ());
					}
				} catch (Exception e) {
					cont_obj.Invoke ("reject", e.ToString ());
				} finally {
					cont_obj.Dispose ();
					FreeObject (task);
				}
			}
		}

		/// <summary>
		///   Fetches a global object from the Javascript world, either from the current brower window or from the node.js global context.
		/// </summary>
		/// <remarks>
		///   This method returns the value of a global object marshalled for consumption in C#.
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
		/// <param name="str">The name of the global object, or null if you want to retrieve the 'global' object itself.
		/// On a browser, this is the 'window' object, on node.js it is the 'global' object.
		/// </param>
		public static object GetGlobalObject (string? str = null)
		{
			int exception;
			var globalHandle = Runtime.GetGlobalObject (str, out exception);

			if (exception != 0)
				throw new JSException ($"Error obtaining a handle to global {str}");

			return globalHandle;
		}

		static string ObjectToString (object o)
		{

			if (o is Enum)
				return EnumToExportContract ((Enum)o).ToString ();

			return o.ToString ();
		}

		static double GetDateValue (object dtv)
		{
			if (dtv == null)
				throw new ArgumentNullException (nameof (dtv), "Value can not be null");
			if (!(dtv is DateTime dt)) {
				throw new InvalidCastException ($"Unable to cast object of type {dtv.GetType()} to type DateTime.");
			}
			if (dt.Kind == DateTimeKind.Local)
				dt = dt.ToUniversalTime ();
			else if (dt.Kind == DateTimeKind.Unspecified)
				dt = new DateTime (dt.Ticks, DateTimeKind.Utc);
			return new DateTimeOffset(dt).ToUnixTimeMilliseconds();
		}

		static DateTime CreateDateTime (double ticks)
		{
			var unixTime = DateTimeOffset.FromUnixTimeMilliseconds((Int64)ticks);
			return unixTime.DateTime;
		}
		static Uri CreateUri (string uri)
		{
			return new Uri(uri);
		}

		// This is simple right now and will include FlagsAttribute later.
		public static Enum EnumFromExportContract (Type enumType, object value)
		{

			if (!enumType.IsEnum) {
				throw new ArgumentException ("Type provided must be an Enum.", nameof (enumType));
			}

			if (value is string) {

				var fields = enumType.GetFields ();
				foreach (var fi in fields) {
					// Do not process special names
					if (fi.IsSpecialName)
						continue;

					ExportAttribute [] attributes =
					    (ExportAttribute [])fi.GetCustomAttributes (typeof (ExportAttribute), false);

					var enumConversionType = ConvertEnum.Default;

					object? contractName = null;

					if (attributes != null && attributes.Length > 0) {
						enumConversionType = attributes [0].EnumValue;
						if (enumConversionType != ConvertEnum.Numeric)
							contractName = attributes [0].ContractName;

					}

					if (contractName == null)
						contractName = fi.Name;

					switch (enumConversionType) {
					case ConvertEnum.ToLower:
						contractName = contractName.ToString ().ToLower ();
						break;
					case ConvertEnum.ToUpper:
						contractName = contractName.ToString ().ToUpper ();
						break;
					case ConvertEnum.Numeric:
						contractName = (int)Enum.Parse (value.GetType (), contractName.ToString ());
						break;
					default:
						contractName = contractName.ToString ();
						break;
					}

					if (contractName.ToString () == value.ToString ()) {
						return (Enum)Enum.Parse (enumType, fi.Name);
					}

				}

				throw new ArgumentException ($"Value is a name, but not one of the named constants defined for the enum of type: {enumType}.", nameof (value));
			} else {
				return (Enum)Enum.ToObject (enumType, value);
			}

		}

		// This is simple right now and will include FlagsAttribute later.
		public static object EnumToExportContract (Enum value)
		{

			FieldInfo fi = value.GetType ().GetField (value.ToString ());

			ExportAttribute [] attributes =
			    (ExportAttribute [])fi.GetCustomAttributes (typeof (ExportAttribute), false);

			var enumConversionType = ConvertEnum.Default;

			object? contractName = null;

			if (attributes != null && attributes.Length > 0) {
				enumConversionType = attributes [0].EnumValue;
				if (enumConversionType != ConvertEnum.Numeric)
					contractName = attributes [0].ContractName;

			}

			if (contractName == null)
				contractName = value;

			switch (enumConversionType) {
			case ConvertEnum.ToLower:
				contractName = contractName.ToString ().ToLower ();
				break;
			case ConvertEnum.ToUpper:
				contractName = contractName.ToString ().ToUpper ();
				break;
			case ConvertEnum.Numeric:
				contractName = (int)Enum.Parse (value.GetType (), contractName.ToString ());
				break;
			default:
				contractName = contractName.ToString ();
				break;
			}

			return contractName;
		}

		//
		// Can be called by the app to stop profiling
		//
		[MethodImplAttribute (MethodImplOptions.NoInlining)]
		public static void StopProfile () {
		}

		// Called by the AOT profiler to save profile data into Module.aot_profile_data
		internal unsafe static void DumpAotProfileData (ref byte buf, int len, string s) {
			var arr = new byte [len];
			fixed (void *p = &buf) {
				var span = new ReadOnlySpan<byte> (p, len);

				// Send it to JS
				var js_dump = (JSObject)Runtime.GetGlobalObject ("Module");
				js_dump.SetObjectProperty ("aot_profile_data", WebAssembly.Core.Uint8Array.From (span));
			}
		}

		// Called by the coverage profiler to save profile data into Module.coverage_profile_data
		internal static void DumpCoverageProfileData (string data, string s) {
			// Send it to JS
			var js_dump = (JSObject)Runtime.GetGlobalObject ("Module");
			js_dump.SetObjectProperty ("coverage_profile_data", data);
		}

		/// <summary>
		/// Register a service type
		/// </summary>
		/// <typeparam name="TFrom"></typeparam>
		/// <typeparam name="TTo"></typeparam>
		public static void Register<TFrom, TTo> ()
		{
			Register (typeof(TFrom), typeof(TTo));
		}

		/// <summary>
		/// Register a service type
		/// </summary>
		/// <param name="TFrom"></param>
		/// <param name="TTo"></param>
		public static void Register (Type TFrom, Type TTo)
		{
			bindingsContainer.RegisterType (TFrom, TTo);
		}

		/// <summary>
		/// Locate a service that is requested
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static TTo LocateService <TFrom, TTo> () => (TTo)LocateService (typeof (TFrom));

		/// <summary>
		/// Locate a service that is requested
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static TFrom LocateService<TFrom> () => (TFrom)LocateService (typeof (TFrom));

		/// <summary>
		/// Locate a service that is requested
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static object LocateService (Type type)
		{
			if (type == null)
				throw new ArgumentNullException (nameof (type), "Value can not be null");

			
			var resolvedType = bindingsContainer.Resolve (type);
			//Console.WriteLine ($"locating service for {type} / {resolvedType}");
			return resolvedType;
		}
	}
}
