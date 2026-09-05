// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_traversing;

/// <summary>
/// A single value carries no further model to validate, so the traversal must stop at it. It is asked whether it has
/// a validator of its own — a concept-style value legitimately can — but its internals are never walked.
/// <see cref="DateOnly"/> and <see cref="TimeOnly"/> are called out because a hand-maintained list of value types
/// previously omitted them, so the traversal reflected over their calendar components on every request.
/// </summary>
public class with_single_value_types : given.a_model_graph_validator
{
    void Because() => _validator.Validate(new ModelGraphValidationRequest(new ModelWithValues())).GetAwaiter().GetResult();

    [Fact] void should_not_descend_into_date_only() => DescendedInto(typeof(DayOfWeek)).ShouldBeFalse();
    [Fact] void should_not_descend_into_time_only() => DescendedInto(typeof(TimeSpan)).ShouldBeFalse();
    [Fact] void should_still_consider_the_value_types_themselves() => _typesAskedFor.ShouldContain(typeof(DateOnly));
    [Fact] void should_consider_time_only_itself() => _typesAskedFor.ShouldContain(typeof(TimeOnly));
    [Fact] void should_consider_guid_itself() => _typesAskedFor.ShouldContain(typeof(Guid));

    record ModelWithValues
    {
        public DateOnly Date { get; init; }
        public TimeOnly Time { get; init; }
        public Guid Id { get; init; }
        public decimal Amount { get; init; }
        public string Name { get; init; } = string.Empty;
    }
}
