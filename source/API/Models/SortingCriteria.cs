using Newtonsoft.Json;

namespace Shoko.Plugin.FilterHelper.API.Models;

/// <summary>
/// Sorting criteria for ordering filter results.
/// Follows an OrderBy().ThenBy().ThenBy() chain pattern.
/// Mirrors the Shoko API v3 <c>SortingCriteria</c> shape.
/// </summary>
public class SortingCriteria
{
    /// <summary>
    /// The sorting type name (SortingExpression name without "SortingSelector").
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// The next criteria to fall back on when this one is equal.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public SortingCriteria? Next { get; set; }

    /// <summary>
    /// Assumed Ascending unless set to true (for descending).
    /// </summary>
    public bool IsInverted { get; set; }

    /// <summary>
    /// Optional parameter for sorting types that require one,
    /// such as FuzzyNameRelevance (query string).
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Parameter { get; set; }
}
