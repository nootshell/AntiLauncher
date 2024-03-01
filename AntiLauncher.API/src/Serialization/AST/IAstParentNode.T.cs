using System.Collections.Generic;




namespace AntiLauncher.API.Serialization.AST {

	public interface IAstParentNode<TChildNode> : IAstNode where TChildNode : IAstNode {

		ICollection<TChildNode> Children { get; }

	}

}
