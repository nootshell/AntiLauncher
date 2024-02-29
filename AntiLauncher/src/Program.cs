using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Launchers;

using Serilog;




namespace AntiLauncher {

	static class Program {

		static async Task<int> Main(string[] args) {
			Log.Logger = (new LoggerConfiguration())
				.MinimumLevel.Debug()
				.WriteTo.Async(x => x.Console(), blockWhenFull: true)
				.CreateLogger();

			try {
				ILauncher launcher = new SteamLauncher();
				await foreach (ILauncherAppDetails appDetails in launcher.GetInstalledApplicationsAsync()) {
					Log.Information("App {Name} by {Publisher} found in {InstallRoot}.", appDetails.Name, appDetails.Publisher, appDetails.InstallRoot);
				}
			} finally {
				Log.CloseAndFlush();
			}

			return 0;
		}

	}

}
