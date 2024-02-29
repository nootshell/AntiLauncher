using System.IO;




namespace AntiLauncher.API.Launchers {

	public interface ILauncherAppDetails {

		DirectoryInfo InstallRoot { get; }

		string Name { get; }
		string Publisher { get; }

	}

}
