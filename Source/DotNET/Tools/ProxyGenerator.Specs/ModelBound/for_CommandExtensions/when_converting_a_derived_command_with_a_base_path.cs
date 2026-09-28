// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.ProxyGenerator.ModelBound.for_CommandExtensions;

public class when_converting_a_derived_command_with_a_base_path : Specification
{
    CommandDescriptor _result;

    void Because()
    {
        var type = typeof(DerivedCommand).GetTypeInfo();
        _result = type.ToCommandDescriptor("/output", 5, false, "api", [type]);
    }

    [Fact] void should_use_the_conventional_route() => _result.Route.ShouldEqual("/api/derived-command");

    [Path("/base")]
    public record BaseCommand
    {
        public void Handle() { }
    }

    [Command]
    public record DerivedCommand : BaseCommand;
}
