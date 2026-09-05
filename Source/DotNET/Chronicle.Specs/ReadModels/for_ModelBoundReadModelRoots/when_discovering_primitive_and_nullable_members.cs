// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels.for_ModelBoundReadModelRoots;

public class when_discovering_primitive_and_nullable_members : Specification
{
    Type[] _candidates;
    IEnumerable<Type> _roots;

    void Establish() => _candidates =
    [
        typeof(Parent), typeof(int), typeof(bool), typeof(string), typeof(decimal), typeof(DateTime),
        typeof(DateTimeOffset), typeof(TimeSpan), typeof(Guid), typeof(DayOfWeek), typeof(int?), typeof(OptionalValue?)
    ];

    void Because() => _roots = ModelBoundReadModelRoots.Discover(_candidates).ToArray();

    [Fact] void should_not_remove_scalar_or_nullable_value_candidates_referenced_by_the_parent() => _roots.ShouldContainOnly(_candidates);

    record Parent(
        int Count,
        bool Enabled,
        string Name,
        decimal Amount,
        DateTime Date,
        DateTimeOffset Timestamp,
        TimeSpan Duration,
        Guid Id,
        DayOfWeek Day,
        int? OptionalCount,
        OptionalValue? Optional);

    record struct OptionalValue(int Value);
}
