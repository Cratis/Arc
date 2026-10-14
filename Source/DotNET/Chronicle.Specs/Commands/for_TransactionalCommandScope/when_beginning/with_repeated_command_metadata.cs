// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Commands.for_TransactionalCommandScope.when_beginning;

public class with_repeated_command_metadata : given.a_guarded_transactional_scope
{
    void Establish()
    {
        _context = _context with { Type = typeof(CreatesCachedId), Command = new CreatesCachedId() };
        _handler.Parameters.Returns(_context.Type.GetMethod("Handle")!.GetParameters());
    }
    async Task Because()
    {
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
        _context = _context with { Values = new() };
        _scope.Begin(_context);
        await _scope.Complete(_context, CommandResult.Success(_correlationId));
    }

    [Fact] void should_resolve_handler_metadata_at_most_once() => _serviceProvider.ReceivedCalls().Count(call => call.GetArguments().Contains(typeof(ICommandHandlerProviders))).ShouldBeLessThanOrEqual(1);
    [Fact] void should_still_register_each_invocation() => _unitOfWork.Received(2).AddEnrollmentContributor(Arg.Any<IUnitOfWorkEnrollmentContributor>());

    public class CreatesCachedId
    {
        public void Handle([GeneratedValue] Commands.given.a_generated_target.Identifier id) { }
    }
}
