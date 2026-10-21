// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Validation;
using Cratis.Concepts;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.given;

public class annotated_concepts : a_data_annotation_validation_filter
{
    protected void ConceptParameter<T>(string name, T value, Func<object, object?> read, params ValidationAttribute[] attributes)
        where T : notnull
    {
        EstablishPerformerWith(new QueryParameters([new QueryParameter(name, typeof(T))]), new QueryArguments { [name] = value });
        _context = _context with { Name = new FullyQualifiedQueryName(GetType().FullName!) };
        DataAnnotationMembers.RegisterQueryParameters(_context.Name, [new(name, "Label", read, () => attributes)]);
    }

    public record Text(string Value) : ConceptAs<string>(Value);
    public record Number(int Value) : ConceptAs<int>(Value);
}
