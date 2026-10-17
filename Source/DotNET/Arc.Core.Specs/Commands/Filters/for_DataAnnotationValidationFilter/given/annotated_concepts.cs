// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.Validation;
using Cratis.Concepts;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.given;

public class annotated_concepts : a_data_annotation_validation_filter
{
    protected CommandResult _result;

    void Establish()
    {
        Register<RequiredText>(nameof(RequiredText.Name), command => ((RequiredText)command).Name?.Value);
        Register<RangedNumber>(nameof(RangedNumber.Count), command => ((RangedNumber)command).Count.Value);
        Register<SizedText>(nameof(SizedText.Name), command => ((SizedText)command).Name.Value);
        Register<PatternedText>(nameof(PatternedText.Name), command => ((PatternedText)command).Name.Value);
        Register<OptionalText>(nameof(OptionalText.Name), command => ((OptionalText)command).Name?.Value);
        Register<MinimumText>(nameof(MinimumText.Name), command => ((MinimumText)command).Name.Value);
        Register<MaximumText>(nameof(MaximumText.Name), command => ((MaximumText)command).Name.Value);
        Register<PlainText>(nameof(PlainText.Name), command => ((PlainText)command).Name);
        Register<ObjectRule>(nameof(ObjectRule.Name), command => ((ObjectRule)command).Name.Value);
        Register<TypeRule>(nameof(TypeRule.Name), command => ((TypeRule)command).Name.Value);
    }

    protected void Command<T>(T command)
        where T : notnull => _context = new(_correlationId, typeof(T), command, [], new());

    static void Register<T>(string name, Func<object, object?> read) => DataAnnotationMembers.Register(typeof(T),
        [new(name, "Label", read, () => typeof(T).GetProperty(name)!.GetCustomAttributes<ValidationAttribute>(true).ToArray())]);

    public record Text(string Value) : ConceptAs<string>(Value);
    public record Number(int Value) : ConceptAs<int>(Value);
    public record RequiredText([property: Required, Display(Name = "Label")] Text? Name);
    public record RangedNumber([property: Range(1, 5)] Number Count);
    public record SizedText([property: StringLength(3)] Text Name);
    public record PatternedText([property: RegularExpression("^[A-Z]+$")] Text Name);
    public record OptionalText([property: StringLength(3)] Text? Name);
    public record MinimumText([property: MinLength(3)] Text Name);
    public record MaximumText([property: MaxLength(3)] Text Name);
    public record PlainText([property: Required, StringLength(3), Display(Name = "Label")] string Name);
    public record LegacyPlainText([property: Required, StringLength(3), Display(Name = "Label")] string Name);

    public record ObjectRule([property: Required] Text Name) : IValidatableObject
    {
        public IEnumerable<System.ComponentModel.DataAnnotations.ValidationResult> Validate(ValidationContext validationContext) =>
            [new("Object rejected")];
    }

    [Reject]
    public record TypeRule([property: Required] Text Name);

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RejectAttribute : ValidationAttribute
    {
        protected override System.ComponentModel.DataAnnotations.ValidationResult IsValid(object? value, ValidationContext validationContext) => new("Type rejected");
    }
}
