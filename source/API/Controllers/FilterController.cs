using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shoko.Abstractions.Filtering;
using Shoko.Abstractions.Filtering.Generic;
using Shoko.Abstractions.Filtering.Services;
using Shoko.Abstractions.Metadata.Services;
using Shoko.Abstractions.Metadata.Shoko;
using Shoko.Abstractions.User;
using Shoko.Abstractions.User.Services;
using Shoko.Plugin.FilterHelper.API.Models;
using Shoko.Plugin.FilterHelper.Services;

namespace Shoko.Plugin.FilterHelper.API.Controllers;

/// <summary>
///   Lightweight filter endpoints for client-side filtering.
/// </summary>
[ApiController]
[Authorize]
[Route("/api/plugin/FilterHelper")]
public sealed class FilterController(
    IMetadataService metadataService,
    IMetadataFilteringService filteringService,
    IFilterPresetManager filterPresetManager,
    IUserService userService,
    FilterConversionService conversionService
) : ControllerBase
{
    private const string FilterNotFound = "No Filter exists for the given filter ID";

    private const string GroupNotFound = "No Group exists for the given group ID";

    /// <summary>
    ///   A filter that matches everything. With no expression, and no sorting
    ///   expression.
    /// </summary>
    private static readonly IFilter NoOpFilter = new GenericFilter
    {
        ApplyAtSeriesLevel = true,
        Expression = null,
        SortingExpression = null,
    };

    private new IUser User => userService.GetUserFromHttpContext(HttpContext)!;

    /// <summary>
    ///   Get a list of all (GroupID, SeriesID) tuples for the
    ///   <see cref="IFilter"/> with the given <paramref name="filterID"/>
    ///   for client-side filtering.
    /// </summary>
    /// <param name="filterID">Filter ID</param>
    /// <returns></returns>
    [HttpGet("{filterID}/TupleIDs")]
    public ActionResult<List<GroupSeriesTuple>> GetFilteredTuples(
        [FromRoute, Range(0, int.MaxValue)] int filterID
    )
    {
        var user = User;
        if (filterID is 0)
            return filteringService.Engine.EvaluateFilterWithTuples(
                NoOpFilter,
                user,
                cancellationToken: HttpContext.RequestAborted
            )
                .Select(tuple => new GroupSeriesTuple
                {
                    GroupID = tuple.GroupID,
                    SeriesID = tuple.SeriesID,
                })
                .ToList();

        if (filterPresetManager.GetPresetById(filterID) is not { } filterPreset)
            return NotFound(FilterNotFound);

        return filteringService.Engine.EvaluateFilterWithTuples(
            filterPreset,
            user,
            cancellationToken: HttpContext.RequestAborted
        )
            .Select(tuple => new GroupSeriesTuple
            {
                GroupID = tuple.GroupID,
                SeriesID = tuple.SeriesID,
            })
            .ToList();
    }

    /// <summary>
    ///   Get a list of filtered group IDs with hierarchy chain information
    ///   for the <see cref="IFilter"/> with the given
    ///   <paramref name="filterID"/>.
    /// </summary>
    /// <param name="filterID">Filter ID</param>
    /// <param name="topLevelOnly">Only list the top level groups if set.</param>
    /// <param name="includeEmpty">Include groups with no series.</param>
    /// <returns></returns>
    [HttpGet("{filterID}/FilteredIDs")]
    public ActionResult<List<FilteredGroupIDs>> GetFilteredGroupIDs(
        [FromRoute, Range(0, int.MaxValue)] int filterID,
        [FromQuery] bool topLevelOnly = true,
        [FromQuery] bool includeEmpty = true
    )
    {
        if (filterID is 0)
            return MapResults(
                topLevelOnly
                    ? filteringService.GetTopLevelFilteredGroups(
                        NoOpFilter,
                        User,
                        cancellationToken: HttpContext.RequestAborted
                    )
                    : filteringService.GetAllFilteredGroupsWithChains(
                        NoOpFilter,
                        User,
                        cancellationToken: HttpContext.RequestAborted
                    ),
                includeEmpty
            );

        if (filterPresetManager.GetPresetById(filterID) is not { } filterPreset)
            return NotFound(FilterNotFound);

        return MapResults(
            topLevelOnly
                ? filteringService.GetTopLevelFilteredGroups(
                    filterPreset,
                    User,
                    cancellationToken: HttpContext.RequestAborted
                )
                : filteringService.GetAllFilteredGroupsWithChains(
                    filterPreset,
                    User,
                    cancellationToken: HttpContext.RequestAborted
                ),
            includeEmpty
        );
    }

    /// <summary>
    ///   Get a list of filtered sub-group IDs with hierarchy chain information
    ///   for the <see cref="IFilter"/> within the group.
    /// </summary>
    /// <param name="filterID">Filter ID</param>
    /// <param name="groupID">Group ID</param>
    /// <param name="includeEmpty">Include groups with no series.</param>
    /// <returns></returns>
    [HttpGet("{filterID}/Group/{groupID}/FilteredIDs")]
    public ActionResult<List<FilteredGroupIDs>> GetFilteredSubGroupIDs(
        [FromRoute, Range(0, int.MaxValue)] int filterID,
        [FromRoute, Range(1, int.MaxValue)] int groupID,
        [FromQuery] bool includeEmpty = true
    )
    {
        var user = User;
        if (filterID is 0)
        {
            
            if (metadataService.GetShokoGroupByID(groupID) is not { } group0)
                return NotFound(GroupNotFound);

            if (!user.IsAllowedToSee(group0))
                return Forbid();

            return MapResults(filteringService.GetFilteredSubGroups(
                NoOpFilter,
                group0,
                user,
                cancellationToken: HttpContext.RequestAborted
            ), includeEmpty);
        }

        if (filterPresetManager.GetPresetById(filterID) is not { } filterPreset)
            return NotFound(FilterNotFound);

        if (metadataService.GetShokoGroupByID(groupID) is not { } group)
            return NotFound(GroupNotFound);

        if (!user.IsAllowedToSee(group))
            return Forbid();

        return MapResults(filteringService.GetFilteredSubGroups(
            filterPreset,
            group,
            user,
            cancellationToken: HttpContext.RequestAborted
        ), includeEmpty);
    }

    /// <summary>
    ///   Get a list of all (GroupID, SeriesID) tuples for the live filter
    ///   for client-side filtering.
    /// </summary>
    /// <param name="filter">The filter to preview</param>
    /// <returns></returns>
    [HttpPost("Preview/TupleIDs")]
    public ActionResult<List<GroupSeriesTuple>> GetPreviewFilteredTuples(
        [FromBody] CreateOrUpdateFilterBody filter
    )
    {
        var filterToEvaluate = conversionService.ConvertToGenericFilter(filter);
        return filteringService.Engine.EvaluateFilterWithTuples(
            filterToEvaluate,
            User,
            cancellationToken: HttpContext.RequestAborted
        )
            .Select(tuple => new GroupSeriesTuple
            {
                GroupID = tuple.GroupID,
                SeriesID = tuple.SeriesID,
            })
            .ToList();
    }

    /// <summary>
    ///   Get a list of filtered group IDs with hierarchy chain information
    ///   for the live filter.
    /// </summary>
    /// <param name="filter">The filter to preview</param>
    /// <param name="topLevelOnly">Only list the top level groups if set.</param>
    /// <param name="includeEmpty">Include groups with no series.</param>
    /// <returns></returns>
    [HttpPost("Preview/FilteredIDs")]
    public ActionResult<List<FilteredGroupIDs>> GetPreviewFilteredGroupIDs(
        [FromBody] CreateOrUpdateFilterBody filter,
        [FromQuery] bool topLevelOnly = true,
        [FromQuery] bool includeEmpty = true
    )
    {
        var filterToEvaluate = conversionService.ConvertToGenericFilter(filter);
        return MapResults(
            topLevelOnly
                ? filteringService.GetTopLevelFilteredGroups(
                    filterToEvaluate,
                    User,
                    cancellationToken: HttpContext.RequestAborted
                )
                : filteringService.GetAllFilteredGroupsWithChains(
                    filterToEvaluate,
                    User,
                    cancellationToken: HttpContext.RequestAborted
                ),
            includeEmpty
        );
    }

    /// <summary>
    ///   Get a list of filtered sub-group IDs with hierarchy chain information
    ///   for the group within the live filter.
    /// </summary>
    /// <param name="filter">The filter to preview</param>
    /// <param name="groupID">Group ID</param>
    /// <param name="includeEmpty">Include groups with no series.</param>
    /// <returns></returns>
    [HttpPost("Preview/Group/{groupID}/FilteredIDs")]
    public ActionResult<List<FilteredGroupIDs>> GetPreviewFilteredSubGroupIDs(
        [FromBody] CreateOrUpdateFilterBody filter,
        [FromRoute, Range(1, int.MaxValue)] int groupID,
        [FromQuery] bool includeEmpty = true
    )
    {
        var filterToEvaluate = conversionService.ConvertToGenericFilter(filter);
        if (metadataService.GetShokoGroupByID(groupID) is not { } group)
            return NotFound(GroupNotFound);

        var user = User;
        if (!user.IsAllowedToSee(group))
            return Forbid();

        return MapResults(filteringService.GetFilteredSubGroups(
            filterToEvaluate,
            group,
            user,
            cancellationToken: HttpContext.RequestAborted
        ), includeEmpty);
    }

    private List<FilteredGroupIDs> MapResults(
        IReadOnlyList<FilteredGroupResult> results,
        bool includeEmpty
    )
    {
        return [.. results
            .Where(r => includeEmpty || r.Group.AllSeries.Any(ser => ser.Videos.Count > 0))
            .Select(r => new FilteredGroupIDs
            {
                GroupID = r.Group.ID,
                GroupIDChains = r.GroupIDChains,
                SeriesIDs = r.SeriesIDs,
            })
        ];
    }
}
