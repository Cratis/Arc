// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.given;

public class annotated_parameters : a_data_annotation_validation_filter
{
    protected void Parameter(Type type, object? value, ValidationAttribute attribute, bool supplied = true)
    {
        var arguments = supplied ? new QueryArguments { ["input"] = value! } : QueryArguments.Empty;
        EstablishPerformerWith(new QueryParameters([new QueryParameter("input", type)]), arguments);
        _context = _context with { Name = new FullyQualifiedQueryName(GetType().FullName!) };
        DataAnnotationMembers.RegisterQueryParameters(_context.Name, [new("input", "Label", instance => instance, () => [attribute])]);
    }
}
