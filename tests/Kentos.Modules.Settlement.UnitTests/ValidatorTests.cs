using Kentos.Modules.Settlement.Application.Neighborhoods;
using Kentos.Modules.Settlement.Application.Provinces;
using Shouldly;

namespace Kentos.Modules.Settlement.UnitTests;

public sealed class ValidatorTests
{
    [Fact]
    public void CreateProvince_rejects_empty_name()
    {
        var result = new CreateProvinceCommandValidator().Validate(new CreateProvinceCommand("", 34));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateProvince_rejects_out_of_range_plate_code()
    {
        var result = new CreateProvinceCommandValidator().Validate(new CreateProvinceCommand("Istanbul", 99));
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateProvince_accepts_valid_input()
    {
        var result = new CreateProvinceCommandValidator().Validate(new CreateProvinceCommand("Istanbul", 34));
        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void CreateNeighborhood_rejects_invalid_latitude()
    {
        var command = new CreateNeighborhoodCommand("Center", Guid.NewGuid(), "34000", 200, 29, null);
        var result = new CreateNeighborhoodCommandValidator().Validate(command);
        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void CreateNeighborhood_accepts_valid_input()
    {
        var command = new CreateNeighborhoodCommand("Center", Guid.NewGuid(), "34000", 41.0, 29.0, null);
        var result = new CreateNeighborhoodCommandValidator().Validate(command);
        result.IsValid.ShouldBeTrue();
    }
}
