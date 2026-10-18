// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.Filters;
using Cratis.Arc.Validation;
using Cratis.Execution;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes.given;

public class hidden_validated_properties : annotated_models
{
    protected bool _registered;
    protected Exception? _error;
    protected CommandResult _result;

    protected async Task CompileHiding(string parentProperty, string hiddenProperty)
    {
        var source = $$"""
            using System.ComponentModel.DataAnnotations;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            namespace Annotated;
            public record Text(string Value) : ConceptAs<string>(Value);
            public class Parent { {{parentProperty}} }
            [Command] public class Sensitive : Parent
            {
                {{hiddenProperty}}
                [Required] public Text Safe { get; } = new("safe");
                public void Handle() { }
            }
            """;
        _error = await Catch.Exception(async () =>
        {
            Compile(source);
            var type = _assembly.GetType("Annotated.Sensitive")!;
            _registered = DataAnnotationMembers.TryGet(type, out _);
            var command = Activator.CreateInstance(type)!;
            _result = await new DataAnnotationValidationFilter().OnExecution(new CommandContext(CorrelationId.New(), type, command, [], new()));
        });
    }
}
