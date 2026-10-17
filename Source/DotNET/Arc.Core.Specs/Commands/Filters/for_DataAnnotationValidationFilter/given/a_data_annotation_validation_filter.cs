// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Concepts;
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

    public record Name(string Value) : ConceptAs<string>(Value);
    public record Amount(int Value) : ConceptAs<int>(Value);
    public record WithRequiredName([property: Required] Name? Name);
    public record WithAmount([property: Range(1, 100)] Amount Amount);

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RejectedObjectAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object? value, ValidationContext validationContext) =>
            new("Object annotation rejected", ["Name"]);
    }

    public record WithObjectRules([property: Required, MinLength(2)] Name? Name) : IValidatableObject
    {
        public int ValidationCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ValidationCalls++;
            return [new("Object rule rejected", ["Items[0].Name"])];
        }
    }

    [RejectedObject]
    public record WithTypeRules([property: Required] Name? Name) : IValidatableObject
    {
        public int ValidationCalls { get; private set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            ValidationCalls++;
            return [new("Object rule rejected")];
        }
    }

    public record WithMixedProperties(
        Name Name,
        [property: Required(ErrorMessage = "Label is required"), MinLength(2)] string Label);

    [RejectedObject]
    public record AnnotatedValue(string Value);

    [RejectedObject]
    public record AnnotatedName(string Value) : ConceptAs<string>(Value);

    public record WithAnnotatedProperty(Name Name, AnnotatedValue Value);
    public record WithAnnotatedConcept(AnnotatedName Name);

    public record WithComparedConcepts(
        Name Original,
        [property: Compare(nameof(WithComparedConcepts.Original), ErrorMessage = "Names must match")] Name Confirmation);

    public record WithComparedStrings(
        Name Name,
        string Original,
        [property: Compare(nameof(WithComparedStrings.Original), ErrorMessage = "Labels must match")] string Confirmation);

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class SameAsAttribute(string otherProperty) : CompareAttribute(otherProperty);

    public record WithDerivedComparison(
        Name Original,
        [property: SameAs(nameof(WithDerivedComparison.Original))] Name Confirmation);

    public record WithRequiredComparison(
        Name Original,
        [property: Required(ErrorMessage = "Confirmation is required"), Compare(nameof(WithRequiredComparison.Original))] Name? Confirmation);
}
