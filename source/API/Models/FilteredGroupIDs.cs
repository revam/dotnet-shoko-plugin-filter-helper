using System.Collections.Generic;

namespace Shoko.Plugin.FilterHelper.API.Models;

/// <summary>
/// Lightweight version of <see cref="Shoko.Abstractions.Filtering.FilteredGroupResult"/>
/// that returns the group ID instead of the full group object.
/// </summary>
public sealed class FilteredGroupIDs
{
    /// <summary>
    /// The group ID for the filtered result.
    /// </summary>
    public required int GroupID { get; init; }

    /// <summary>
    /// All group ID chains for the filtered result.
    /// </summary>
    public required IReadOnlyList<IReadOnlyList<int>> GroupIDChains { get; init; }

    /// <summary>
    /// All series IDs for the filtered result.
    /// </summary>
    public required IReadOnlySet<int> SeriesIDs { get; init; }
}
