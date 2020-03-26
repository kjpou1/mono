using System;
using System.Collections;
using WebAssembly;
using WebAssembly.Core;

namespace BindingsTestSuite
{
    public class Program
    {
        public static Uint8ClampedArray Uint8ClampedArrayFrom ()
        {
            var clamped = new byte[50];
            return Uint8ClampedArray.From(clamped);
        }

        public static Uint8Array Uint8ArrayFrom ()
        {
            var array = new byte[50];
            return Uint8Array.From(array);
        }
        public static Uint16Array Uint16ArrayFrom ()
        {
            var array = new ushort[50];
            return Uint16Array.From(array);
        }
        public static Uint32Array Uint32ArrayFrom ()
        {
            var array = new uint[50];
            return Uint32Array.From(array);
        }
        public static Int8Array Int8ArrayFrom ()
        {
            var array = new sbyte[50];
            return Int8Array.From(array);
        }
        public static Int16Array Int16ArrayFrom ()
        {
            var array = new short[50];
            return Int16Array.From(array);
        }
        public static Int32Array Int32ArrayFrom ()
        {
            var array = new int[50];
            return Int32Array.From(array);
        }
        public static Float32Array Float32ArrayFrom ()
        {
            var array = new float[50];
            return Float32Array.From(array);
        }
        public static Float64Array Float64ArrayFrom ()
        {
            var array = new double[50];
            return Float64Array.From(array);
        }
        public static TypedArrayTypeCode TypedArrayType (ITypedArray arr)
        {
            return arr.GetTypedArrayType();
        }

        public static Uint8ClampedArray Uint8ClampedArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Uint8ClampedArray(sab);
        }

        public static Uint8Array Uint8ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Uint8Array(sab);
        }
        public static Uint16Array Uint16ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Uint16Array(sab);
        }
        public static Uint32Array Uint32ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Uint32Array(sab);
        }
        public static Int8Array Int8ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Int8Array(sab);
        }
        public static Int16Array Int16ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Int16Array(sab);
        }
        public static Int32Array Int32ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Int32Array(sab);
        }
        public static Float32Array Float32ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Float32Array(sab);
        }
        public static Float64Array Float64ArrayFromSharedArrayBuffer (SharedArrayBuffer sab)
        {
            return new Float64Array(sab);
        }

        public static int FunctionSumCall (int a, int b) 
        {
            var sum = new Function("a", "b", "return a + b");
            return (int)sum.Call(null, a, b);
        }

        public static double FunctionSumCallD (double a, double b) 
        {
            var sum = new Function("a", "b", "return a + b");
            return Math.Round((double)sum.Call(null, a, b), 2);
        }
        public static int FunctionSumApply (int a, int b) 
        {
            var sum = new Function("a", "b", "return a + b");
            return (int)sum.Apply(null, new object[] { a, b });
        }

        public static double FunctionSumApplyD (double a, double b) 
        {
            var sum = new Function("a", "b", "return a + b");
            return Math.Round((double)sum.Apply(null, new object[] { a, b }), 2);
        }

        public static object FunctionMathMin (WebAssembly.Core.Array array) 
        {
            object[] parms = new object[array.Length];
            for (int x = 0; x < array.Length; x++)
                parms[x] = array[x];

            var math = (JSObject)Runtime.GetGlobalObject("Math");
            var min = (Function)math.GetObjectProperty("min");
            return min.Apply(null, parms);
        }

        public static DataView DataViewConstructor () 
        {
            // create an ArrayBuffer with a size in bytes
            var buffer = new ArrayBuffer(16);

            // Create a couple of views
            var view1 = new DataView(buffer);
            var view2 = new DataView(buffer,12,4); //from byte 12 for the next 4 bytes
            view1.SetInt8(12, 42); // put 42 in slot 12            
            return view2;
        }
        public static DataView DataViewArrayBuffer (ArrayBuffer buffer) 
        {
            var view1 = new DataView(buffer);
            return view1;
        }
        public static DataView DataViewByteLength (ArrayBuffer buffer) 
        {
            var x = new DataView(buffer, 4, 2);
            return x;
        }
        public static DataView DataViewByteOffset (ArrayBuffer buffer) 
        {
            var x = new DataView(buffer, 4, 2);
            return x;
        }
        public static float DataViewGetFloat32 (DataView view) 
        {
            return view.GetFloat32(1);
        }
        public static double DataViewGetFloat64 (DataView view) 
        {
            return view.GetFloat64(1);
        }

        public static short DataViewGetInt16 (DataView view) 
        {
            return view.GetInt16(1);
        }

        public static int DataViewGetInt32 (DataView view) 
        {
            return view.GetInt32(1);
        }

        public static sbyte DataViewGetInt8 (DataView view) 
        {
            return view.GetInt8(1);
        }

        public static ushort DataViewGetUint16 (DataView view) 
        {
            return view.GetUint16(1);
        }

        public static uint DataViewGetUint32 (DataView view) 
        {
            return view.GetUint32(1);
        }

        public static byte DataViewGetUint8 (DataView view) 
        {
            return view.GetUint8(1);
        }

        public static DataView DataViewSetFloat32 () 
        {
            // create an ArrayBuffer with a size in bytes
            var buffer = new ArrayBuffer(16);

            var view = new DataView(buffer);
            view.SetFloat32(1, (float)Math.PI);
            return view;
        }

        public static DataView DataViewSetFloat64 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetFloat64(1, Math.PI);        
            return x;
        }
        
        public static DataView DataViewSetInt16 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetInt16(1, 1234);
            return x;
        }
        
        public static DataView DataViewSetInt32 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetInt32(1, 1234);
            return x;
        }
        
        public static DataView DataViewSetInt8 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetInt8(1, 123);
            return x;
        }
        
        public static DataView DataViewSetUint16 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetUint16(1, 1234);
            return x;
        }
        
        public static DataView DataViewSetUint32 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetUint32(1, 1234);
            return x;
        }
        
        public static DataView DataViewSetUint8 () 
        {
            var x = new DataView(new ArrayBuffer(12), 0);
            x.SetUint8(1, 123);
            return x;
        }

        public static Map MapTestCtor1 () 
        {
            return new Map();
        }

        public static Map MapTestCount1(int max) {
            Map h = new Map();
            
            for (int i = 1; i <= max; i++) {
                h[i] = i;
            }

            return h;
        }
        public static Map MapTestCount2(int max) {
            Map h = new Map();
            for (int i = 1; i <= max; i++) {
                h[i] = i;
            }
            for (int i = 1; i <= max; i++) {
                h[i] = i * 2;
            }
            return h;
        }
        public static bool MapTestIsFixedSize() {
            Map h = new Map();
            return h.IsFixedSize;
        }
        public static bool MapTestIsReadOnly() {
            Map h = new Map();
            return h.IsReadOnly;
        }
        public static bool MapTestIsSynchronized() {
            Map h = new Map();
            return h.IsSynchronized;
        }
        public static object MapTestItem() {
            Map h = new Map();
            Object o = h[null];
            return o;
        }
        public static JSObject MapTestKeys1() {
            string[] keys = {"this", "is", "a", "test"};
            string[] values1 = {"a", "b", "c", "d"};
            var h1 = new Map();
            for (int i = 0; i < keys.Length; i++) {
                h1[keys[i]] = values1[i];
            }
            var obj = new JSObject();
            obj.SetObjectProperty("keysLength", keys.Length);
            obj.SetObjectProperty("mapKeysLength", h1.Keys.Count);
            return obj;
        }
        public static JSObject MapTestKeys2() {
            string[] keys = {"this", "is", "a", "test"};
		    string[] keys2 = {"new", "keys"};            
            string[] values1 = {"a", "b", "c", "d"};
            string[] values2 = {"e", "f", "g", "h"};
            var h1 = new Map();
            for (int i = 0; i < keys.Length; i++) {
                h1[keys[i]] = values1[i];
            }

            ICollection keysReference;
            keysReference = h1.Keys;

            for (int i = 0; i < keys2.Length; i++) 
            {
                h1[keys2[i]] = values2[i];
            }

            var obj = new JSObject();
            obj.SetObjectProperty("keysLength", keys.Length+keys2.Length);
            obj.SetObjectProperty("h1.Keys.Count", h1.Keys.Count);
            obj.SetObjectProperty("keysReference.Count", keysReference.Count);
            return obj;
        }
        public static JSObject MapTestValues1() {
            string[] keys = {"this", "is", "a", "test"};
            string[] values1 = {"a", "b", "c", "d"};
            var h1 = new Map();
            for (int i = 0; i < keys.Length; i++) {
                h1[keys[i]] = values1[i];
            }
            var obj = new JSObject();
            obj.SetObjectProperty("keysLength", keys.Length);
            obj.SetObjectProperty("valuesLength", values1.Length);
            obj.SetObjectProperty("mapValuesCount", h1.Values.Count);
            return obj;
        }

        public static JSObject MapTestValues2() {
            string[] keys = {"this", "is", "a", "test"};
            string[] values1 = {"a", "b", "c", "d"};
            string[] values2 = {"e", "f", "g", "h"};
            var h1 = new Map();
            for (int i = 0; i < keys.Length; i++) {
                h1[keys[i]] = values1[i];
            }

            for (int i = 0; i < keys.Length; i++) {
                h1[keys[i]] = values2[i];
            }

            ICollection keysReference, valuesReference;
            keysReference = h1.Keys;
            valuesReference = h1.Values;

            var obj = new JSObject();
            obj.SetObjectProperty("keysLength", keys.Length);
            obj.SetObjectProperty("valuesLength", values2.Length);
            obj.SetObjectProperty("h1.Values.Count", h1.Values.Count);
            obj.SetObjectProperty("valuesReference.Count", valuesReference.Count);
            return obj;
        }
        public static Map MapTestAddNullKey() {
            var map = new Map();
            // Null key is valid in JS Map
            map.Add(null, "huh?");
            return map;
        }

        public static Map MapTestAddDuplicateKey() {
            var map = new Map();
            // Add duplicate key is valid in JS Map
            map.Add("a", 1);
            map.Add("a", 2);
            return map;
        }
        public static Map MapTestClear() {
            Map h = new Map();
            int max = 500;
            for (int i = 1; i <= max; i++) {
                h[i] = i;
            }
            h.Clear();
            return h;
        }

        public string MapTestGetEnumerator() {
            String[] s1 = {"this", "is", "a", "test"};
            string[] c1 = {"a", "b", "c", "d"};
            Map h1 = new Map();
            for (int i = 0; i < s1.Length; i++) {
                h1[s1[i]] = c1[i];
            }
            IDictionaryEnumerator en = h1.GetEnumerator();
            if (en == null) {
                return "Can not get Enumerator over Map";
            }
            
            for (int i = 0; i < s1.Length; i++) {
                en.MoveNext();
                if (System.Array.IndexOf(s1, en.Key) < 0) {
                    return $"Not enumerating for {en.Key}";
                }
                if (System.Array.IndexOf(c1, en.Value) < 0) {
                    return $"Not enumerating for {en.Value}";
                }
            }

            return null;
        }  
        public bool MapTestEnumerator ()
        {
            Map ht = new Map();
            ht.Add("k1","another");
            ht.Add("k2","yet");
            ht.Add("k3","hashtable");


            IEnumerator e = ht.GetEnumerator ();

            while (e.MoveNext ()) {}

            return !e.MoveNext ();

        }              
        public Map MapTestRemove ()
        {
            Map ht = new Map();
            ht.Add("k1","another");
            ht.Add("k2","yet");
            ht.Add("k3","hashtable");

            ht.Remove("k2");
            return ht;
        }              

        public string MapTestContains ()
        {
            Map ht = new Map();
            for (int i = 0; i < 10000; i += 2) 
                ht[i] = i;
            for (int i = 0; i < 10000; i += 2) {
                if (!ht.Contains(i))
                    return $"Map must contain {i}";
                if (ht.Contains(i+1))
                    return $"Map must not contain {i+1}";
            }            
            return null;
        }

        public static object ArrayPop () 
        {
            var arr = new WebAssembly.Core.Array();
            return arr.Pop();
        }

        public static int ParameterTest () 
        { 
            return -1;
        }

        public static int ParameterTest2 (string param1) 
        { 
            return -1;
        }
        public static bool StringIsNull (string param1) 
        { 
            return param1 == null;
        }
        public static bool StringIsNullOrEmpty (string param1) 
        { 
            return string.IsNullOrEmpty(param1);
        }
        public static bool StringArrayIsNull (string[] param1) 
        { 
            return param1 == null;
        }        
        public static Uri StringToUri (string uri) 
        { 
            return new Uri(uri);
        }        
    }
}
