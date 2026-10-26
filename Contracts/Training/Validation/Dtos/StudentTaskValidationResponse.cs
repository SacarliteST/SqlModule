using System.Text.Json.Serialization;

namespace SQLModule.Contracts.Training.Validation;

/// <summary>Безопасная validation-часть student task details.</summary>
public sealed record StudentTaskValidationResponse(
    int PassingScore,
    StudentSessionStateResponse? Session,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    StudentTaskHintsResponse? Hints);
