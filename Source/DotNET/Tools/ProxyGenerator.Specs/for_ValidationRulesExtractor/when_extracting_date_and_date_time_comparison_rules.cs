// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_date_and_date_time_comparison_rules : Specification
{
    IEnumerable<PropertyValidationDescriptor> _result;

    void Because() => _result = ValidationRulesExtractor.ExtractValidationRules(typeof(DateOrdering).Assembly, typeof(DateOrdering));

    [Fact] void should_not_project_date_ordering_to_the_client() => _result.ShouldBeEmpty();

    public record DateOrdering(DateTime From, DateTime To, DateTimeOffset OffsetFrom, DateTimeOffset OffsetTo);

    public class DateOrderingValidator : AbstractValidator<DateOrdering>
    {
        public DateOrderingValidator()
        {
            RuleFor(value => value.From).GreaterThan(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            RuleFor(value => value.To).GreaterThan(value => value.From);
            RuleFor(value => value.OffsetFrom).LessThan(new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));
            RuleFor(value => value.OffsetTo).GreaterThanOrEqualTo(value => value.OffsetFrom);
        }
    }
}
