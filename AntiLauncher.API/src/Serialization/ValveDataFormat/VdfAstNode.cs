using AntiLauncher.API.Serialization.AST;




namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public abstract class VdfAstNode : AstNode<VdfAstNodeType> {

		protected VdfAstNode(VdfAstNodeType nodeType) : base(nodeType) { }

	}

}
