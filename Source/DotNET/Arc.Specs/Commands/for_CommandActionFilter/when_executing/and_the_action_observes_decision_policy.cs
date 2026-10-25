// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandActionFilter.when_executing;

/// <summary>
/// Controller actions do not establish a pipeline decision policy, even across an asynchronous continuation.
/// </summary>
public class and_the_action_observes_decision_policy : given.a_post_request_with_execution_scopes
{
    readonly List<(bool Active, CommandDecisionMode Mode, ReadProtectionMode Protection)> _policies = [];

    /// <summary>
    /// Executes the action and observes its policy before and after yielding.
    /// </summary>
    /// <returns>The asynchronous action execution.</returns>
    Task Because() => _filter.OnActionExecutionAsync(_actionContext, async () =>
    {
        _policies.Add((CommandDecisionPolicy.IsActive, CommandDecisionPolicy.Mode, CommandDecisionPolicy.ReadProtection));
        await Task.Yield();
        _policies.Add((CommandDecisionPolicy.IsActive, CommandDecisionPolicy.Mode, CommandDecisionPolicy.ReadProtection));
        return _executedContext;
    });

    /// <summary>
    /// The action retains the non-pipeline defaults throughout execution.
    /// </summary>
    [Fact] void should_keep_decision_policy_inactive() => _policies.ShouldEqual([(false, CommandDecisionMode.Legacy, ReadProtectionMode.Off), (false, CommandDecisionMode.Legacy, ReadProtectionMode.Off)]);

    /// <summary>
    /// The action still completes successfully.
    /// </summary>
    [Fact] void should_return_success() => ResultingCommandResult.IsSuccess.ShouldBeTrue();
}
