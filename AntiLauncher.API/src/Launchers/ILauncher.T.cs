using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;




namespace AntiLauncher.API.Launchers {

	public interface ILauncher<out TDetails> : ILauncher where TDetails : ILauncherAppDetails {

		new IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync(ILogger logger, CancellationToken token);
		new IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync(CancellationToken token);
		new IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync();

	}

}
