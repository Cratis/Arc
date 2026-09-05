// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Arc.Commands;

namespace Cratis.Arc.Testing.Commands;

/// <summary>
/// Holds the discovered <see cref="ICommandResultAssertionPolicy"/> implementations and applies them.
/// </summary>
public static class CommandResultAssertionPolicies
{
    static readonly Lazy<IReadOnlyList<ICommandResultAssertionPolicy>> _policies = new(Discover, isThreadSafe: true);

    /// <summary>
    /// Gets the discovered policies.
    /// </summary>
    public static IReadOnlyList<ICommandResultAssertionPolicy> All => _policies.Value;

    /// <summary>
    /// Applies every discovered policy to an assertion that has just passed.
    /// </summary>
    /// <param name="result">The <see cref="CommandResult"/> that was asserted.</param>
    /// <param name="assertion">The assertion that passed. Supplied by the compiler; do not pass it explicitly.</param>
    /// <remarks>
    /// The assertion name comes from <see cref="CallerMemberNameAttribute"/> rather than from a parameter or an enum,
    /// so it cannot drift from the method it names and adding an assertion needs no second edit. Those names are
    /// matched as strings elsewhere in the product, which is a further reason not to mint a parallel vocabulary for
    /// them here.
    /// </remarks>
    public static void Apply(CommandResult result, [CallerMemberName] string assertion = "")
    {
        // Enumerated by index rather than with foreach: this runs on the pass path of every assertion in every spec,
        // and the common case is that a repository has installed none.
        var policies = _policies.Value;
        for (var index = 0; index < policies.Count; index++)
        {
            policies[index].OnAssertionPassed(assertion, result);
        }
    }

    static IReadOnlyList<ICommandResultAssertionPolicy> Discover() =>
    [
        .. Cratis.Types.Types.Instance.FindMultiple<ICommandResultAssertionPolicy>()
            .Select(policyType => Activator.CreateInstance(policyType) as ICommandResultAssertionPolicy
                ?? throw new InvalidOperationException(
                    $"Failed to create an instance of command result assertion policy '{policyType.FullName}'. " +
                    "Ensure it has a public parameterless constructor."))
    ];
}
