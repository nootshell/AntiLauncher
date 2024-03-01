using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using AntiLauncher.API.Serialization.AST;
using AntiLauncher.API.Threading;




namespace AntiLauncher.API.Serialization.ValveDataFormat {

	public class VdfAstNodeWriter : VdfAstNodeProcessorBase {

		public IndentationOptions IndentationOptions { get; set; } = IndentationOptions.Default;
		public QuotingStyle QuotingStyle { get; set; } = QuotingStyle.Default;

		/// <summary>The current line number.</summary>
		public int LineNumber { get; protected set; } = 0;

		/// <summary>The current column number.</summary>
		/// <remarks>
		///   Note that the behavior changes based on indentation style and tab stop alignment options.
		///
		///   Generally the column number is the literal written character count for the current line, <b>except</b> when the
		///   indentation style is tabs, <see cref="AlignTabStopAt" /> has been configured, <see cref="ForceTabStopAlignment" />
		///   has been set to <c>true</c>, and the configured tab width is not <c>1</c>: then every tab character is counted as
		///   the specified indentation width, leading to unexpected column numbers.
		/// </remarks>
		public int ColumnNumber { get; protected set; } = 0;

		/// <summary>The column number to align the tab stop at.</summary>
		/// <remarks>See <see cref="ForceTabStopAlignment" /> for more information regarding behavior.</remarks>
		public int? AlignTabStopAt { get; set; } = null;

		/// <summary>
		/// Whether or not to force tab stop alignment as configured by <see cref="AlignTabStopAt" /> when the current indentation
		/// style are tabs.
		/// </summary>
		/// <remarks>
		/// Alignment is normally disabled when writing tabs because tab sizes may differ between viewers, editors, and users.
		/// By forcing alignment, tabs are assumed to be sized according to <see cref="IndentationOptions.Width" />.
		/// </remarks>
		public bool ForceTabStopAlignment { get; set; } = false;




		public VdfAstNodeWriter(Stream underlyingStream, bool ownsUnderlyingStream) : base(underlyingStream, ownsUnderlyingStream) { }




		// TODO: optimize
		protected virtual async Task<int> WriteStringLiteralAsync(string value, bool countTowardsLineCharacterCount, CancellationToken token) {
			byte[] bytes = Encoding.UTF8.GetBytes(value);
			await this.underlyingStream.WriteAsync(bytes, 0, bytes.Length, token).ConfigureTask();

			if (countTowardsLineCharacterCount) {
				this.ColumnNumber += value.Length;
			}

			return bytes.Length;
		}


		protected Task<int> WriteStringLiteralAsync(string value, CancellationToken token)
			=> this.WriteStringLiteralAsync(value, countTowardsLineCharacterCount: true, token);




		protected virtual Task<int> WriteEndOfLineAsync(CancellationToken token) {
			++this.LineNumber;
			this.ColumnNumber = 0;
			return this.WriteStringLiteralAsync("\n", countTowardsLineCharacterCount: false, token);
		}


		// TODO: optimize
		protected virtual Task<int> WriteTabStopAsync(CancellationToken token) {
			if (this.IndentationOptions.Style == IndentationStyle.Spaces || this.ForceTabStopAlignment) {
				if (this.AlignTabStopAt is int alignAt && this.ColumnNumber < alignAt) {
					string pad = new string(' ', (alignAt - this.ColumnNumber));
					return this.WriteStringLiteralAsync(pad, token);
				}
			}

			return this.WriteStringLiteralAsync("\t", token);
		}


		protected virtual async Task<int> WriteIndentationAsync(IndentationOptions indentation, CancellationToken token) {
			if (indentation.Style == IndentationStyle.None || indentation.Level < 1) {
				return 0;
			}

			string indent = indentation.Style switch {
				IndentationStyle.Tabs => "\t",
				IndentationStyle.Spaces => new string(' ', indentation.Width)
			};

			int written = 0;
			for (int level = indentation.Level; level-- > 0;) {
				written += await this.WriteStringLiteralAsync(indent, countTowardsLineCharacterCount: false, token).ConfigureTask();

				if (indentation.Style == IndentationStyle.Spaces || (this.ForceTabStopAlignment && this.AlignTabStopAt != null)) {
					this.ColumnNumber += indentation.Width;
				} else {
					++this.ColumnNumber;
				}
			}

			return written;
		}




		// TODO: optimize
		protected virtual Task<int> WriteQuotedStringAsync(string value, CancellationToken token)
			=> this.WriteStringLiteralAsync($"\"{value.Replace("\"", "\\\"")}\"", token);


		protected virtual bool CheckIfStringRequiresQuoting(string value)
			=> value.Any(c => c == '"' || char.IsWhiteSpace(c));


		protected virtual async Task<int> WriteStringAsync(string value, QuotingStyle? quotingStyle, CancellationToken token) {
			quotingStyle ??= this.QuotingStyle;

			switch (quotingStyle) {
				case QuotingStyle.None:
					return await this.WriteStringLiteralAsync(value, token).ConfigureTask();

				case QuotingStyle.Always:
				case QuotingStyle.OnlyIfRequired when (this.CheckIfStringRequiresQuoting(value)):
					return await this.WriteQuotedStringAsync(value, token).ConfigureTask();

				default:
					throw new QuotingStyleNotSupportedException();
			}
		}




		protected virtual async Task<int> WriteNodeAsync(VdfAstCommentNode commentNode, IndentationOptions indentation, CancellationToken token) {
			int written = 0;

			written += await this.WriteIndentationAsync(indentation, token);
			written += await this.WriteStringLiteralAsync("// ", token).ConfigureTask();
			written += await this.WriteStringLiteralAsync(commentNode.Value + $" (ownLine: {commentNode.OwnLine})", token).ConfigureTask();
			written += await this.WriteEndOfLineAsync(token).ConfigureTask();

			return written;
		}


		public virtual Task<int> WriteNodeAsync(VdfAstCommentNode commentNode, CancellationToken token)
			=> this.WriteNodeAsync(commentNode, this.IndentationOptions, token);




		public virtual Task<int> WriteNodeAsync(VdfAstKeyNode keyNode, CancellationToken token)
			=> this.WriteStringAsync(keyNode.Value, quotingStyle: null, token);


		public virtual Task<int> WriteNodeAsync(VdfAstStringNode stringNode, CancellationToken token)
			=> this.WriteStringAsync(stringNode.Value, quotingStyle: null, token);




		protected virtual async Task<int> WriteNodeAsync(VdfAstObjectNode objectNode, IndentationOptions indentation, CancellationToken token) {
			int written = 0;

			written += await this.WriteIndentationAsync(indentation, token).ConfigureTask();
			written += await this.WriteStringLiteralAsync("{", token).ConfigureTask();
			written += await this.WriteEndOfLineAsync(token).ConfigureTask();

			if (objectNode.Children.Any()) {
				IndentationOptions childIndentation = indentation with {
					Level = (indentation.Level + 1)
				};

				foreach (VdfAstNode child in objectNode.Children) {
					switch (child) {
						case VdfAstCommentNode commentChildNode:
							written += await this.WriteNodeAsync(commentChildNode, childIndentation, token).ConfigureTask();
							break;

						case VdfAstPropertyNode propertyChildNode:
							written += await this.WriteNodeAsync(propertyChildNode, childIndentation, token).ConfigureTask();
							break;

						default:
							throw new InvalidOperationException("The specified child node type has not been mapped.");
					}
				}
			}

			written += await this.WriteIndentationAsync(indentation, token).ConfigureTask();
			written += await this.WriteStringLiteralAsync("}", token).ConfigureTask();
			written += await this.WriteEndOfLineAsync(token).ConfigureTask();

			return written;
		}


		public virtual Task<int> WriteNodeAsync(VdfAstObjectNode objectNode, CancellationToken token)
			=> this.WriteNodeAsync(objectNode, this.IndentationOptions, token);




		protected virtual async Task<int> WriteNodeAsync(VdfAstPropertyNode propertyNode, IndentationOptions indentation, CancellationToken token) {
			int written = 0;

			written += await this.WriteIndentationAsync(indentation, token).ConfigureTask();
			written += await this.WriteNodeAsync(propertyNode.Key, token).ConfigureTask();

			switch (propertyNode.Value) {
				case VdfAstStringNode stringNode:
					written += await this.WriteTabStopAsync(token).ConfigureTask();
					written += await this.WriteNodeAsync(stringNode, token).ConfigureTask();
					written += await this.WriteEndOfLineAsync(token).ConfigureTask();
					break;

				case VdfAstObjectNode objectNode:
					written += await this.WriteEndOfLineAsync(token).ConfigureTask();
					written += await this.WriteNodeAsync(objectNode, indentation, token).ConfigureTask();
					break;

				default:
					throw new NotSupportedException("The specified child node type has not been mapped.");
			}

			return written;
		}


		public virtual Task<int> WriteNodeAsync(VdfAstPropertyNode propertyNode, CancellationToken token)
			=> this.WriteNodeAsync(propertyNode, this.IndentationOptions, token);




		public Task<int> WriteNodeAsync(VdfAstNode node, CancellationToken token)
			=> node switch {
				VdfAstPropertyNode propertyNode => this.WriteNodeAsync(propertyNode, token),
				VdfAstCommentNode commentNode => this.WriteNodeAsync(commentNode, token),
				VdfAstKeyNode keyNode => this.WriteNodeAsync(keyNode, token),
				VdfAstStringNode stringNode => this.WriteNodeAsync(stringNode, token),
				VdfAstObjectNode objectNode => this.WriteNodeAsync(objectNode, token),
				_ => throw new NotSupportedException("The specified node type has not been mapped.")
			};

	}

}
