// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands.for_CommandPipeline.when_refusing_an_unbound_command;

public class and_decision_support_is_missing : given.an_unbound_command
{
    async Task Because() => _result = await _commandPipeline.RefuseUnboundHosted(new UnboundCommandRefusalRequest(typeof(ProtectedInput), _bindingFailure, _serviceProvider, false), CancellationToken.None);

    [Fact] void should_fail_instead_of_reporting_binding_validation() => _result.HasExceptions.ShouldBeTrue();
    [Fact] void should_not_report_validation() => _result.ValidationResults.ShouldBeEmpty();
    [Fact] void should_not_evaluate_authorization() => _evaluator.DidNotReceive().IsAuthorized(Arg.Any<Type>());

    [Protected]
    public record ProtectedInput;

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ProtectedAttribute : Attribute, IProtectedDecisionAttribute;
}
