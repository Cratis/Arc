// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.Analysis.Specifications;

/// <summary>
/// Compares complete recovered scenarios, substituting only symbolic value positions.
/// </summary>
internal static class SpecificationCaseComparison
{
    /// <summary>
    /// Compares scenarios independently of their individual names.
    /// </summary>
    /// <param name="expected">The symbolic or concrete expected model.</param>
    /// <param name="actual">The row model.</param>
    /// <param name="values">The normalized row values replacing parameters.</param>
    /// <returns>Whether every step, outcome, caller and fixture agrees.</returns>
    internal static bool Matches(SpecificationModel expected, SpecificationModel actual, IReadOnlyDictionary<string, MappingSourceModel>? values = null) =>
        States(expected.Given, actual.Given, values) && State(expected.When, actual.When, values) &&
        States(expected.Then, actual.Then, values) && expected.Errors.SequenceEqual(actual.Errors, StringComparer.Ordinal) &&
        expected.Reactor == actual.Reactor && expected.AssertsResponse == actual.AssertsResponse &&
        Returns(expected.Returns, actual.Returns, values) && Mappings(expected.GeneratedValues, actual.GeneratedValues, values) &&
        expected.GenerationFailure == actual.GenerationFailure && expected.CallerFailure == actual.CallerFailure &&
        Caller(expected.Caller, actual.Caller);

    static bool States(IEnumerable<SpecificationStateModel> expected, IEnumerable<SpecificationStateModel> actual, IReadOnlyDictionary<string, MappingSourceModel>? values)
    {
        var left = expected.ToList();
        var right = actual.ToList();

        return left.Count == right.Count && left.Zip(right).All(pair => State(pair.First, pair.Second, values));
    }

    static bool State(SpecificationStateModel? expected, SpecificationStateModel? actual, IReadOnlyDictionary<string, MappingSourceModel>? values) =>
        expected is null || actual is null
            ? expected is null && actual is null
            : expected.Name == actual.Name && expected.Kind == actual.Kind && expected.For == actual.For &&
                Route(expected.Route, actual.Route) && Mappings(expected.Values, actual.Values, values);

    static bool Route(SpecificationEventRouteModel? expected, SpecificationEventRouteModel? actual) =>
        expected is null || actual is null
            ? expected is null && actual is null
            : expected.Source == actual.Source && expected.Stream == actual.Stream && expected.StreamId == actual.StreamId &&
                expected.Identity == actual.Identity && expected.StreamIdParts.SequenceEqual(actual.StreamIdParts);

    static bool Returns(SpecificationReturnModel? expected, SpecificationReturnModel? actual, IReadOnlyDictionary<string, MappingSourceModel>? values) =>
        expected is null || actual is null
            ? expected is null && actual is null
            : expected.ResponseType == actual.ResponseType && Source(expected.Value, values) == actual.Value && Mappings(expected.Fields, actual.Fields, values);

    static bool Caller(SpecificationCallerModel? expected, SpecificationCallerModel? actual) =>
        expected is null || actual is null
            ? expected is null && actual is null
            : expected.Authenticated == actual.Authenticated && expected.Roles.SequenceEqual(actual.Roles, StringComparer.Ordinal) &&
                expected.Claims.SequenceEqual(actual.Claims);

    static bool Mappings(IEnumerable<PropertyMappingModel> expected, IEnumerable<PropertyMappingModel> actual, IReadOnlyDictionary<string, MappingSourceModel>? values) =>
        expected.Select(mapping => mapping with { Source = Source(mapping.Source, values)! }).SequenceEqual(actual);

    static MappingSourceModel? Source(MappingSourceModel? source, IReadOnlyDictionary<string, MappingSourceModel>? values) =>
        source is PropertyPathSource path && path.Path.StartsWith("case.", StringComparison.Ordinal) && values is not null
            ? values.GetValueOrDefault(path.Path[5..])
            : source;
}
