using System.ComponentModel.DataAnnotations;
using TournamentService.DTOs;
using TournamentService.Models;
using Xunit;

namespace TournamentService.Tests.DTOs;

public class TournamentDtoTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void CreateTournamentRequest_ValidModel_HasNoValidationErrors()
    {
        var request = new CreateTournamentRequest
        {
            Name = "Winter Championship",
            SeasonIdentifier = "WINTER-2026",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(14)
        };

        var errors = ValidateModel(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void CreateTournamentRequest_MissingRequiredFields_FailsValidation()
    {
        var request = new CreateTournamentRequest();

        var errors = ValidateModel(request);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateTournamentRequest.Name)));
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(CreateTournamentRequest.SeasonIdentifier)));
    }

    [Fact]
    public void UpdateTournamentRequest_ValidModel_HasNoValidationErrors()
    {
        var request = new UpdateTournamentRequest
        {
            Name = "Spring Championship",
            SeasonIdentifier = "SPRING-2026",
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddDays(14)
        };

        var errors = ValidateModel(request);
        Assert.Empty(errors);
    }

    [Fact]
    public void TournamentStatus_AllowedStatuses_ContainsExpectedValues()
    {
        Assert.True(TournamentStatus.IsValid("Scheduled"));
        Assert.True(TournamentStatus.IsValid("Active"));
        Assert.True(TournamentStatus.IsValid("Completed"));
        Assert.True(TournamentStatus.IsValid("Cancelled"));
        Assert.False(TournamentStatus.IsValid("Archived"));
        Assert.False(TournamentStatus.IsValid("Unknown"));
    }
}
