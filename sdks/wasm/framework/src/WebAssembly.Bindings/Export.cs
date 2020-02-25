using System;
namespace WebAssembly {
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		enum ConvertEnum {
		Default,
		ToLower,
		ToUpper,
		Numeric
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field,
			AllowMultiple = true, Inherited = false)]
	#if SYSTEM_NET_HTTP
	internal
#else
	public
#endif
		class ExportAttribute : Attribute {

		public ExportAttribute () : this (null, null)
		{
		}

		public ExportAttribute (Type contractType) : this (null, contractType)
		{
		}

		public ExportAttribute (string contractName) : this (contractName, null)
		{
		}

		public ExportAttribute (string contractName, Type contractType)
		{
			ContractName = contractName;
			ContractType = contractType;
		}

		public string ContractName { get; }

		public Type ContractType { get; }
		public ConvertEnum EnumValue { get; set; }
	}
}
