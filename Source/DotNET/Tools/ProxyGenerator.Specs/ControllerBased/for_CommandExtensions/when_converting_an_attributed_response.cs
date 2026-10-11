// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;
using Cratis.Arc.ProxyGenerator.Templates;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.ProxyGenerator.ControllerBased.for_CommandExtensions;

public class when_converting_an_attributed_response : Specification
{
    CommandDescriptor _descriptor;
    string _proxy;

    void Because()
    {
        _descriptor = typeof(Projects).GetMethod(nameof(Projects.Register))!.ToCommandDescriptor("/output", 0);
        _proxy = InMemoryProxyGenerator.GenerateCommand(_descriptor);
    }

    [Fact] void should_preserve_the_controller_response() => _descriptor.HasResponse.ShouldBeTrue();
    [Fact] void should_preserve_the_attributed_response_type() => _descriptor.ResponseType.Type.ShouldEqual(typeof(ProjectRegistered));
    [Fact] void should_generate_a_typed_controller_response() => _proxy.ShouldContain("extends Command<IRegister, ProjectRegistered>");
    [Fact] void should_generate_the_controller_response_constructor() => _proxy.ShouldContain("super(ProjectRegistered, false)");

    [Route("api/projects")]
    public class Projects : ControllerBase
    {
        [HttpPost("register")]
        public ProjectRegistered Register() => new("Project");
    }
}
