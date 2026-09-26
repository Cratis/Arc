// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_command_with_blocking_severity : Specification
{
    [Command]
    [BlockOnValidationSeverity(ValidationResultSeverity.Information)]
    public record DeclaredCommand
    {
        public void Handle() { }
    }

    CommandDescriptor _result;
    string _generatedCode;

    void Because()
    {
        _result = typeof(DeclaredCommand).GetTypeInfo().ToCommandDescriptor(
            "/output", 5, false, "api", [typeof(DeclaredCommand).GetTypeInfo()]);
        _generatedCode = InMemoryProxyGenerator.GenerateCommand(_result);
    }

    [Fact] void should_emit_the_inclusive_information_severity() => _result.BlockOnValidationSeverity.ShouldEqual((int)ValidationResultSeverity.Information);
    [Fact] void should_mark_the_policy_as_present() => _result.HasBlockingValidationSeverity.ShouldBeTrue();
    [Fact] void should_emit_the_policy_in_generated_typescript() => _generatedCode.ShouldContain("readonly blockOnValidationSeverity = 1;");
}
