using System;




namespace AntiLauncher.API.Serialization.AST {

	public interface IAstNode<TNodeType> : IAstNode where TNodeType : Enum {

		TNodeType NodeType { get; }

	}

}
