// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Exposes the current Arc operation's receipt time to validators and other scoped collaborators.
/// </summary>
public class OperationContextAccessor : IOperationContextAccessor
{
    /// <inheritdoc/>
    public DateTimeOffset? ReceivedAt => OperationContextScope.Current;
}
