namespace Eventify.Shared.Extensions;

/// <summary>
/// String extension methods used across BLL and Web layers.
/// </summary>
public static class StringExtensions
{
    private static readonly Regex NonAlphanumericRegex =
        new(@"[^a-z0-9\-]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MultipleHyphensRegex =
        new("-{2,}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<char, string> ArabicMap =
        new Dictionary<char, string>
        {
            ['ا'] = "a",
            ['أ'] = "a",
            ['إ'] = "i",
            ['آ'] = "aa",
            ['ب'] = "b",
            ['ت'] = "t",
            ['ث'] = "th",
            ['ج'] = "j",
            ['ح'] = "h",
            ['خ'] = "kh",
            ['د'] = "d",
            ['ذ'] = "th",
            ['ر'] = "r",
            ['ز'] = "z",
            ['س'] = "s",
            ['ش'] = "sh",
            ['ص'] = "s",
            ['ض'] = "d",
            ['ط'] = "t",
            ['ظ'] = "z",
            ['ع'] = "a",
            ['غ'] = "gh",
            ['ف'] = "f",
            ['ق'] = "q",
            ['ك'] = "k",
            ['ل'] = "l",
            ['م'] = "m",
            ['ن'] = "n",
            ['ه'] = "h",
            ['و'] = "w",
            ['ي'] = "y",
            ['ى'] = "a",
            ['ة'] = "h",
            ['ء'] = "",
            ['ئ'] = "y",
            ['ؤ'] = "w",
        };


    /// <summary>
    /// Converts a string into a URL-friendly slug.
    /// </summary>
    public static string ToSlug(this string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var normalized = value.Normalize(NormalizationForm.FormD);

        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            sb.Append(c);
        }

        var result = sb.ToString()
                       .Normalize(NormalizationForm.FormC)
                       .ToLowerInvariant();

        result = TransliterateArabic(result);

        result = NonAlphanumericRegex.Replace(result, "-");
        result = MultipleHyphensRegex.Replace(result, "-");

        return result.Trim('-');
    }

    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (maxLength <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxLength),
                "Max length must be greater than zero.");

        if (value.Length <= maxLength)
            return value;

        var suffixLength = suffix.Length;

        if (maxLength <= suffixLength)
            return suffix[..maxLength];

        return string.Concat(value.AsSpan(0, maxLength - suffixLength), suffix);
    }

    public static bool IsEmpty(this string? value)
        => string.IsNullOrWhiteSpace(value);

    public static bool HasValue(this string? value)
        => !string.IsNullOrWhiteSpace(value);

    private static string TransliterateArabic(string input)
    {
        var sb = new StringBuilder();

        foreach (var c in input)
        {
            if (ArabicMap.TryGetValue(c, out var replacement))
                sb.Append(replacement);
            else
                sb.Append(c);
        }

        return sb.ToString();
    }
}