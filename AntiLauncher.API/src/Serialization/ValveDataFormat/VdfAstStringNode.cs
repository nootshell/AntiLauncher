using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Serialization.AST;



namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public sealed class VdfAstStringNode : VdfAstNode, IAstStringValueNode {

		public string Value { get; }




		public VdfAstStringNode(string value) : base(VdfAstNodeType.String) {
			this.Value = value;
		}




		public override string ToString()
			=> $"{nameof(VdfAstStringNode)} [\"{this.Value}\"]";

	}

}
