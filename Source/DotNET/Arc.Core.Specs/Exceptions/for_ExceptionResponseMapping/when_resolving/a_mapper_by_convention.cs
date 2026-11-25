// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Execution;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Cratis.Arc.Exceptions.for_ExceptionResponseMapping.when_resolving;

[Collection("MutatesTypeDiscovery")]
public class a_mapper_by_convention : Specification
{
    CommandResult _result;
    IExceptionResponseMapper[] _mappers;

    void Because()
    {
        using var services = new ServiceCollection().AddCratisArcCore().BuildServiceProvider();
        using var scope = services.CreateScope();
        _mappers = scope.ServiceProvider.GetRequiredService<IInstancesOf<IExceptionResponseMapper>>().ToArray();
        _result = CommandResult.Error(CorrelationId.New(), new TheMappedFailure());
        ExceptionResponseMapping.Apply(_result, scope.ServiceProvider, NullLogger.Instance);
    }

    [Fact] void should_discover_the_application_mapper() => _mappers.Any(mapper => mapper is TheDiscoveredMapper).ShouldBeTrue();
    [Fact] void should_apply_the_discovered_mapping() => _result.ErrorCode.ShouldEqual("discovered_failure");
}
