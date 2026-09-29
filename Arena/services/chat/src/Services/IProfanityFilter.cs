namespace ChatService.Services;

public interface IProfanityFilter
{
    /// <summary>
    /// Returns the input with all blacklisted words replaced by asterisks (***).
    /// </summary>
    string Scrub(string input);

    /// <summary>
    /// Returns true if the input contains any blacklisted words.
    /// </summary>
    bool ContainsProfanity(string input);
}
