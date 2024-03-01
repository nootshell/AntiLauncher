namespace AntiLauncher.API.Serialization {

	public record IndentationOptions {

		public static IndentationOptions Default { get; } = new IndentationOptions();

		public IndentationStyle Style { get; set; } = IndentationStyle.Default;
		public int Level { get; set; } = 0;
		public int Width { get; set; } = 4;

	}

}
