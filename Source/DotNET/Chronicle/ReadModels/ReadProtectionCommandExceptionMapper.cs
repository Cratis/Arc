// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Adds command context to Chronicle's typed read-protection failures without inferring missing event identities.
/// </summary>
public class ReadProtectionCommandExceptionMapper : ICommandExceptionMapper
{
    /// <inheritdoc/>
    public bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result)
    {
        result = null;
        if (CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On)
        {
            return false;
        }

        while (exception is TargetInvocationException or DecisionReadCouldNotBeAcquired && exception.InnerException is { } inner)
        {
            exception = inner;
        }

        if (exception is not (ReadRefusedWhileReadsAreProtected or DetachedReadWhileReadsAreProtected or
            ImmediateAppendWhileReadsAreProtected or ReadAfterProtectedCommit or ExplicitScopeDoesNotCoverReads or
            DeclaredRoutingNotCoveredByExplicitScope or ReadOfSourceWithStagedEvents))
        {
            return false;
        }

        // Chronicle already supplies the actionable operation/contribution text, but no structured diagnostics.
        // Only substitute its generic subject; never extract an operation, event type or position from the prose.
        var message = exception.Message.Replace("The command", context.Type.Name, StringComparison.Ordinal);
        result = CommandResult.Error(context.CorrelationId, exception);
        result.ExceptionMessages = [message];
        return true;
    }
}
