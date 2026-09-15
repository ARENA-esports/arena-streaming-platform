namespace TournamentService.Exceptions;

/// <summary>
/// Exception thrown when a team creation or update conflicts with an existing team (e.g. duplicate team name).
/// </summary>
public class TeamConflictException : Exception
{
    public string ConflictingName { get; }

    public TeamConflictException(string conflictingName)
        : base($"A team with the name '{conflictingName}' already exists.")
    {
        ConflictingName = conflictingName;
    }

    public TeamConflictException(string conflictingName, Exception innerException)
        : base($"A team with the name '{conflictingName}' already exists.", innerException)
    {
        ConflictingName = conflictingName;
    }
}
