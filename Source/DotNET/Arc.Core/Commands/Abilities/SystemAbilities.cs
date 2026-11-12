// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Commands.Abilities;

/// <summary>
/// Base for the collaborator a model's <c>system</c> becomes: one injectable type per system whose methods are its
/// abilities. A derived type names its system and calls <see cref="Invoke"/>; an operation takes the derived type's
/// interface as a parameter of Execute or Compensate, so a test substitutes it.
/// </summary>
/// <param name="invoker">The invoker that hands the invocation to the extension host.</param>
/// <param name="contextAccessor">The accessor for the command being executed.</param>
public abstract class SystemAbilities(IAbilityInvoker invoker, ICommandContextAccessor contextAccessor)
{
    /// <summary>
    /// Gets the system name as declared in the model.
    /// </summary>
    protected abstract string System { get; }

    /// <summary>
    /// Builds the idempotency key for one call.
    /// </summary>
    /// <param name="correlationId">The command's correlation identity.</param>
    /// <param name="scope">The running operation phase.</param>
    /// <param name="system">The system.</param>
    /// <param name="ability">The ability.</param>
    /// <param name="call">The call number within the phase.</param>
    /// <returns>A key that is the same for the same call and never includes an attempt.</returns>
    public static string IdempotencyKey(string correlationId, CommandOperationPhaseScope scope, string system, string ability, int call)
    {
        var identity = $"{correlationId}|{scope.Index}|{scope.Operation.FullName}|{scope.Phase}|{system}.{ability}|{call}";
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(identity))).ToLowerInvariant();
    }

    /// <summary>
    /// Runs an ability. Only valid while a command operation is executing or compensating.
    /// </summary>
    /// <param name="ability">The ability name.</param>
    /// <param name="inputs">The typed inputs by name.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result, when the ability returned or ended in a declared outcome.</returns>
    /// <exception cref="InvalidCommandOperation">Thrown when no command operation is running.</exception>
    /// <exception cref="AbilityInvocationFailed">Thrown when the ability failed, which fails the command.</exception>
    protected async Task<AbilityInvocationResult> Invoke(string ability, IReadOnlyDictionary<string, object?> inputs, CancellationToken cancellationToken)
    {
        var scope = CommandOperationPhaseScope.Current
            ?? throw new InvalidCommandOperation($"Ability '{System}.{ability}' can only be invoked from a command operation's Execute or Compensate method.");
        var context = contextAccessor.Current;
        var correlationId = context.CorrelationId.ToString();
        var invocation = new AbilityInvocation(
            System,
            ability,
            inputs,
            IdempotencyKey(correlationId, scope, System, ability, scope.NextCall()),
            new Dictionary<string, string>
            {
                ["command"] = context.Type.FullName ?? context.Type.Name,
                ["correlationId"] = correlationId,
                ["operation"] = scope.Operation.FullName ?? scope.Operation.Name,
                ["phase"] = scope.Phase.ToString().ToLowerInvariant()
            });

        var result = await invoker.Invoke(invocation, cancellationToken);
        if (result.Outcome == AbilityOutcomes.Failed)
        {
            throw new AbilityInvocationFailed(System, ability, result.Reason);
        }

        return result;
    }
}
