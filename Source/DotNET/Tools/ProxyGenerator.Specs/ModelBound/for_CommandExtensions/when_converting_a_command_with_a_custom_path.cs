// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_command_with_a_custom_path : Specification
{
    CommandDescriptor _result;
    string _code;

    void Because()
    {
        var command = typeof(StableCommand).GetTypeInfo();
        _result = command.ToCommandDescriptor("/output", 0, true, "api", [command, typeof(AnotherCommand).GetTypeInfo()]);
        _code = InMemoryProxyGenerator.GenerateCommand(_result);
    }

    [Fact] void should_use_the_declared_route_even_with_a_namespace_collision() => _result.Route.ShouldEqual("/Stable/Command");
    [Fact] void should_emit_the_declared_route_in_the_proxy() => _code.ShouldContain("readonly route: string = '/Stable/Command'");

    [Path("/Stable/Command")]
    public record StableCommand
    {
        public void Handle() { }
    }

    public record AnotherCommand
    {
        public void Handle() { }
    }
}
