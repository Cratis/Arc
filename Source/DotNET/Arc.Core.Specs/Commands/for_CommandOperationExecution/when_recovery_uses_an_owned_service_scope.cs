// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Traces;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_CommandOperationExecution;

public class when_recovery_uses_an_owned_service_scope : given.an_operation_pipeline
{
    ServiceProvider _root;
    List<string> _calls;

    void Establish()
    {
        _calls = [];
        _services.AddSingleton(_calls);
        _services.AddScoped<given.ScopedOperationProbe>();
        _root = _services.BuildServiceProvider();
        var traces = Substitute.For<IActivitySource<CommandPipeline>>();
        traces.ActualSource.Returns(_activitySource);
        _commandPipeline = new(
            _correlationIdAccessor,
            _commandFilters,
            _commandHandlerProviders,
            _commandResponseValueHandlers,
            _commandContextModifier,
            _commandContextValuesBuilder,
            _commandHandlerArgumentResolver,
            new KnownInstancesOf<ICommandExecutionScope>([]),
            _root.GetRequiredService<IServiceScopeFactory>(),
            traces);
    }

    async Task Because() => _result = await _commandPipeline.Execute(new given.ScopedProbeCommand());

    void Destroy() => _root.Dispose();

    [Fact] void should_keep_the_dependency_alive_until_recovery_finishes() =>
        _calls.ShouldEqual(["execute", "compensate", "dispose"]);

    [Fact] void should_report_observed_compensation_completion() =>
        _result.Recovery.Status.ShouldEqual(CommandRecoveryStatus.Completed);

    [Fact] void should_preserve_the_forward_failure() =>
        _result.ExceptionMessages.ShouldContain("The forward operation failed.");

    [Fact] void should_not_report_a_disposed_dependency_failure() =>
        _result.OperationOutcomes[0].CompensationFailure.ShouldBeNull();
}
