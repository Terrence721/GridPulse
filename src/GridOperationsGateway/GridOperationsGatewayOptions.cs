using System.ComponentModel.DataAnnotations;

namespace GridPulse.GridOperationsGateway;

public sealed class GridOperationsGatewayOptions
{
    public const string SectionName = "GridOperationsGateway";

    [Required(ErrorMessage = "AllowedCorsOrigins must be set (at least one allowed browser origin, e.g. http://localhost:5173).")]
    [MinLength(1, ErrorMessage = "AllowedCorsOrigins must contain at least one origin.")]
    public string[] AllowedCorsOrigins { get; set; } = [];
}
