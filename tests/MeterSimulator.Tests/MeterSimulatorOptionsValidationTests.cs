using System.ComponentModel.DataAnnotations;

namespace GridPulse.MeterSimulator.Tests;

public sealed class MeterSimulatorOptionsValidationTests
{
    private static MeterSimulatorOptions ValidOptions() => new()
    {
        StreetName = "Elm St",
        StartingAddress = 100,
        BuildingsPerSide = 14,
        City = "Lansing",
        ZipCode = "48933"
    };

    private static IList<ValidationResult> Validate(MeterSimulatorOptions options)
    {
        var context = new ValidationContext(options);
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(options, context, results, validateAllProperties: true);
        return results;
    }

    [Fact]
    public void Validate_FullyConfiguredOptions_ProducesNoErrors()
    {
        var results = Validate(ValidOptions());

        Assert.Empty(results);
    }

    [Fact]
    public void Validate_MissingStreetName_ProducesError()
    {
        var options = ValidOptions();
        options.StreetName = "";

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MeterSimulatorOptions.StreetName)));
    }

    [Fact]
    public void Validate_ZeroStartingAddress_ProducesError()
    {
        var options = ValidOptions();
        options.StartingAddress = 0;

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MeterSimulatorOptions.StartingAddress)));
    }

    [Fact]
    public void Validate_ZeroBuildingsPerSide_ProducesError()
    {
        var options = ValidOptions();
        options.BuildingsPerSide = 0;

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MeterSimulatorOptions.BuildingsPerSide)));
    }

    [Fact]
    public void Validate_MissingCity_ProducesError()
    {
        var options = ValidOptions();
        options.City = "";

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MeterSimulatorOptions.City)));
    }

    [Fact]
    public void Validate_MissingZipCode_ProducesError()
    {
        var options = ValidOptions();
        options.ZipCode = "";

        var results = Validate(options);

        Assert.Contains(results, r => r.MemberNames.Contains(nameof(MeterSimulatorOptions.ZipCode)));
    }

    [Fact]
    public void Validate_AllFieldsUnset_ProducesFiveErrors()
    {
        var options = new MeterSimulatorOptions();

        var results = Validate(options);

        Assert.Equal(5, results.Count);
    }
}
