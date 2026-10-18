// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Validation;

namespace Cratis.Arc.Generators.Specs.for_DataAnnotationMemberTypes.given;

public class diagnostic_sensitive_members : annotated_models
{
    protected bool _registered;
    protected bool _normalRegistered;

    protected void CompileProperty(string property)
    {
        var source = $$"""
            using System;
            using System.ComponentModel.DataAnnotations;
            using System.Diagnostics.CodeAnalysis;
            using Cratis.Arc.Commands.ModelBound;
            using Cratis.Concepts;
            namespace Annotated;
            public record Text(string Value) : ConceptAs<string>(Value);
            [Command] public class Sensitive
            {
                {{property}}
                [Required] public Text Safe { get; } = new("safe");
                public void Handle() { }
            }
            [Command] public record Normal([property: Required] Text Name) { public void Handle() { } }
            """;
        Compile(source);
        _registered = DataAnnotationMembers.TryGet(_assembly.GetType("Annotated.Sensitive")!, out _);
        _normalRegistered = DataAnnotationMembers.TryGet(_assembly.GetType("Annotated.Normal")!, out _);
    }
}
