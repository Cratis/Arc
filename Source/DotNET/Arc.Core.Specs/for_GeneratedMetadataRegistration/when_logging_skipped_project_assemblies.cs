// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.for_GeneratedMetadataRegistration;

public class when_logging_skipped_project_assemblies : given.a_generated_metadata_registration
{
    ILogger<GeneratedMetadataRegistration> _logger;

    void Establish()
    {
        _logger = Substitute.For<ILogger<GeneratedMetadataRegistration>>();
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
        _getDependencyContextProjectNames().Returns(["Missing.Project"]);
        _loadAssembly(Arg.Any<AssemblyName>()).Returns(_ => throw new FileNotFoundException("Not deployed", "Missing.Project"));
        _registration.EnsureRegistered();
    }

    void Because()
    {
        _registration.LogSkipped(_logger);
        _registration.LogSkipped(_logger);
    }

    [Fact] void should_log_the_skipped_assembly_once() => _logger.ReceivedCalls().Count(_ => _.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
    [Fact] void should_forget_it_once_logged() => _registration.Skipped.ShouldBeEmpty();
}
