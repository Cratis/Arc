// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.Testing.Commands;

namespace Cratis.Arc.Testing.for_CommandResultShouldExtensions.given;

/// <summary>
/// Records what the assertions hand a policy, and can be told to reject.
/// </summary>
/// <remarks>
/// Discovery instantiates a policy once, so the recording lives on statics that each spec resets rather than on the
/// instance the discovery happened to build.
/// </remarks>
public class a_recording_policy : ICommandResultAssertionPolicy
{
    /// <summary>
    /// The message a rejecting policy throws with.
    /// </summary>
    public const string RejectionMessage = "The policy says no";

    /// <summary>
    /// Gets the assertions the policy was consulted for, in order.
    /// </summary>
    public static List<string> Consulted { get; } = [];

    /// <summary>
    /// Gets or sets the results the policy was handed.
    /// </summary>
    public static List<CommandResult> Received { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the policy rejects what it is shown.
    /// </summary>
    public static bool Rejects { get; set; }

    /// <summary>
    /// Forgets everything recorded, so one spec cannot see another's calls.
    /// </summary>
    public static void Reset()
    {
        Consulted.Clear();
        Received.Clear();
        Rejects = false;
    }

    /// <inheritdoc/>
    public void OnAssertionPassed(string assertion, CommandResult result)
    {
        Consulted.Add(assertion);
        Received.Add(result);

        if (Rejects)
        {
            throw new CommandResultAssertionException(RejectionMessage);
        }
    }
}
