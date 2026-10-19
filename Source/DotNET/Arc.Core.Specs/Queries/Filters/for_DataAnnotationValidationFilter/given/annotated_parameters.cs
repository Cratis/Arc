// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Queries.ModelBound;
using Cratis.Arc.Validation;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Queries.Filters.for_DataAnnotationValidationFilter.given;

public class annotated_parameters : a_data_annotation_validation_filter
{
    protected void OmittedParameter(string methodName)
    {
        var method = GetType().GetMethod(methodName)!;
        var performer = new ModelBoundQueryPerformer(
            GetType(),
            GetType().FullName!,
            method,
            Substitute.For<IServiceProviderIsService>(),
            Substitute.For<IAuthorizationEvaluator>());
        EstablishPerformerWith(performer.Parameters, QueryArguments.Empty);
        _context = _context with { Name = performer.FullyQualifiedName };
        var parameter = method.GetParameters().Single();
        DataAnnotationMembers.RegisterQueryParameters(_context.Name,
            [new(parameter.Name!, parameter.Name!, instance => instance, () => parameter.GetCustomAttributes<ValidationAttribute>().ToArray())]);
    }

    protected void Parameter(Type type, object? value, ValidationAttribute attribute, bool supplied = true)
    {
        var arguments = supplied ? new QueryArguments { ["input"] = value! } : QueryArguments.Empty;
        EstablishPerformerWith(new QueryParameters([new QueryParameter("input", type, IsRequired: true)]), arguments);
        _context = _context with { Name = new FullyQualifiedQueryName(GetType().FullName!) };
        DataAnnotationMembers.RegisterQueryParameters(_context.Name, [new("input", "Label", instance => instance, () => [attribute])]);
    }
}
