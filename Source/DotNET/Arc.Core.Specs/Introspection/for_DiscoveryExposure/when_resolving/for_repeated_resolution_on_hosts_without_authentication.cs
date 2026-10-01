// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Introspection.for_DiscoveryExposure.when_resolving;

public class for_repeated_resolution_on_hosts_without_authentication : given.a_host
{
    IServiceProvider _firstHost;
    IServiceProvider _secondHost;
    ILogger _firstLogger;
    ILogger _secondLogger;

    void Establish()
    {
        var environment = Substitute.For<IHostEnvironment>();
        environment.EnvironmentName.Returns(Environments.Production);
        _firstHost = Substitute.For<IServiceProvider>();
        _secondHost = Substitute.For<IServiceProvider>();
        _firstHost.GetService(typeof(IHostEnvironment)).Returns(environment);
        _secondHost.GetService(typeof(IHostEnvironment)).Returns(environment);
        _firstLogger = Substitute.For<ILogger>();
        _secondLogger = Substitute.For<ILogger>();
        _firstLogger.IsEnabled(LogLevel.Warning).Returns(true);
        _secondLogger.IsEnabled(LogLevel.Warning).Returns(true);
    }

    void Because()
    {
        DiscoveryExposure.Resolve(_mapperThatCannotAuthenticate, new IntrospectionOptions(), _firstHost, _firstLogger);
        DiscoveryExposure.Resolve(_mapperThatCannotAuthenticate, new IntrospectionOptions(), _firstHost, _firstLogger);
        DiscoveryExposure.Resolve(_mapperThatCannotAuthenticate, new IntrospectionOptions(), _secondHost, _secondLogger);
    }

    [Fact] void should_report_the_first_hosts_problem_once() => _firstLogger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
    [Fact] void should_also_report_the_second_hosts_problem() => _secondLogger.ReceivedCalls().Count(call => call.GetMethodInfo().Name == nameof(ILogger.Log)).ShouldEqual(1);
}
