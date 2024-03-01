using System;




namespace AntiLauncher.API.Serialization {

	public class QuotingStyleNotSupportedException : NotSupportedException {

		public QuotingStyleNotSupportedException(string? message) : base(message) { }

		public QuotingStyleNotSupportedException() : base() { }

	}

}
