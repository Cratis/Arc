// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Defines a house policy applied on top of the built-in <see cref="CommandResult"/> assertions.
/// </summary>
/// <remarks>
/// <para>
/// Implementations are discovered automatically using the <see cref="Types.ITypes"/> type discovery system, the same
/// way <see cref="ICommandScenarioExtender"/> is. Any class in any loaded assembly that implements this interface and
/// has a public parameterless constructor is instantiated once and consulted by every assertion, so a repository
/// installs its policy in one visible place rather than at each call site.
/// </para>
/// <para>
/// A policy can only ever <em>strengthen</em> an assertion. It is consulted after the built-in check has already
/// passed, and never when the built-in check failed - a passing assertion may be turned into a failure, a failing one
/// can never be turned into a pass. That asymmetry is deliberate: the package's own guarantees are not negotiable by
/// a consumer, and a spec that has already failed has nothing useful left to say.
/// </para>
/// <para>
/// The point of a policy is a rule about what a <em>good</em> assertion of some kind looks like in a given
/// repository, which is a judgement the package should not make. A rejection spec that passes for a rule other than
/// the one it is named after, for instance, still passes <see cref="CommandResultShouldExtensions.ShouldHaveValidationErrors"/> -
/// it is rejected, just not for the reason the spec claims - and only the repository knows whether it minds.
/// </para>
/// </remarks>
public interface ICommandResultAssertionPolicy
{
    /// <summary>
    /// Called after a built-in assertion has passed.
    /// </summary>
    /// <param name="assertion">The name of the assertion method that passed, e.g. <c>ShouldHaveValidationErrors</c>.</param>
    /// <param name="result">The <see cref="CommandResult"/> that was asserted.</param>
    /// <remarks>
    /// Throw to fail the assertion. Return to let it stand. A policy that only cares about one assertion should
    /// return early for the others rather than assume which one it is being called for.
    /// </remarks>
    void OnAssertionPassed(string assertion, CommandResult result);
}
