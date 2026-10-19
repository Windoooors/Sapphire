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
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        value = Jp2T.Convert(value);
        value = T2S.Convert(value);

        var sb = new StringBuilder(value.Length);

        foreach (var c in value)
        {
            var mapped = c switch
            {
                '\u00C5' or '\u212B' => 'a',
                '\u00E5' => 'a',
                'Ä' or 'ä' => 'a',
                'Ö' or 'ö' => 'o',
                'Ø' or 'ø' => 'o',
                'Æ' or 'æ' => 'a',
                '+' or '＋' => '+',
                _ => c
            };

            sb.Append(MapSimilarCharacter(mapped));
        }

        var decomposed = sb.ToString()
            .Normalize(NormalizationForm.FormKD);

        sb.Clear();

        foreach (var c in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);

            if (category is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.ConnectorPunctuation
                or UnicodeCategory.DashPunctuation
                or UnicodeCategory.OpenPunctuation
                or UnicodeCategory.ClosePunctuation
                or UnicodeCategory.InitialQuotePunctuation
                or UnicodeCategory.FinalQuotePunctuation
                or UnicodeCategory.OtherPunctuation
                or UnicodeCategory.MathSymbol
                or UnicodeCategory.CurrencySymbol
                or UnicodeCategory.ModifierSymbol
                or UnicodeCategory.OtherSymbol)
                continue;

            if (char.IsWhiteSpace(c))
                continue;

            sb.Append(char.ToLowerInvariant(c));
        }

        return sb.ToString().Normalize(NormalizationForm.FormC);

        static char MapSimilarCharacter(char c)
        {
            return c switch
            {
                'Å' or 'å' or 'Å' => 'a',
                'Ä' or 'ä' => 'a',
                'Ö' or 'ö' or 'Ø' or 'ø' => 'o',
                'Æ' or 'æ' => 'a',

                'ε' or 'ϵ' or 'Ε' or 'Ɛ' or 'ɛ' => 'e',

                'ϲ' or 'Ϲ' => 'c',

                'α' or 'Α' => 'a',
                'β' or 'Β' => 'b',

                'ι' or 'Ι' or 'ı' or 'İ' => 'i',
                'κ' or 'Κ' => 'k',
                'ο' or 'Ο' => 'o',
                'ρ' or 'Ρ' => 'p',
                'ς' or 'σ' or 'Σ' => 's',
                'τ' or 'Τ' => 't',
                'χ' or 'Χ' => 'x',
                'υ' or 'Υ' => 'y',

                'γ' or 'Γ' => 'y',
                'φ' or 'ϕ' or 'Φ' => 'f',

                '＋' => '+',

                _ => char.ToLowerInvariant(c)
            };
        }
    }
}