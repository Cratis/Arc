// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Claims;
using Cratis.Arc.Commands;
using Cratis.Execution;
using NSubstitute.Extensions;

namespace Cratis.Arc.Chronicle.Reactors.for_CommandSideEffectExecutor.when_executing;

public class with_a_real_caller_without_system_execution : given.a_real_system_executor
{
    List<ClaimsPrincipal> _actors = [];

    void Establish() => _commandPipeline.Configure().Execute(_command, _serviceProvider).Returns(_ =>
    {
        _actors.Add(_principal.Current!);
        return CommandResult.Success(CorrelationId.New());
    });

    async Task Because()
    {
        using var callerScope = _principal.BeginScope(_original);
        await _executor.Execute([_command]);
        await _executor.Execute([_command], typeof(TestReactor));
        _after = _principal.Current!;
    }

    [Fact] void should_preserve_the_caller_during_both_non_system_overloads() => _actors.SequenceEqual([_original, _original]).ShouldBeTrue();
    [Fact] void should_keep_the_caller_after_execution() => ReferenceEquals(_after, _original).ShouldBeTrue();
}
