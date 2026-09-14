// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Assertions over actual operation observations, never a simulated execution mode.
/// </summary>
public static class CommandOperationShouldExtensions
{
    /// <summary>
    /// Asserts that at least one operation of the given type entered and completed Execute.
    /// </summary>
    /// <typeparam name="TOperation">The declaration type.</typeparam>
    /// <param name="result">The actual pipeline result.</param>
    /// <exception cref="CommandResultAssertionException">No matching Execute returned.</exception>
    public static void ShouldHaveExecutedOperation<TOperation>(this CommandResult result)
        where TOperation : ICommandOperation
    {
        if (!result.OperationOutcomes.Any(outcome => outcome.OperationType == typeof(TOperation) && outcome.ExecutionCompleted))
        {
            throw new CommandResultAssertionException($"Expected a completed Execute for '{typeof(TOperation).Name}', but none was observed. Inspect OperationOutcomes for partial invocations.");
        }
    }

    /// <summary>
    /// Asserts that at least one compensator of the given type returned. This is not proof of atomic reversal.
    /// </summary>
    /// <typeparam name="TOperation">The declaration type.</typeparam>
    /// <param name="result">The actual pipeline result.</param>
    /// <exception cref="CommandResultAssertionException">No matching Compensate returned.</exception>
    public static void ShouldHaveCompensatedOperation<TOperation>(this CommandResult result)
        where TOperation : ICommandOperation
    {
        if (!result.OperationOutcomes.Any(outcome => outcome.OperationType == typeof(TOperation) && outcome.Compensation == CommandOperationCompensation.Completed))
        {
            throw new CommandResultAssertionException($"Expected a completed Compensate for '{typeof(TOperation).Name}', but none was observed.");
        }
    }

    /// <summary>
    /// Asserts that no operation entered Execute, including partial invocations that threw.
    /// </summary>
    /// <param name="result">The actual pipeline result.</param>
    /// <exception cref="CommandResultAssertionException">An operation entered Execute.</exception>
    public static void ShouldHaveNoOperationInvocations(this CommandResult result)
    {
        if (result.OperationOutcomes.Count != 0)
        {
            throw new CommandResultAssertionException($"Expected no operation invocations, but {result.OperationOutcomes.Count} entered Execute.");
        }
    }
}
