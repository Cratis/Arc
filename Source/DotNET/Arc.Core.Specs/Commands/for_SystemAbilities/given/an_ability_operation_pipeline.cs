// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands.Abilities;
using Cratis.Arc.Commands.for_CommandOperationExecution.given;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands.for_SystemAbilities.given;

public class an_ability_operation_pipeline : an_operation_pipeline
{
    protected RecordingAbilityInvoker _invoker;

    void Establish()
    {
        _invoker = new();
        var contextAccessor = Substitute.For<ICommandContextAccessor>();
        contextAccessor.Current.Returns(new CommandContext(_correlationId, typeof(OperationCommand), new object(), [], new CommandContextValues()));
        _services.AddSingleton<IAbilityInvoker>(_invoker);
        _services.AddSingleton(contextAccessor);
        _services.AddSingleton<ILedger, LedgerAbilities>();
    }
}
