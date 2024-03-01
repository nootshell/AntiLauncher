namespace AntiLauncher.API.Serialization.AST {

	public interface IAstMappingNode<TKeyNode, TValueNode> : IAstNode where TKeyNode : IAstNode where TValueNode : IAstNode {

		TKeyNode Key { get; }
		TValueNode Value { get; }

	}

}
