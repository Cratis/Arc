// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.Occurrences;

/// <summary>
/// Preserves append admission refusals for the occurrence executor instead of treating them as retryable failures.
/// </summary>
internal sealed class OccurrenceCommandExceptionMapper : ICommandExceptionMapper
{
    /// <inheritdoc/>
    public bool TryMap(CommandContext context, Exception exception, [NotNullWhen(true)] out CommandResult? result)
    {
        result = null;
        while (exception is TargetInvocationException && exception.InnerException is { } inner)
        {
            exception = inner;
        }
        if (exception is not ImmediateAppendRefused || OccurrenceExecution.Current is not { } execution)
        {
            return false;
        }

        execution.RecordRefusal(exception);
        result = CommandResult.Error(context.CorrelationId, exception);
        return true;
    }
}
