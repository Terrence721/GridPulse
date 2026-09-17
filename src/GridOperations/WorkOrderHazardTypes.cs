namespace GridPulse.GridOperations;

public static class WorkOrderHazardTypes
{
    public const string DownedWire = "DownedWire";
    public const string BlownTransformer = "BlownTransformer";
    public const string BrokenPole = "BrokenPole";
    public const string VegetationContact = "VegetationContact";
    public const string EquipmentFailure = "EquipmentFailure";
    public const string VehicleAccident = "VehicleAccident";
    public const string WeatherDamage = "WeatherDamage";
    public const string AnimalContact = "AnimalContact";
    public const string SuspectedOutage = "SuspectedOutage";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        DownedWire, BlownTransformer, BrokenPole, VegetationContact,
        EquipmentFailure, VehicleAccident, WeatherDamage, AnimalContact, SuspectedOutage
    };
}
