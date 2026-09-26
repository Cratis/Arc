// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Validation;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_command_with_invalid_blocking_severity : Specification
{
    [Command]
    [BlockOnValidationSeverity((ValidationResultSeverity)99)]
    public record InvalidCommand
    {
        public void Handle() { }
    }

    Exception _exception;

    void Because() => _exception = Catch.Exception(() =>
    {
        var type = typeof(InvalidCommand).GetTypeInfo();
        _ = type.ToCommandDescriptor("/output", 5, false, "api", [type]);
    });

    [Fact] void should_reject_unsupported_severity() => _exception.ShouldBeOfExactType<InvalidCommandValidationSeverity>();
    [Fact] void should_identify_the_invalid_command() => _exception.Message.ShouldContain(nameof(InvalidCommand));
    [Fact] void should_identify_the_invalid_value() => _exception.Message.ShouldContain("99");
}
