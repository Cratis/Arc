// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_completing;

public class with_a_guarded_nested_invocation : given.a_guarded_transactional_scope
{
    IDisposable _nestedRegistration;
    int _ownerDisposalsAfterNested;

    void Establish()
    {
        _nestedRegistration = Substitute.For<IDisposable>();
        _unitOfWork.AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>()).Returns(_registration, _nestedRegistration);
    }
    async Task Because()
    {
        _scope.Begin(_context);
        var nested = _context with { Values = new(), CorrelationId = CorrelationId.New() };
        _scope.Begin(nested);
        await _scope.Complete(nested, CommandResult.Success(nested.CorrelationId));
        _ownerDisposalsAfterNested = _registration.ReceivedCalls().Count();
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_register_each_invocation() => _unitOfWork.Received(2).AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());
    [Fact] void should_dispose_only_the_nested_handle_on_nested_completion() => _ownerDisposalsAfterNested.ShouldEqual(0);
    [Fact] void should_dispose_the_nested_handle_once() => _nestedRegistration.Received(1).Dispose();
    [Fact] void should_dispose_the_owner_handle_once() => _registration.Received(1).Dispose();
    [Fact] void should_commit_only_at_the_owner() => _unitOfWork.Received(1).Commit();
}
