using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Threading;




namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public abstract class VdfAstNodeProcessorBase : IDisposable {

		protected readonly Stream underlyingStream;
		protected readonly bool ownsUnderlyingStream;




		protected VdfAstNodeProcessorBase(Stream underlyingStream, bool ownsUnderlyingStream) : base() {
			this.underlyingStream = underlyingStream;
			this.ownsUnderlyingStream = ownsUnderlyingStream;
		}

		~VdfAstNodeProcessorBase() {
			this.Dispose(disposing: false);
		}




		private bool disposed = false;

		protected virtual void Dispose(bool disposing) {
			if (this.disposed) {
				return;
			}
			this.disposed = true;

			if (disposing && this.ownsUnderlyingStream) {
				this.underlyingStream.Dispose();
			}
		}

		public void Dispose() {
			this.Dispose(disposing: false);
			GC.SuppressFinalize(this);
		}

	}

}
