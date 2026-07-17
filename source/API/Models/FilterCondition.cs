using Newtonsoft.Json;

namespace Shoko.Plugin.FilterHelper.API.Models;

/// <summary>
/// A node in the filter expression tree. Mirrors the Shoko API v3
/// <c>FilterCondition</c> shape for client-side filtering.
/// </summary>
public class FilterCondition
{
    /// <summary>
    /// Condition type. Matches the FilterExpression name without the
    /// "Expression", "Function", or "Selector" suffix.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The first (left) child expression.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public FilterCondition? Left { get; set; }

    /// <summary>
    /// The second (right) child expression.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public FilterCondition? Right { get; set; }

    /// <summary>
    /// The parameter value, coerced to string.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Parameter { get; set; }

    /// <summary>
    /// The second parameter value, coerced to string.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? SecondParameter { get; set; }
}
