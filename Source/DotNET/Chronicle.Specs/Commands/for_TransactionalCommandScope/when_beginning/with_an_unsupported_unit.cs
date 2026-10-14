// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_beginning;

public class with_an_unsupported_unit : given.a_guarded_transactional_scope
{
    Commands.given.RecordingUnitOfWork _unsupported;
    void Establish()
    {
        _unsupported = new();
        _unitOfWorkManager.Begin(Arg.Any<Cratis.Execution.CorrelationId>()).Returns(_unsupported);
    }
    async Task Because()
    {
        _error = Catch.Exception(() => _scope.Begin(_context));
        await _scope.Complete(_context, CommandResult.Error(_correlationId, "begin failed"));
    }

    [Fact] void should_fail_closed_with_an_arc_exception() => _error.ShouldBeOfExactType<FreshIdentifierGuardRequiresEnrollmentContributors>();
    [Fact] void should_preserve_the_chronicle_cause() => _error.InnerException.ShouldBeOfExactType<Cratis.Chronicle.Transactions.UnitOfWorkEnrollmentContributorsNotSupported>();
    [Fact] void should_explain_the_required_support() => _error.Message.ShouldContain("guarded [GeneratedValue]");
    [Fact] void should_roll_back_after_begin_failed() => _unsupported.IsCompleted.ShouldBeTrue();
    [Fact] void should_clear_the_ambient_transaction() => CommandTransaction.TryGetActive(out _).ShouldBeFalse();
}
