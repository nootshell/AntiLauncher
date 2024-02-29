using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;




namespace AntiLauncher.API.Launchers {

	public abstract class LauncherBase<TDetails> : ILauncher<TDetails> where TDetails : ILauncherAppDetails {

		protected readonly ILogger logger;

		public string Name { get; }




		protected LauncherBase(ILogger logger, string name) : base() {
			this.logger = logger.ForContext(this.GetType());

			this.Name = name;
		}




		protected abstract IAsyncEnumerable<TDetails> GetInstalledApplicationsCoreAsync(ILogger logger, CancellationToken token);

		public IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync(ILogger logger, CancellationToken token)
			=> this.GetInstalledApplicationsCoreAsync(logger, token);

		public IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync(CancellationToken token)
			=> this.GetInstalledApplicationsAsync(this.logger, token);

		public IAsyncEnumerable<TDetails> GetInstalledApplicationsAsync()
			=> this.GetInstalledApplicationsAsync(default(CancellationToken));


		IAsyncEnumerable<ILauncherAppDetails> ILauncher.GetInstalledApplicationsAsync(ILogger logger, CancellationToken token)
			=> (IAsyncEnumerable<ILauncherAppDetails>)this.GetInstalledApplicationsAsync(logger, token);

		IAsyncEnumerable<ILauncherAppDetails> ILauncher.GetInstalledApplicationsAsync(CancellationToken token)
			=> (IAsyncEnumerable<ILauncherAppDetails>)this.GetInstalledApplicationsAsync(token);

		IAsyncEnumerable<ILauncherAppDetails> ILauncher.GetInstalledApplicationsAsync()
			=> (IAsyncEnumerable<ILauncherAppDetails>)this.GetInstalledApplicationsAsync();

	}

}
