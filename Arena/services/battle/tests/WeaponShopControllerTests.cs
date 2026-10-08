using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using BattleEconomyService.Controllers;
using BattleEconomyService.DTOs;
using BattleEconomyService.Services;
using Xunit;

namespace BattleEconomyService.Tests;

public class WeaponShopControllerTests
{
    private readonly Mock<IWeaponShopService> _weaponShopServiceMock;
    private readonly Mock<ILogger<WeaponShopController>> _loggerMock;

    public WeaponShopControllerTests()
    {
        _weaponShopServiceMock = new Mock<IWeaponShopService>();
        _loggerMock = new Mock<ILogger<WeaponShopController>>();
    }

    private WeaponShopController CreateController(ClaimsPrincipal? user = null)
    {
        var controller = new WeaponShopController(_weaponShopServiceMock.Object, _loggerMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = user ?? new ClaimsPrincipal(new ClaimsIdentity(new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, "42")
                    }, "TestAuth"))
                }
            }
        };
        return controller;
    }

    [Fact]
    public async Task GetWeapons_Returns200_WithWeaponList()
    {
        // Arrange
        var mockWeapons = new List<WeaponDto>
        {
            new() { WeaponId = 1, Name = "Throwing Knife", Cost = 10, Damage = 1, IconKey = "knife" },
            new() { WeaponId = 2, Name = "Crossbow Bolt", Cost = 25, Damage = 3, IconKey = "crossbow" }
        };

        _weaponShopServiceMock.Setup(s => s.GetWeaponsAsync())
            .ReturnsAsync(mockWeapons);

        var controller = CreateController();

        // Act
        var result = await controller.GetWeapons();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var weapons = Assert.IsAssignableFrom<List<WeaponDto>>(okResult.Value);
        Assert.Equal(2, weapons.Count);
        Assert.Equal("Throwing Knife", weapons[0].Name);
    }

    [Fact]
    public async Task PurchaseAttack_Success_Returns200_WithAttackResponse()
    {
        // Arrange
        var request = new AttackRequest { WeaponId = 1, MatchId = 100, TeamId = 2 };
        var serviceResponse = new AttackResponse
        {
            Success = true,
            AttackId = 999,
            CoinsSpent = 10,
            CurrentBalance = 90,
            DamageDealt = 1,
            Message = "Throwing Knife attack launched! Dealt 1 damage."
        };

        _weaponShopServiceMock.Setup(s => s.PurchaseAttackAsync(42, request))
            .ReturnsAsync(serviceResponse);

        var controller = CreateController();

        // Act
        var result = await controller.PurchaseAttack(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(okResult.Value);
        Assert.True(response.Success);
        Assert.Equal(999, response.AttackId);
        Assert.Equal(10, response.CoinsSpent);
        Assert.Equal(90, response.CurrentBalance);
    }

    [Fact]
    public async Task PurchaseAttack_InsufficientCoins_Returns400()
    {
        // Arrange
        var request = new AttackRequest { WeaponId = 4, MatchId = 100, TeamId = 2 };
        var serviceResponse = new AttackResponse
        {
            Success = false,
            CurrentBalance = 5,
            Message = "Insufficient coins to purchase this weapon."
        };

        _weaponShopServiceMock.Setup(s => s.PurchaseAttackAsync(42, request))
            .ReturnsAsync(serviceResponse);

        var controller = CreateController();

        // Act
        var result = await controller.PurchaseAttack(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(badRequestResult.Value);
        Assert.False(response.Success);
        Assert.Equal(5, response.CurrentBalance);
        Assert.Equal("Insufficient coins to purchase this weapon.", response.Message);
    }

    [Fact]
    public async Task PurchaseAttack_NullRequest_Returns400()
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.PurchaseAttack(null);

        // Assert
        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Equal("Request body is required.", response.Message);
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    public async Task PurchaseAttack_InvalidWeaponId_Returns400(int weaponId, int matchId, int teamId)
    {
        var controller = CreateController();
        var result = await controller.PurchaseAttack(new AttackRequest { WeaponId = weaponId, MatchId = matchId, TeamId = teamId });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("weapon ID", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(1, -1, 1)]
    public async Task PurchaseAttack_InvalidMatchId_Returns400(int weaponId, int matchId, int teamId)
    {
        var controller = CreateController();
        var result = await controller.PurchaseAttack(new AttackRequest { WeaponId = weaponId, MatchId = matchId, TeamId = teamId });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("match ID", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -1)]
    public async Task PurchaseAttack_InvalidTeamId_Returns400(int weaponId, int matchId, int teamId)
    {
        var controller = CreateController();
        var result = await controller.PurchaseAttack(new AttackRequest { WeaponId = weaponId, MatchId = matchId, TeamId = teamId });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(badRequest.Value);
        Assert.False(response.Success);
        Assert.Contains("team ID", response.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PurchaseAttack_MissingUserIdClaim_Returns401()
    {
        // Arrange: empty identity without NameIdentifier
        var emptyUser = new ClaimsPrincipal(new ClaimsIdentity());
        var controller = CreateController(emptyUser);

        // Act
        var result = await controller.PurchaseAttack(new AttackRequest { WeaponId = 1, MatchId = 1, TeamId = 1 });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task PurchaseAttack_SubClaimSupported_Returns200()
    {
        // Arrange: JWT with "sub" claim instead of NameIdentifier
        var subUser = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sub", "88")
        }, "TestAuth"));

        var request = new AttackRequest { WeaponId = 1, MatchId = 1, TeamId = 1 };
        _weaponShopServiceMock.Setup(s => s.PurchaseAttackAsync(88, request))
            .ReturnsAsync(new AttackResponse { Success = true });

        var controller = CreateController(subUser);

        // Act
        var result = await controller.PurchaseAttack(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AttackResponse>(okResult.Value);
        Assert.True(response.Success);
    }

    [Fact]
    public async Task GetBattleBar_ValidMatchId_Returns200_WithBars()
    {
        // Arrange
        var bars = new List<BattleEconomyService.Models.BattleBar>
        {
            new() { BarId = 1, MatchId = 10, TeamId = 1, TotalDamage = 150 },
            new() { BarId = 2, MatchId = 10, TeamId = 2, TotalDamage = 200 }
        };
        _weaponShopServiceMock.Setup(s => s.GetBarsForMatchAsync(10)).ReturnsAsync(bars);

        var controller = CreateController();

        // Act
        var result = await controller.GetBattleBar(10);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBars = Assert.IsAssignableFrom<List<BattleEconomyService.Models.BattleBar>>(okResult.Value);
        Assert.Equal(2, returnedBars.Count);
        Assert.Equal(150, returnedBars[0].TotalDamage);
        Assert.Equal(200, returnedBars[1].TotalDamage);
    }

    [Fact]
    public async Task GetBattleBar_NoDataForMatch_Returns200_WithEmptyList()
    {
        // Arrange
        _weaponShopServiceMock.Setup(s => s.GetBarsForMatchAsync(999))
            .ReturnsAsync(new List<BattleEconomyService.Models.BattleBar>());

        var controller = CreateController();

        // Act
        var result = await controller.GetBattleBar(999);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedBars = Assert.IsAssignableFrom<List<BattleEconomyService.Models.BattleBar>>(okResult.Value);
        Assert.Empty(returnedBars);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetBattleBar_InvalidMatchId_Returns400(int matchId)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetBattleBar(matchId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GetRoundHistory_ValidMatchId_Returns200OkWithHistoryList()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var mockHistory = new List<BattleRoundHistoryDto>
        {
            new()
            {
                RoundId = 2,
                MatchId = 101,
                RoundNumber = 2,
                WinningTeamId = 2,
                TargetDamage = 100,
                FinalBarState = new List<BattleBarDto>
                {
                    new() { TeamId = 1, TotalDamage = 60 },
                    new() { TeamId = 2, TotalDamage = 100 }
                },
                CreatedAt = now.AddMinutes(-5),
                EndedAt = now
            },
            new()
            {
                RoundId = 1,
                MatchId = 101,
                RoundNumber = 1,
                WinningTeamId = 1,
                TargetDamage = 100,
                FinalBarState = new List<BattleBarDto>
                {
                    new() { TeamId = 1, TotalDamage = 100 },
                    new() { TeamId = 2, TotalDamage = 45 }
                },
                CreatedAt = now.AddMinutes(-10),
                EndedAt = now.AddMinutes(-5)
            }
        };

        _weaponShopServiceMock.Setup(s => s.GetRoundHistoryAsync(101))
            .ReturnsAsync(mockHistory);

        var controller = CreateController();

        // Act
        var result = await controller.GetRoundHistory(101);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedHistory = Assert.IsAssignableFrom<List<BattleRoundHistoryDto>>(okResult.Value);
        Assert.Equal(2, returnedHistory.Count);
        Assert.Equal(2, returnedHistory[0].RoundNumber);
        Assert.Equal(1, returnedHistory[1].RoundNumber);
    }

    [Fact]
    public async Task GetRoundHistory_EmptyHistory_Returns200OkWithEmptyList()
    {
        // Arrange
        _weaponShopServiceMock.Setup(s => s.GetRoundHistoryAsync(999))
            .ReturnsAsync(new List<BattleRoundHistoryDto>());

        var controller = CreateController();

        // Act
        var result = await controller.GetRoundHistory(999);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedHistory = Assert.IsAssignableFrom<List<BattleRoundHistoryDto>>(okResult.Value);
        Assert.Empty(returnedHistory);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task GetRoundHistory_InvalidMatchId_Returns400(int matchId)
    {
        // Arrange
        var controller = CreateController();

        // Act
        var result = await controller.GetRoundHistory(matchId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }
}
