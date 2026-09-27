// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_derived_command_with_blocking_severity : Specification
{
    [BlockOnValidationSeverity(ValidationResultSeverity.Warning)]
    public record PolicyBase
    {
        public void Handle() { }
    }

    [Command]
    public record DerivedCommand : PolicyBase;

    CommandDescriptor _result;
    string _generatedCode;

    void Because()
    {
        var type = typeof(DerivedCommand).GetTypeInfo();
        _result = type.ToCommandDescriptor("/output", 5, false, "api", [type]);
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_result);
    }

    [Fact] void should_inherit_the_warning_policy() => _result.BlockOnValidationSeverity.ShouldEqual((int)ValidationResultSeverity.Warning);
    [Fact] void should_emit_inherited_policy() => _generatedCode.ShouldContain("readonly blockOnValidationSeverity = 2;");
    [Fact] void should_treat_warnings_as_errors() => _generatedCode.ShouldContain("readonly treatWarningsAsErrors: boolean = true;");
}
