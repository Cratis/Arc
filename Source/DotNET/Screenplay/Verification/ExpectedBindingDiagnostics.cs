// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Identifies the executable admission limits deliberately retained in generated authoring documents.
/// </summary>
/// <remarks>
/// Screenplay 4.124.1 reports malformed bindings under PLAY0268 too. Its diagnostics carry no structured reason,
/// so these exact message shapes follow SemanticModelBinder.Commands, ReadModels, Concepts, and CommandProductions at that
/// version. A changed or unknown message fails closed as SP0056 rather than silently acquiring an exemption.
/// </remarks>
public static class ExpectedBindingDiagnostics
{
    /// <summary>
    /// Determines whether a binding diagnostic describes a known admission limit rather than malformed output.
    /// </summary>
    /// <param name="diagnostic">The diagnostic from the language's semantic binder.</param>
    /// <param name="authoringOnlyConstructs">Whether additional authoring-only constructs were requested.</param>
    /// <returns>True only for the documented admission and preserved legacy-consistency messages.</returns>
    public static bool IsExpected(Diagnostic diagnostic, bool authoringOnlyConstructs)
    {
        var message = diagnostic.Message;
        if (diagnostic.Code == "PLAY0271")
        {
            var command = Reason(message, "Command");

            return command == " concurrency metadata keeps its legacy meaning and cannot bind to ESM v1." ||
                (authoringOnlyConstructs && command is not null && Reason(command, " reads") == " with legacy semantics that cannot imply decision consistency.");
        }

        if (diagnostic.Code != "PLAY0268")
        {
            return false;
        }

        if (authoringOnlyConstructs &&
            (message == "Operations and systems are not admitted by any supported executable model (ESM) version yet (#301)." ||
            Reason(Reason(message, "Stream id mapping") ?? string.Empty, " on command") == " reads a property path; only direct command inputs are admitted by event routes."))
        {
            return true;
        }

        var query = Reason(message, "Query");

        // Screenplay#624: same-named queries in different slices hit a binder limit. Remove this tolerance when its fix ships.
        return Reason(message, "Query reference") == " is ambiguous across slices in the current ESM v1 binder." ||
            Reason(message, "Command") == " handler requires a constrained implementation attachment." ||
            Reason(message, "Read model") == " must have one unambiguous keyed query or one conventional '*Id' property to identify instances in the admitted ESM query shapes." ||
            Reason(message, "Concept") == " compliance attributes require portable data-subject semantics." ||
            query == " uses filtering, scope, or implementation behavior outside the admitted ESM query shapes." ||
            query == " must use caller-supplied 'by' arguments in the admitted ESM query shapes." ||
            query == " without a 'by' argument must return a read-model collection in the admitted ESM query shapes." ||
            query == " must return an optional read model or a read-model collection in the admitted ESM query shapes.";
    }

    /// <summary>
    /// Gets the exact reason following a quoted declaration name, excluding all other message forms.
    /// </summary>
    /// <param name="message">The binder message.</param>
    /// <param name="kind">The declaration kind preceding the name.</param>
    /// <returns>The reason, or null when the message does not name that kind of declaration.</returns>
    static string? Reason(string message, string kind)
    {
        var prefix = $"{kind} '";
        if (!message.StartsWith(prefix, StringComparison.Ordinal))
        {
            return null;
        }

        var end = message.IndexOf('\'', prefix.Length);

        return end > prefix.Length ? message[(end + 1)..] : null;
    }
}
