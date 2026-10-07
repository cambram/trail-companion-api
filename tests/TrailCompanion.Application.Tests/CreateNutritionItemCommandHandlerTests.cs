using TrailCompanion.Application.Modules.Users.Commands;
using TrailCompanion.Application.Modules.Users.Dtos;
using TrailCompanion.Domain.Common.Exceptions;
using TrailCompanion.Domain.Modules.Users.Entities;
using TrailCompanion.Domain.Modules.Users.Repositories;
using NSubstitute;

namespace TrailCompanion.Application.Tests;

public class CreateNutritionItemCommandHandlerTests
{
    private readonly INutritionItemsRepository _nutrition = Substitute.For<INutritionItemsRepository>();
    private readonly Guid _appUserId = Guid.NewGuid();

    private CreateNutritionItemCommandHandler CreateHandler() => new(_nutrition);

    private static NutritionItemRequest Request(
        string? name = "Maurten Gel 160",
        NutritionType? type = NutritionType.Gel,
        int? size = null,
        int? carbs = null) =>
        new(name, type, size, carbs, null, null, null, null, null, null);

    [Fact]
    public async Task Handle_TrimsTheNameAndKeepsBlankMacrosNull()
    {
        var request = new NutritionItemRequest(
            "  Maurten Gel 160 ", NutritionType.Gel, 40, 40, null, 100, null, null, null, null);

        var result = await CreateHandler().Handle(
            new CreateNutritionItemCommand(_appUserId, request), CancellationToken.None);

        Assert.Equal("Maurten Gel 160", result.Name);
        Assert.Equal(NutritionType.Gel, result.Type);
        Assert.Equal(40, result.Size);
        Assert.Equal(40, result.Carbs);
        Assert.Null(result.Calories);
        Assert.Equal(100, result.Caffeine);
        Assert.Null(result.Sodium);
        await _nutrition.Received(1).AddAsync(
            Arg.Is<NutritionItems>(n => n!.AppUserId == _appUserId && n.Carbs == 40 && n.Calories == null),
            Arg.Any<CancellationToken>());
        await _nutrition.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAZeroMacro_StoresZeroNotNull()
    {
        var result = await CreateHandler().Handle(
            new CreateNutritionItemCommand(_appUserId, Request(carbs: 0)), CancellationToken.None);

        Assert.Equal(0, result.Carbs);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task Handle_WithABlankName_ThrowsInvalidName(string? name)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(new CreateNutritionItemCommand(_appUserId, Request(name)), CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidName");
        await _nutrition.DidNotReceive().AddAsync(Arg.Any<NutritionItems>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithATooLongName_ThrowsInvalidName()
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(
                new CreateNutritionItemCommand(_appUserId, Request(new string('a', 201))), CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidName");
    }

    [Theory]
    [InlineData(null)]
    [InlineData((NutritionType)97)]
    public async Task Handle_WithAMissingOrUndefinedType_ThrowsInvalidType(NutritionType? type)
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(
                new CreateNutritionItemCommand(_appUserId, Request(type: type)), CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidType");
        await _nutrition.DidNotReceive().AddAsync(Arg.Any<NutritionItems>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithANegativeMacro_ThrowsAndSavesNothing()
    {
        var request = new NutritionItemRequest(
            "A", NutritionType.Gel, null, null, null, null, -1, null, null, null);

        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(new CreateNutritionItemCommand(_appUserId, request), CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidSodium");
        await _nutrition.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithANegativeSize_Throws()
    {
        var exception = await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(
                new CreateNutritionItemCommand(_appUserId, Request(size: -5)), CancellationToken.None));

        Assert.Contains(exception.Failures, f => f.Code == "InvalidSize");
    }

    [Fact]
    public async Task Handle_AtTheLibraryLimit_ThrowsAndStoresNothing()
    {
        _nutrition.GetAllAsync(_appUserId, Arg.Any<CancellationToken>())
            .Returns(Enumerable.Range(0, AccountLimitRules.MaxNutritionItems).Select(_ => new NutritionItems()).ToList());

        await Assert.ThrowsAsync<ValidationFailedException>(() =>
            CreateHandler().Handle(new CreateNutritionItemCommand(_appUserId, Request()), CancellationToken.None));

        await _nutrition.DidNotReceive().AddAsync(Arg.Any<NutritionItems>(), Arg.Any<CancellationToken>());
    }
}
