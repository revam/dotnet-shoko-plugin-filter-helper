using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Shoko.Abstractions.Filtering;
using Shoko.Abstractions.Filtering.Expressions;
using Shoko.Abstractions.Filtering.Expressions.Containers;
using Shoko.Abstractions.Filtering.Generic;
using Shoko.Abstractions.Filtering.Sorting;
using Shoko.Plugin.FilterHelper.API.Models;

namespace Shoko.Plugin.FilterHelper.Services;

/// <summary>
/// Converts between the plugin's API DTOs (<see cref="FilterCondition"/>,
/// <see cref="SortingCriteria"/>, <see cref="CreateOrUpdateFilterBody"/>)
/// and Shoko's internal filter abstractions (<see cref="FilterExpression{T}"/>,
/// <see cref="SortingExpression"/>, <see cref="GenericFilter"/>).
///
/// Adapted from the StreamLined plugin.
/// </summary>
public sealed class FilterConversionService
{
    private readonly Dictionary<string, Type> _expressionTypes;
    private readonly Dictionary<string, Type> _sortingTypes;

    /// <summary>
    /// Initializes the service by scanning loaded assemblies for filter
    /// expression and sorting types.
    /// </summary>
    private static string TrimSuffix(string name, params string[] suffixes)
    {
        foreach (var suffix in suffixes)
        {
            name = name.EndsWith(suffix, StringComparison.Ordinal)
                ? name[..^suffix.Length]
                : name;
        }

        return name;
    }

    public FilterConversionService()
    {
        _expressionTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(a =>
                a != typeof(FilterExpression) &&
                !a.IsAbstract &&
                !a.IsGenericType &&
                typeof(FilterExpression).IsAssignableFrom(a) &&
                !typeof(SortingExpression).IsAssignableFrom(a)
            )
            .Distinct()
            .ToDictionary<Type, string>(
                a => TrimSuffix(a.Name, "Expression", "Function", "Selector"),
                StringComparer.OrdinalIgnoreCase
            );

        _sortingTypes = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(a =>
                a != typeof(FilterExpression) &&
                !a.IsAbstract &&
                !a.IsGenericType &&
                typeof(SortingExpression).IsAssignableFrom(a)
            )
            .Distinct()
            .ToDictionary<Type, string>(
                a => TrimSuffix(a.Name, "SortingSelector"),
                StringComparer.OrdinalIgnoreCase
            );
    }

    /// <summary>
    /// Convert a <see cref="CreateOrUpdateFilterBody"/> to a
    /// <see cref="GenericFilter"/> for use with Shoko's filtering engine.
    /// </summary>
    public GenericFilter ConvertToGenericFilter(CreateOrUpdateFilterBody body)
    {
        return new GenericFilter
        {
            ApplyAtSeriesLevel = body.ApplyAtSeriesLevel,
            Expression = body.IsDirectory ? null : GetExpressionTree<bool>(body.Expression),
            SortingExpression = body.IsDirectory ? null : GetSortingCriteria(body.Sorting),
        };
    }

    /// <summary>
    /// Convert a <see cref="FilterCondition"/> DTO tree to a
    /// <see cref="FilterExpression{T}"/>.
    /// </summary>
    public FilterExpression<T>? GetExpressionTree<T>(FilterCondition? condition)
    {
        if (condition is null)
            return null;

        if (!_expressionTypes.TryGetValue(condition.Type.Trim(), out var type))
            throw new ArgumentException(
                $"FilterCondition type '{condition.Type}' was not found. "
                + "Ensure the required Shoko assemblies are loaded."
            );

        var result = (FilterExpression<T>)Activator.CreateInstance(type)!;

        // Left / First
        switch (result)
        {
            case IWithExpressionParameter left:
                left.Left = GetExpressionTree<bool>(condition.Left);
                break;
            case IWithDateSelectorParameter left:
                left.Left = GetExpressionTree<DateTime?>(condition.Left);
                break;
            case IWithNumberSelectorParameter left:
                left.Left = GetExpressionTree<double>(condition.Left);
                break;
            case IWithStringSelectorParameter left:
                left.Left = GetExpressionTree<string>(condition.Left);
                break;
            case IWithStringSetSelectorParameter left:
                left.Left = GetExpressionTree<IReadOnlySet<string>>(condition.Left);
                break;
        }

        // Parameters
        switch (result)
        {
            case IWithBoolParameter parameter:
                parameter.Parameter = condition.Parameter?.ToLower() is "true";
                break;
            case IWithStringParameter parameter:
                parameter.Parameter = condition.Parameter;
                break;
            case IWithNumberParameter parameter:
                parameter.Parameter = string.IsNullOrEmpty(condition.Parameter)
                    ? default
                    : double.Parse(condition.Parameter!, CultureInfo.InvariantCulture);
                break;
            case IWithDateParameter parameter:
                parameter.Parameter = string.IsNullOrEmpty(condition.Parameter)
                    ? default
                    : DateTime.ParseExact(
                        condition.Parameter!,
                        "yyyy-MM-dd",
                        CultureInfo.InvariantCulture.DateTimeFormat
                    );
                break;
            case IWithTimeSpanParameter parameter:
                parameter.Parameter = string.IsNullOrEmpty(condition.Parameter)
                    ? default
                    : TimeSpan.ParseExact(
                        condition.Parameter!,
                        "G",
                        CultureInfo.InvariantCulture.DateTimeFormat
                    );
                break;
            case IWithStringSetParameter parameter:
                parameter.Parameter = condition.Parameter is null
                    ? []
                    : condition.Parameter.Split("|||").ToHashSet();
                break;
        }

        // Right / Second
        switch (result)
        {
            case IWithSecondExpressionParameter right:
                right.Right = GetExpressionTree<bool>(condition.Right);
                break;
            case IWithSecondDateSelectorParameter right:
                right.Right = GetExpressionTree<DateTime?>(condition.Right);
                break;
            case IWithSecondStringSelectorParameter right:
                right.Right = GetExpressionTree<string>(condition.Right);
                break;
            case IWithSecondNumberSelectorParameter right:
                right.Right = GetExpressionTree<double>(condition.Right);
                break;
        }

        // Second Parameter
        switch (result)
        {
            case IWithSecondStringParameter right:
                right.SecondParameter = condition.SecondParameter ?? string.Empty;
                break;
        }

        return result;
    }

    /// <summary>
    /// Convert a <see cref="SortingCriteria"/> DTO chain to a
    /// <see cref="SortingExpression"/>.
    /// </summary>
    public SortingExpression? GetSortingCriteria(SortingCriteria? criteria)
    {
        if (criteria is null)
            return null;

        if (!_sortingTypes.TryGetValue(criteria.Type, out var type))
            throw new ArgumentException(
                $"SortingExpression type '{criteria.Type}' was not found. "
                + "Ensure the required Shoko assemblies are loaded."
            );

        var result = (SortingExpression)Activator.CreateInstance(type)!;
        result.Descending = criteria.IsInverted;

        if (result is IWithStringParameter withParam)
            withParam.Parameter = criteria.Parameter;

        if (criteria.Next is not null)
            result.Next = GetSortingCriteria(criteria.Next);

        return result;
    }
}
