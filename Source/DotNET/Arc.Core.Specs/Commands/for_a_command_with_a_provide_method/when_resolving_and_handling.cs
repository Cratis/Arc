// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.ModelBound;
using Cratis.Execution;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_a_command_with_a_provide_method;

public class when_resolving_and_handling : given.a_loan_approval_command
{
    ICreditBureau _bureau;
    IServiceProvider _serviceProvider;
    ICommandHandler _handler;
    CommandHandlerArgumentResolver _resolver;
    object _result;

    void Establish()
    {
        _bureau = Substitute.For<ICreditBureau>();
        _bureau.GetScore("acme").Returns(800);
        _serviceProvider = new ServiceCollection().AddSingleton(_bureau).BuildServiceProvider();
        _resolver = new CommandHandlerArgumentResolver(new CommandProvideInvoker());
        _handler = new ModelBoundCommandHandler(typeof(ApproveLoan), typeof(ApproveLoan).GetMethod(nameof(ApproveLoan.Handle))!);
    }

    async Task Because()
    {
        var context = new CommandContext(CorrelationId.New(), typeof(ApproveLoan), new ApproveLoan("acme"), [], new());
        var resolution = await _resolver.Resolve(_handler, context, _serviceProvider, allowedSeverity: null);
        context = context with { Dependencies = resolution.Arguments };
        _result = await _handler.Handle(context);
    }

    [Fact] void should_fetch_the_score_through_provide() => _bureau.Received().GetScore("acme");
    [Fact] void should_produce_the_event_from_handle() => _result.ShouldBeOfExactType<LoanApproved>();
}
