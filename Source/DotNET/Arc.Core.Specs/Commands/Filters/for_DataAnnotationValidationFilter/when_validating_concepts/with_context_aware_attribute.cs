// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.Arc.Validation;

namespace Cratis.Arc.Commands.Filters.for_DataAnnotationValidationFilter.when_validating_concepts;

public class with_context_aware_attribute : given.annotated_concepts
{
    Inspected _command;
    InspectAttribute _attribute;
    CommandResult _contextResult;

    void Establish()
    {
        _command = new(new Text("value"));
        _attribute = new();
        DataAnnotationMembers.Register(typeof(Inspected), [new("Name", "Label", instance => ((Inspected)instance).Name.Value, () => [_attribute])]);
        Command(_command);
    }

    async Task Because() => _contextResult = await _filter.OnExecution(_context);

    [Fact] void should_keep_the_command_as_the_object_instance() => _attribute.Context!.ObjectInstance.ShouldBeSame(_command);
    [Fact] void should_set_the_declared_member_name() => _attribute.Context!.MemberName.ShouldEqual("Name");
    [Fact] void should_set_the_display_name() => _attribute.Context!.DisplayName.ShouldEqual("Label");
    [Fact] void should_pass_the_unwrapped_value() => _attribute.Value.ShouldEqual("value");
    [Fact] void should_accept_the_command() => _contextResult.IsValid.ShouldBeTrue();

    public record Inspected([property: Inspect] Text Name);

    [AttributeUsage(AttributeTargets.Property)]
    public sealed class InspectAttribute : ValidationAttribute
    {
        public ValidationContext? Context { get; private set; }
        public object? Value { get; private set; }

        protected override System.ComponentModel.DataAnnotations.ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            Context = validationContext;
            Value = value;
            return null;
        }
    }
}
