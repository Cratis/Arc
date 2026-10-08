// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Identifies the executable admission limits deliberately retained in generated authoring documents.
/// </summary>
/// <remarks>
/// Screenplay 4.80.0 reports malformed bindings under PLAY0268 too. Its diagnostics carry no structured reason,
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
                (command is not null && Reason(command, " reads") == " with legacy semantics that cannot imply decision consistency.");
        }

        if (diagnostic.Code != "PLAY0268")
        {
            return false;
        }

        if (authoringOnlyConstructs &&
            (message == "Operations and systems are not admitted by any supported executable model (ESM) version yet (#301)." ||
            message == "Event sources, streams and routes are not admitted by any supported executable model (ESM) version yet (#302)."))
        {
            return true;
        }

        var query = Reason(message, "Query");

        return Reason(message, "Command") == " handler requires a constrained implementation attachment." ||
            Reason(message, "Read model") == " must have one unambiguous keyed query to identify instances in the first ESM v1 vertical." ||
            Reason(message, "Concept") == " compliance attributes require portable data-subject semantics." ||
            query == " uses delivery, filtering, scope, or implementation behavior outside the first ESM v1 vertical." ||
            query == " must declare one caller-supplied 'by' argument in the first ESM v1 vertical." ||
            query == " must return one optional read model in the first ESM v1 vertical.";
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
