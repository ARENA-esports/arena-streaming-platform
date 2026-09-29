using System.Text.RegularExpressions;

namespace ChatService.Services;

/// <summary>
/// Regex-based profanity filter that replaces blacklisted words with asterisks (***).
/// Uses word boundaries (\b) to prevent partial-word false positives (e.g., "class" won't match "ass").
/// </summary>
public class ProfanityFilter : IProfanityFilter
{
    private readonly Regex _profanityRegex;

    /// <summary>
    /// Blacklisted words. This list can be externalized to configuration in the future.
    /// </summary>
    private static readonly string[] BlacklistedWords =
    {
        "damn", "hell", "crap", "shit", "fuck", "ass",
        "bitch", "bastard", "dick", "piss", "slut",
        "whore", "idiot", "stupid", "dumb", "moron",
        "retard", "nigger", "faggot", "cock", "pussy",
        "wanker", "twat", "bollocks"
    };

    public ProfanityFilter()
    {
        // Build a compiled regex: \b(word1|word2|...)\b with case-insensitive matching.
        // Word boundaries ensure partial matches like "class" don't trigger on "ass".
        var pattern = @"\b(" + string.Join("|", BlacklistedWords.Select(Regex.Escape)) + @")\b";
        _profanityRegex = new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    /// <inheritdoc />
    public string Scrub(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        return _profanityRegex.Replace(input, "***");
    }

    /// <inheritdoc />
    public bool ContainsProfanity(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        return _profanityRegex.IsMatch(input);
    }
}
