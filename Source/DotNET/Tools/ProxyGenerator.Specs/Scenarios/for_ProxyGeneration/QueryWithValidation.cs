// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_ProxyGeneration;

public class QueryWithValidation
{
    public int MinAge { get; set; }
    public string SearchTerm { get; set; } = string.Empty;
}

public class QueryWithValidationValidator : BaseValidator<QueryWithValidation>
{
    public QueryWithValidationValidator()
    {
        RuleFor(x => x.MinAge).GreaterThanOrEqualTo(0).LessThanOrEqualTo(150);
        RuleFor(x => x.SearchTerm).MinimumLength(3);
    }
}
