// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Declares only attributed reference-element enumerables as consumed by the fixture integration.
/// </summary>
public class AttributedEnumerableValueHandler :
    ICommandResponseValueHandler,
    ICommandResponseValueHandlerForAttributedEnumerable<ConsumedEventAttribute>,
    ICommandResponseValueHandlerForAttributedEnumerable<EnumerableOnlyAttribute>
{
    /// <inheritdoc/>
    public bool CanHandle(CommandContext commandContext, object value) =>
        value is IEnumerable<ProjectRegistered> or IEnumerable<EnumerableOnlyValue>;

    /// <inheritdoc/>
    public Task<CommandResult> Handle(CommandContext commandContext, object value) =>
        Task.FromResult(CommandResult.Success(commandContext.CorrelationId));
}
