using System.Globalization;
using System.Text;
using OpenccNetLib;

namespace Sapphire.Utils;

public static class SearchHelper
{
    private static readonly Opencc Jp2T = new(OpenccConfig.Jp2T);
    private static readonly Opencc T2S = new(OpenccConfig.T2S);

    public static string NormalizeCjkString(string value)
    {
        var builder = new StringBuilder(value.Length);

        value = Jp2T.Convert(value);
        value = T2S.Convert(value);

        foreach (var c in value)
        {
            if (char.IsWhiteSpace(c))
                continue;

            if (IsIgnoredCharacter(c)) continue;

            builder.Append(c);
        }

        return builder.ToString();

        static bool IsIgnoredCharacter(char c)
        {
            if (char.IsWhiteSpace(c))
                return true;

            return CharUnicodeInfo.GetUnicodeCategory(c) switch
            {
                UnicodeCategory.ConnectorPunctuation => true,
                UnicodeCategory.DashPunctuation => true,
                UnicodeCategory.OpenPunctuation => true,
                UnicodeCategory.ClosePunctuation => true,
                UnicodeCategory.InitialQuotePunctuation => true,
                UnicodeCategory.FinalQuotePunctuation => true,
                UnicodeCategory.OtherPunctuation => true,

                UnicodeCategory.MathSymbol => true,
                UnicodeCategory.CurrencySymbol => true,
                UnicodeCategory.ModifierSymbol => true,
                UnicodeCategory.OtherSymbol => true,

                _ => false
            };
        }
    }
}