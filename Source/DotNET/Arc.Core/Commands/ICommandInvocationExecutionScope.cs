// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines an execution scope with resources that must survive completion of every command scope.
/// </summary>
public interface ICommandInvocationExecutionScope : ICommandExecutionScope
{
    /// <summary>
    /// Releases invocation resources in the pipeline's finally, including when beginning or completing a scope fails.
    /// </summary>
    /// <param name="context">The invocation being exited.</param>
    void End(CommandContext context);
}
