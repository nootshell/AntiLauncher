using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Serialization.AST;



namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public sealed class VdfAstObjectNode : VdfAstNode, IAstParentNode<VdfAstNode> {

		public ICollection<VdfAstNode> Children { get; }




		public VdfAstObjectNode(ICollection<VdfAstNode> children) : base(VdfAstNodeType.Object) {
			this.Children = children;
		}

		public VdfAstObjectNode() : this(new List<VdfAstNode>()) { }

	}

}
