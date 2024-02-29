using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;




namespace AntiLauncher.API.Launchers {

	public interface ILauncher {

		string Name { get; }




		IAsyncEnumerable<ILauncherAppDetails> GetInstalledApplicationsAsync(ILogger logger, CancellationToken token);
		IAsyncEnumerable<ILauncherAppDetails> GetInstalledApplicationsAsync(CancellationToken token);
		IAsyncEnumerable<ILauncherAppDetails> GetInstalledApplicationsAsync();

	}

}
