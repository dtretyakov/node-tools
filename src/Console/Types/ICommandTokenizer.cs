using System.Collections.Generic;

namespace Console.Types
{
    /// <summary>
    /// Interface for command line tokenizer (syntax highlighting).
    /// </summary>
    public interface ICommandTokenizer
    {
        /// <summary>
        /// Tokenize a sequence of command lines.
        /// </summary>
        /// <param name="lines">The command lines.</param>
        /// <returns>A sequence of Tokens.</returns>
        IEnumerable<Token> Tokenize(string[] lines);
    }
}
