using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Serialization.AST;



namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public sealed class VdfAstCommentNode : VdfAstNode, IAstStringValueNode {

		public string Value { get; }

		public bool OwnLine { get; } // TODO: add logic to writer




		public VdfAstCommentNode(string value, bool ownLine) : base(VdfAstNodeType.Comment) {
			this.Value = value;
			this.OwnLine = ownLine;
		}

	}

}
