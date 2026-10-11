// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

extern alias FakeContracts;

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.for_MethodInfoExtensions.when_getting_a_response_model.given;

public class CounterfeitAttributeMarkerHandler :
    ICommandResponseValueHandler,
    FakeContracts::Cratis.Arc.Commands.ICommandResponseValueHandlerForAttributed<CounterfeitConsumedAttribute>,
    FakeContracts::Cratis.Arc.Commands.ICommandResponseValueHandlerForAttributedEnumerable<CounterfeitConsumedAttribute>
{
    public bool CanHandle(CommandContext commandContext, object value) => value is CounterfeitClaimedValue;

    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
