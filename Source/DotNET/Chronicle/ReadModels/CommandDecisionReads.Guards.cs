// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle.ReadModels;
using Cratis.Chronicle.Transactions;

namespace Cratis.Arc.Chronicle.ReadModels;

internal sealed partial class CommandDecisionReads
{
    static void ValidateProtectedKey<T>(ReadModelKey key)
    {
        if (CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            ProtectedReadModelKeys.ValidateWireText(typeof(T), key.Value, key.Value);
        }
    }

    static bool IsProtectedRefusal(DecisionReadRefused exception, DecisionReadRefusalReason reason) =>
        CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On && exception.Reason == reason;

    static bool IsUnexpectedProtectedFailure(Exception ex) =>
        CommandDecisionPolicy.IsProtected
        && CommandDecisionPolicy.ReadProtection != ReadProtectionMode.On
        && ex is not (OperationCanceledException or DecisionReadCouldNotBeAcquired);

    static ReadMode ModeFor(bool validating)
    {
        if (CommandDecisionPolicy.Mode == CommandDecisionMode.Unprotected)
        {
            return ReadMode.Unprotected;
        }

        return validating ? ReadMode.Validation : ReadMode.Protected;
    }

    static void VerifyUnitOfWork(Type readModelType, IUnitOfWork? requestedUnit, IUnitOfWork? unitOfWork)
    {
        if (unitOfWork is { IsCompleted: true } && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On)
        {
            throw new ReadAfterProtectedCommit(readModelType.Name);
        }
        if (requestedUnit is not null && unitOfWork is not null && !ReferenceEquals(requestedUnit, unitOfWork))
        {
            throw new ReadProtectionUnitMismatch();
        }
    }

    async Task<DecisionRead<T>> ReadOutsideCommand<T>(ReadModelKey key, StreamScope? streamScope, IUnitOfWork? requestedUnit, CancellationToken cancellationToken)
        where T : class
    {
        return requestedUnit is null
            ? await ReadAmbient<T>(key, streamScope, cancellationToken)
            : await ReadEnrolled<T>(requestedUnit, key, streamScope, cancellationToken);
    }

    async Task<object> ReadForMode<T>(ReadMode mode, IUnitOfWork? unitOfWork, ReadModelKey key, StreamScope? streamScope, CancellationToken cancellationToken)
        where T : class
    {
        if (mode == ReadMode.Unprotected)
        {
            return await ReadAdvisory<T>(key, streamScope, cancellationToken);
        }

        return mode == ReadMode.Protected && CommandDecisionPolicy.ReadProtection == ReadProtectionMode.On
            ? await ReadEnrolled<T>(unitOfWork!, key, streamScope, cancellationToken)
            : await ReadDetached<T>(key, streamScope, cancellationToken);
    }
}
