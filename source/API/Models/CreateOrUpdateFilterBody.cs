using Newtonsoft.Json;

namespace Shoko.Plugin.FilterHelper.API.Models;

/// <summary>
/// Body for creating, updating, or previewing a filter.
/// Mirrors the Shoko API v3 <c>Filter.Input.CreateOrUpdateFilterBody</c> shape.
/// </summary>
public class CreateOrUpdateFilterBody
{
    /// <summary>
    /// The filter name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// The ID of the parent filter.
    /// </summary>
    public int? ParentID { get; set; }

    /// <summary>
    /// Indicates the filter should be a directory filter.
    /// </summary>
    public bool IsDirectory { get; set; }

    /// <summary>
    /// Indicates the filter should be hidden unless explicitly requested.
    /// </summary>
    public bool IsHidden { get; set; }

    /// <summary>
    /// Indicates the filter should be applied at the series level.
    /// </summary>
    public bool ApplyAtSeriesLevel { get; set; }

    /// <summary>
    /// The filter expression tree.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public FilterCondition? Expression { get; set; }

    /// <summary>
    /// The sorting criteria.
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public SortingCriteria? Sorting { get; set; }
}
