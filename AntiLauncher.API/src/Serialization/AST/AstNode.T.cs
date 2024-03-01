using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;



namespace AntiLauncher.API.Serialization.AST {

	public abstract class AstNode<TNodeType> : IAstNode<TNodeType> where TNodeType : Enum {

		public TNodeType NodeType { get; }




		protected AstNode(TNodeType nodeType) : base() {
			this.NodeType = nodeType;
		}

	}

}
