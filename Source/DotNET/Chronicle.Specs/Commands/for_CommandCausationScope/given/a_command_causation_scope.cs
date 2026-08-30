// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.Auditing;
using Cratis.Execution;

namespace Cratis.Arc.Chronicle.Commands.for_CommandCausationScope.given;

public class a_command_causation_scope : Specification
{
    protected CommandCausationScope _scope;
    protected CausationManager _causationManager;
    protected IServiceProvider _serviceProvider;

    void Establish()
    {
        _causationManager = new();
        _serviceProvider = Substitute.For<IServiceProvider>();
        _serviceProvider.GetService(typeof(ICausationManager)).Returns(_causationManager);
        _scope = new();
    }

    protected CommandContext ContextFor<TCommand>() =>
        new(CorrelationId.New(), typeof(TCommand), new object(), [], new(), ServiceProvider: _serviceProvider);

    protected sealed record ApproveExpenseReport;

    protected sealed record SubmitExpenseReport;
}
