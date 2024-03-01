using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Launchers;
using AntiLauncher.API.Serialization.AST;
using AntiLauncher.API.Serialization.ValveDataFormat;

using Serilog;




namespace AntiLauncher {

	static class Program {

		static async Task<int> Main(string[] args) {
			Log.Logger = (new LoggerConfiguration())
				.MinimumLevel.Debug()
				.WriteTo.Async(x => x.Console(), blockWhenFull: true)
				.CreateLogger();

			try {
				/*ILauncher launcher = new SteamLauncher();
				await foreach (ILauncherAppDetails appDetails in launcher.GetInstalledApplicationsAsync()) {
					Log.Information("App {Name} by {Publisher} found in {InstallRoot}.", appDetails.Name, appDetails.Publisher, appDetails.InstallRoot);
				}*/

				byte[] bytes = Encoding.UTF8.GetBytes("""
// this is a comment
// this is another comment
  //// this should also be a comment
// this // is also a // comment
"root" "with value"
"root2"
  "with value"
"objectRoot"
{ // yes another comment
	"subProperty" "withValueSub" // also a comment
	"bigDick" "nootHasIt"
	"subObjectRoot"
	{
		"pawgBitches" "nootAlsoHasEm" // random comment
	}
}
""");
				MemoryStream ms = new MemoryStream();
				MemoryStream nms = new MemoryStream();
				await ms.WriteAsync(bytes, 0, bytes.Length);
				ms.Seek(0, SeekOrigin.Begin);

				using VdfAstNodeReader reader = new VdfAstNodeReader(ms, ownsUnderlyingStream: true);
				using VdfAstNodeWriter writer = new VdfAstNodeWriter(nms, ownsUnderlyingStream: true)/* { AlignTabStopAt = 40 }*/;
				VdfAstNode? node;
				int written = 0;
				while ((node = await reader.ReadNodeAsync(default(CancellationToken))) != null) {
					switch (node) {
						case IAstStringValueNode stringNode:
							Log.Information("Got {Type} node with content {Content}.", stringNode.GetType(), stringNode.Value);
							break;

						case VdfAstPropertyNode propertyNode:
							Log.Information("Got property with key {Key} and value {Value}.", propertyNode.Key, propertyNode.Value);
							break;

						default:
							Log.Warning("Got unknown node of type {type}: {@obj}", node?.GetType(), node);
							break;
					}

					written += await writer.WriteNodeAsync(node, default(CancellationToken));
				}

				nms.Seek(0, SeekOrigin.Begin);
				Log.Information("Rewritten form ({written}/{length}): {rewritten}", written, nms.Length, Encoding.UTF8.GetString(nms.GetBuffer()));
			} finally {
				Log.CloseAndFlush();
			}

			return 0;
		}

	}

}
