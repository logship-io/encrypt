using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace encrypt.Utilities
{
    internal static class Utf8JsonObjectReader
    {
        /// <summary>
        /// Reads a JSON object from a stream and returns it as a string.
        /// </summary>
        /// <param name="stream">The input stream.</param>
        /// <param name="result">The json object result.</param>
        /// <returns>The result.</returns>
        public static bool ReadJsonObjectAsString(Stream stream, [NotNullWhen(true)] out string? result)
        {
            var output = new StringBuilder();

            var first = stream.ReadByte();
            if (first != '{')
            {
                result = null;
                return false;
            }

            var bracketCount = 1;
            output.Append((char)first);

            while (bracketCount > 0)
            {
                var nextByte = stream.ReadByte();
                if (nextByte == -1)
                {
                    result = null;
                    return false; // Unexpected end of stream
                }
                output.Append((char)nextByte);
                if (nextByte == '{')
                {
                    bracketCount++;
                }
                else if (nextByte == '}')
                {
                    bracketCount--;
                }
            }

            result = output.ToString();
            return true;
        }
    }
}
