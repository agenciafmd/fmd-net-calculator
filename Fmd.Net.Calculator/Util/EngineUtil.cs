using System.Text;

namespace Fmd.Net.Calculator.Util;

/// <summary>
/// Utility methods of Jace.NET that can be used throughout the engine.
/// </summary>
internal static class EngineUtil
{
    internal static IDictionary<string, decimal> ConvertVariableNamesToLowerCase(IDictionary<string, decimal> variables)
    {
        Dictionary<string, decimal> temp = new Dictionary<string, decimal>();
        foreach (KeyValuePair<string, decimal> keyValuePair in variables)
        {
            temp.Add(keyValuePair.Key.ToLowerFast(), keyValuePair.Value);
        }

        return temp;
    }

    // This is a fast ToLower for strings that are in ASCII
    internal static string ToLowerFast(this string text)
    {
        StringBuilder buffer = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            if (c >= 'A' && c <= 'Z')
            {
                buffer.Append((char)(c + 32));
            }
            else
            {
                buffer.Append(c);
            }
        }

        return buffer.ToString();
    }
}