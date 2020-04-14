using System;
using System.Collections.Generic;
using System.Reflection;

namespace WebAssembly {

	// very simple container implementation
	internal sealed class BindingsContainer {

		readonly Dictionary<Type, Type> containerMap = new Dictionary<Type, Type> ();
		public BindingsContainer ()
		{
		}

		public void Register<TFrom, TTo> ()
		{
			Register (typeof (TFrom), typeof (TTo));
		}

		public void Register (Type TFrom, Type TTo)
		{
			containerMap.Add (TFrom, TTo);
		}

		public T Resolve<T> (Type type)
		{
			return (T)Resolve (typeof (T));
		}

		public object Resolve (Type type)
		{
			Type resolvedType;
			if (!containerMap.TryGetValue (type, out resolvedType)) {
				throw new ArgumentException ($"Could not resolve type {type}");
			}

			var ctors = resolvedType.GetConstructors ();
			if (ctors == null || ctors.Length == 0)
			{
				return Activator.CreateInstance (resolvedType);	
			}

			var ctor = ctors [0];
			
			var ctorParameters = ctor.GetParameters ();
			if (ctorParameters.Length == 0) {
				return Activator.CreateInstance (resolvedType);
			}

			var parameters = new List<object> ();
			foreach (var parameterToResolve in ctorParameters) {
				parameters.Add (Resolve (parameterToResolve.ParameterType));
			}
			return ctor.Invoke(parameters.ToArray());
		}
	}
}
