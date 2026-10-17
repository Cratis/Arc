// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Execution;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.given;

public class a_data_annotation_validation_filter : Specification
{
    protected DataAnnotationValidationFilter _filter;
    protected CommandContext _context;
    protected CorrelationId _correlationId;

    void Establish()
    {
        _correlationId = CorrelationId.New();
        _filter = new DataAnnotationValidationFilter();
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RejectedObjectAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext) =>
            new("Object annotation rejected", ["Name"]);
    }

    public record WithObjectRules([property: Required, MinLength(2)] string? Name) : IValidatableObject
    {
        public int ValidationCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ValidationCalls++;
            return [new("Object rule rejected", ["Items[0].Name"])];
        }
    }

    [RejectedObject]
    public record WithTypeRules([property: Required] string? Name) : IValidatableObject
    {
        public int ValidationCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ValidationCalls++;
            return [new("Object rule rejected")];
        }
    }
}
