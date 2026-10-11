// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Declares attributed values as consumed by the fixture integration.
/// </summary>
public class AttributedValueHandler :
    ICommandResponseValueHandler,
    ICommandResponseValueHandlerForAttributed<ConsumedEventAttribute>,
    ICommandResponseValueHandlerForAttributed<ScalarOnlyAttribute>,
    ICommandResponseValueHandlerForAttributed<NonInheritedConsumedEventAttribute>,
    ICommandResponseValueHandlerForAttributed<DefaultInheritedConsumedEventAttribute>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is ProjectRegistered or ScalarOnlyValue or NonInheritedEvent or DefaultInheritedEvent;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
