using System.ComponentModel.DataAnnotations;
using TournamentService.DTOs;
using Xunit;

namespace TournamentService.Tests.DTOs;

public class CreateTeamRequestTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }

    [Theory]
    [InlineData("#FF0055")]
    [InlineData("#0077FF")]
    [InlineData("#00FF66")]
    [InlineData("#8A2BE2")]
    [InlineData("#000000")]
    [InlineData("#FFFFFF")]
    [InlineData("#ffffff")]
    [InlineData("#123456")]
    [InlineData("#abcdef")]
    [InlineData("#ABCDEF")]
    public void CreateTeamRequest_ValidColorHex_PassesValidation(string validHex)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = "Team Crimson",
            ColorHex = validHex
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.Empty(results);
    }

    [Theory]
    [InlineData("FF0055")]    // Missing leading '#'
    [InlineData("#FFF")]       // Too short (4 chars)
    [InlineData("#F")]         // Too short (2 chars)
    [InlineData("#FF005511")]  // Too long (9 chars)
    [InlineData("#12345")]     // Too short (6 chars)
    [InlineData("#GG0055")]    // Invalid hex character 'G'
    [InlineData("#ZZ1122")]    // Invalid hex character 'Z'
    [InlineData("#FF 055")]    // Space in hex
    [InlineData("blue")]       // Named color
    [InlineData("#!@#$%^")]    // Symbols
    public void CreateTeamRequest_InvalidColorHex_FailsValidation(string invalidHex)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = "Team Crimson",
            ColorHex = invalidHex
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateTeamRequest.ColorHex)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateTeamRequest_MissingColorHex_FailsValidation(string? missingHex)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = "Team Crimson",
            ColorHex = missingHex!
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateTeamRequest.ColorHex)));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void CreateTeamRequest_MissingTeamName_FailsValidation(string? missingName)
    {
        // Arrange
        var request = new CreateTeamRequest
        {
            TeamName = missingName!,
            ColorHex = "#FF0055"
        };

        // Act
        var results = ValidateModel(request);

        // Assert
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.MemberNames.Contains(nameof(CreateTeamRequest.TeamName)));
    }
}
