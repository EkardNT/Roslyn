using System;

namespace Microsoft.CodeAnalysis.CSharp.Utilities
{
    public static class OctalParsing
    {
        public static bool TryParse(string text, out ulong result)
        {
            result = 0;

            if (text == null)
                return false;
            if (!HasOctalPrefix(text))
                return false;

            // This is a placeholder implementation. It will be replaced 
            // with a more performant manual conversion once the rest 
            // of the octal literal code is working.
            try
            {
                result = Convert.ToUInt64(text.Substring(2, text.Length - 2), 8);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tests whether the provided text starts with an octal prefix,
        /// which is either "0o" or "0O".
        /// </summary>
        /// <param name="text">The text that may contain an octal prefix.</param>
        /// <returns>True if an octal prefix is specified, false otherwise.</returns>
        private static bool HasOctalPrefix(string text)
        {
            if (text.Length < 2)
                return false;
            if (text[0] != '0')
                return false;
            return text[1] == 'o' || text[1] == 'O';
        }
    }
}
