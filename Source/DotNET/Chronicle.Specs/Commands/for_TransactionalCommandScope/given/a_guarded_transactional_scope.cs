// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.given;

public class a_guarded_transactional_scope : a_transactional_command_scope
{
    protected IDisposable _registration;
    protected ICommandHandler _handler;
    protected Exception _error;

    void Establish()
    {
        _unitOfWork = Substitute.For<UnitOfWorkWithNamedTags>();
        _unitOfWorkManager.Begin(Arg.Any<Cratis.Execution.CorrelationId>()).Returns(_unitOfWork);
        _registration = Substitute.For<IDisposable>();
        _unitOfWork.AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>()).Returns(_registration);
        _handler = Substitute.For<ICommandHandler>();
        _handler.Parameters.Returns(typeof(Commands.given.a_generated_target.CreatesId).GetMethod("Handle")!.GetParameters());
        var providers = Substitute.For<ICommandHandlerProviders>();
        providers.TryGetHandlerFor(Arg.Any<object>(), out Arg.Any<ICommandHandler>()).Returns(call =>
        {
            call[1] = _handler;
            return true;
        });
        _serviceProvider.GetService(typeof(ICommandHandlerProviders)).Returns(providers);
    }

    void Destroy() => CommandTransaction.Current = null;
}
