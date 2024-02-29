using System.IO;




namespace AntiLauncher.API.Launchers {

	public record SteamAppDetails : ILauncherAppDetails {

		public DirectoryInfo InstallRoot { get; init; }

		public string Name { get; init; }
		public string Publisher { get; init; }




		public SteamAppDetails(DirectoryInfo installRoot, string name, string publisher) : base() {
			this.InstallRoot = installRoot;
			this.Name = name;
			this.Publisher = publisher;
		}

	}

}
