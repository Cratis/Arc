// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.EventSequences;
using Cratis.Chronicle.Transactions;
using Cratis.Execution;
using Cratis.Monads;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.when_a_response_value_handler_appends.given;

public class a_command_with_a_response_value_handler : Specification
{
    protected CausationManager _causationManager;
    protected IEventLog _eventLog;
    protected IUnitOfWork _unitOfWork;
    protected CommandContext _context;
    protected CommandResult _result;
    protected PayInvoice _command;
    protected IImmutableList<Causation> _chainAtCompletion;
    protected IImmutableList<Causation> _chainAtCommit;

    void Establish()
    {
        var correlationId = CorrelationId.New();
        _causationManager = new();
        _command = new("invoice-42");

        _eventLog = Substitute.For<IEventLog>();
        _eventLog.CompleteStream("completion", "period").Returns(_ =>
        {
            _chainAtCompletion = _causationManager.GetCurrentChain();
            Result<EventSequenceNumber, CompleteStreamError> completed = EventSequenceNumber.First;
            return Task.FromResult(completed);
        });

        _unitOfWork = Substitute.For<IUnitOfWork>();
        _unitOfWork.Commit().Returns(_ =>
        {
            _chainAtCommit = _causationManager.GetCurrentChain();
            return Task.CompletedTask;
        });

        var unitOfWorkManager = Substitute.For<IUnitOfWorkManager>();
        unitOfWorkManager.HasCurrent.Returns(false);
        unitOfWorkManager.Begin(Arg.Any<CorrelationId>()).Returns(_unitOfWork);

        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(ICausationManager)).Returns(_causationManager);
        serviceProvider.GetService(typeof(IUnitOfWorkManager)).Returns(unitOfWorkManager);

        _context = new(correlationId, typeof(PayInvoice), _command, [], new(), ServiceProvider: serviceProvider);
        _result = CommandResult.Success(correlationId);
    }

    /// <summary>
    /// Executes a command the way the pipeline does: scopes begin in list order, the response value is handled, and the scopes complete in reverse order.
    /// </summary>
    /// <param name="scopes">The execution scopes, in list order.</param>
    /// <param name="handle">Handles the response value while the scopes are begun.</param>
    /// <returns>A task representing the execution.</returns>
    protected async Task Execute(ICommandExecutionScope[] scopes, Func<Task> handle)
    {
        foreach (var scope in scopes)
        {
            scope.Begin(_context);
        }

        await handle();

        for (var index = scopes.Length - 1; index >= 0; index--)
        {
            await scopes[index].Complete(_context, _result);
        }
    }

    protected static Causation CommandLink(IImmutableList<Causation> chain) => chain.Single(_ => _.Type == CommandCausation.Type);

    protected sealed record PayInvoice(string InvoiceNumber);
}
