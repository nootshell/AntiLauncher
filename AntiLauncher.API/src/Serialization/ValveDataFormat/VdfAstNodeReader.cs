using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Threading;




namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public class VdfAstNodeReader : IDisposable {

		protected readonly Stream underlyingStream;
		protected readonly bool ownsUnderlyingStream;




		public VdfAstNodeReader(Stream underlyingStream, bool ownsUnderlyingStream) : base() {
			this.underlyingStream = underlyingStream;
			this.ownsUnderlyingStream = ownsUnderlyingStream;
		}

		~VdfAstNodeReader() {
			this.Dispose(disposing: false);
		}




		Decoder decoder = Encoding.UTF8.GetDecoder();

		char[] buffer = new char[512];
		int buflen = 512;
		int offset = 512;
		int offsetInState = 0;

		protected enum DecState {
			None,
			String,
			Comment
		}
		DecState state = DecState.None;

		StringBuilder builder = new StringBuilder(64);




		protected Exception GetUnexpectedEndOfStreamException()
			=> new IOException("Unexpected end of stream.");


		protected Exception GetUnexpectedCharacterException()
			=> new InvalidOperationException("Unexpected character.");




		protected virtual async Task<char?> PollNextCharAsync(CancellationToken token) {
			if (this.offset >= this.buflen) {
				byte[] bytebuf = new byte[buffer.Length]; // XXX: assumes encoding taking 1+ bytes per char
				int read = await this.underlyingStream.ReadAsync(bytebuf, 0, bytebuf.Length, token).ConfigureTask();

				this.buflen = this.decoder.GetChars(bytebuf, 0, read, this.buffer, 0, flush: false);
				if (this.buflen <= 0) {
					return null;
				}

				this.offset = 0;
			}

			return this.buffer[this.offset];
		}


		protected virtual void AdvanceOffsets() {
			++this.offset;
			++this.offsetInState;
		}


		protected virtual async Task<char?> NextCharAsync(CancellationToken token) {
			char? c = await this.PollNextCharAsync(token).ConfigureTask();

			if (c != null) {
				this.AdvanceOffsets();
			}

			return c;
		}




		protected virtual void ChangeState(DecState state) {
			this.offsetInState = 0;
			this.state = state;
		}


		protected virtual TValue ResetAndReturn<TValue>(TValue value, DecState state = DecState.None) where TValue : notnull {
			this.ChangeState(state);
			return value;
		}




		protected virtual VdfAstCommentNode ConstructCommentNode(bool ownLine) {
			/* Trim whitespace from end */
			while (this.builder.Length > 1 && char.IsWhiteSpace(this.builder[this.builder.Length - 1])) {
				--this.builder.Length;
			}
			string value = this.builder.ToString();

			/* Reset builder */
			this.builder.Length = 0;

			return new VdfAstCommentNode(value, ownLine);
		}


		protected virtual async Task<VdfAstCommentNode> ReadCommentNodeAsync(bool ownLine, CancellationToken token) {
			for ( ;;) {
				if (await this.NextCharAsync(token).ConfigureTask() is not char c) {
					if (this.state != DecState.Comment) {
						throw this.GetUnexpectedEndOfStreamException();
					}

					return this.ResetAndReturn(this.ConstructCommentNode(ownLine));
				}

				switch (this.state) {
					case DecState.None:
						if (await this.NextCharAsync(token).ConfigureTask() is not char nc) {
							throw this.GetUnexpectedEndOfStreamException();
						}

						if (c == '/' && nc == '/') {
							this.ChangeState(DecState.Comment);
						} else {
							throw this.GetUnexpectedCharacterException();
						}
						break;

					case DecState.Comment:
						if (c == '\r' || c == '\n') {
							/* Comments are terminated by EOL */
							return this.ResetAndReturn(this.ConstructCommentNode(ownLine));
						}

						if (this.builder.Length == 0 && char.IsWhiteSpace(c)) {
							/* Skip leading whitespace */
							continue;
						}

						this.builder.Append(c);
						break;

					default:
						throw new InvalidOperationException("Invalid decoder state.");
				}
			}
		}




		protected virtual string ConstructStringBasedNodeValue() {
			string value = this.builder.ToString();
			this.builder.Length = 0;
			return value;
		}


		protected virtual async Task<string?> ReadStringBasedNodeValueAsync(bool nullIfEndOfStream, CancellationToken token) {
			string ResetAndReturn() {
				string value = this.ConstructStringBasedNodeValue();
				this.ChangeState(DecState.None);
				return value;
			}

			int firstReadOffset = -1;
			bool quoted = false;
			bool escape = false;
			for ( ;;) {
				if (await this.NextCharAsync(token).ConfigureTask() is not char c) {
					if (quoted) {
						/* Quoted strings always expect an end quote */
						throw this.GetUnexpectedEndOfStreamException();
					}

					if (nullIfEndOfStream) {
						return null;
					}

					return this.ResetAndReturn(this.ConstructStringBasedNodeValue());
				}

				switch (this.state) {
					case DecState.None:
						if (char.IsWhiteSpace(c)) {
							throw this.GetUnexpectedCharacterException();
						} else if (c == '"') {
							quoted = true;
						} else {
							/* First character is already part of the value */
							this.builder.Append(c);
						}
						this.ChangeState(DecState.String);
						break;

					case DecState.String:
						/* If we're quoted and find an unescaped end quote, or we're not quoted and we find unescaped whitespace: return. */
						if ((quoted && !escape && c == '"') || (!quoted && !escape && char.IsWhiteSpace(c))) {
							return this.ResetAndReturn(this.ConstructStringBasedNodeValue());
						}

						this.builder.Append(c);

						if (c == '\\') {
							escape = !escape;
						} else {
							escape = false;
						}
						break;

					default:
						throw new InvalidOperationException("Invalid decoder state.");
				}
			}
		}


		protected virtual async Task<VdfAstKeyNode> ReadKeyNodeAsync(CancellationToken token) {
			string? value = await this.ReadStringBasedNodeValueAsync(nullIfEndOfStream: true, token).ConfigureTask();
			if (value == null) {
				throw this.GetUnexpectedEndOfStreamException();
			}

			return new VdfAstKeyNode(value);
		}


		protected virtual async Task<VdfAstStringNode> ReadStringNodeAsync(CancellationToken token) {
			string? value = await this.ReadStringBasedNodeValueAsync(nullIfEndOfStream: false, token).ConfigureTask();
			if (value == null) {
				throw this.GetUnexpectedEndOfStreamException();
			}

			return new VdfAstStringNode(value);
		}




		protected virtual async Task<VdfAstObjectNode> ReadObjectNodeAsync(CancellationToken token) {
			VdfAstObjectNode node = new VdfAstObjectNode();

			bool foundNewLine = false;
			VdfAstNode child;
			for ( ;;) {
				if (await this.PollNextCharAsync(token).ConfigureTask() is not char c) {
					throw this.GetUnexpectedEndOfStreamException();
				}

				if (c == '{' || char.IsWhiteSpace(c)) {
					if (c == '\n') {
						foundNewLine = true;
					}
					this.AdvanceOffsets();
					continue;
				}

				if (c == '}') {
					this.AdvanceOffsets();
					return node;
				}

				if (c == '/') {
					child = await this.ReadCommentNodeAsync(foundNewLine, token).ConfigureTask();
				} else {
					child = await this.ReadPropertyNodeAsync(token).ConfigureTask();
				}
				node.Children.Add(child);
				foundNewLine = false;
			}
		}




		protected virtual async Task<VdfAstPropertyNode> ReadPropertyNodeAsync(CancellationToken token) {
			VdfAstKeyNode? key = null;
			VdfAstNode? value = null;
			do {
				if (await this.PollNextCharAsync(token).ConfigureTask() is not char c) {
					throw this.GetUnexpectedEndOfStreamException();
				}

				/* Skip any whitespace we find between the key and the value */
				if (char.IsWhiteSpace(c)) {
					this.AdvanceOffsets();
					continue;
				}

				switch (c) {
					case '{' when (key != null):
						value = await this.ReadObjectNodeAsync(token).ConfigureTask();
						break;

					case '"':
					case char when (char.IsLetter(c)):
						if (key == null) {
							key = await this.ReadKeyNodeAsync(token).ConfigureTask();
						} else {
							value = await this.ReadStringNodeAsync(token).ConfigureTask();
						}
						break;
				}
			} while (key == null || value == null);

			return new VdfAstPropertyNode(key, value);
		}




		public async Task<VdfAstNode?> ReadNodeAsync(CancellationToken token) {
			bool foundNewLine = false;
			for ( ;;) {
				if (this.state != DecState.None) {
					throw new InvalidOperationException("Reader is in an unexpected state.");
				}

				if (await this.PollNextCharAsync(token).ConfigureTask() is not char c) {
					return null;
				}

				if (char.IsWhiteSpace(c)) {
					if (c == '\n') {
						foundNewLine = true;
					}
					this.AdvanceOffsets();
					continue;
				}

				switch (c) {
					case '/':
						return await this.ReadCommentNodeAsync(foundNewLine, token).ConfigureTask();

					case '"':
					case char when (char.IsLetter(c)):
						return await this.ReadPropertyNodeAsync(token).ConfigureTask();
				}

				foundNewLine = false;
			}
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
