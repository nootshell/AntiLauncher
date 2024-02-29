using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Threading;

using Microsoft.Win32;




namespace AntiLauncher.API.Launchers {

	public sealed class SteamLauncher : LauncherBase<SteamAppDetails> {

		public const string WinReg_KeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";
		public const string WinReg_KeyPrefix = "Steam App ";

		public const string WinReg_ValueKey_InstallRoot = "InstallLocation";
		public const string WinReg_ValueKey_Name = "DisplayName";
		public const string WinReg_ValueKey_Publisher = "Publisher";




		public SteamLauncher(ILogger logger) : base(logger, "Steam") { }

		public SteamLauncher() : this(Log.Logger) { }




		protected override async IAsyncEnumerable<SteamAppDetails> GetInstalledApplicationsCoreAsync(ILogger logger, CancellationToken token) {
			// TODO: rework to parse .acf files in steam libraries instead

			await Task.Yield();

			const RegistryValueOptions ValueOptions = RegistryValueOptions.DoNotExpandEnvironmentNames;
			using RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(WinReg_KeyPath);

			IEnumerable<string> applicableSubKeys = rootKey.GetSubKeyNames()
				.Where(keyName => keyName.StartsWith(WinReg_KeyPrefix));

			object? value;
			foreach (string subKey in applicableSubKeys) {
				if (token.IsCancellationRequested) {
					yield break;
				}

				using RegistryKey appKey = rootKey.OpenSubKey(subKey);

				value = appKey.GetValue(WinReg_ValueKey_InstallRoot, defaultValue: null, options: ValueOptions);
				if (value is not string installRoot) {
					throw new InvalidOperationException("Expected a string here.");
				} else if (string.IsNullOrWhiteSpace(installRoot)) {
					logger.Debug("invalid, skipping");
					continue;
				}

				value = appKey.GetValue(WinReg_ValueKey_Name, defaultValue: null, options: ValueOptions);
				if (value is not string name) {
					throw new InvalidOperationException("Expected a string here.");
				}

				value = appKey.GetValue(WinReg_ValueKey_Publisher, defaultValue: null, options: ValueOptions);
				if (value is not string publisher) {
					throw new InvalidOperationException("Expected a string here.");
				}

				yield return new SteamAppDetails(
					new DirectoryInfo(installRoot),
					name,
					publisher
				);
			}
		}

	}

}
