// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Files;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Reactions;

/// <summary>
/// Builds the Screenplay <c>reaction</c> declaration for a reactor.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// A reactor body is code, so the only faithful rendering is a file reference. A trigger with neither a file nor an
/// inline body has an empty body and does not compile, which is why a path is always resolved.
/// <para>
/// The Chronicle side keeps the name <em>reactor</em> and the Screenplay side is a <em>reaction</em>: what is read
/// here is a Chronicle reactor, and what is written is the language construct for it. A Chronicle reactor is set
/// off by an event and nothing else, so every trigger written is a named one.
/// </para>
/// </remarks>
public class ReactionSyntaxBuilder(IScreenplayNaming naming, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// Builds the reaction declaration.
    /// </summary>
    /// <param name="reactor">The reactor to build for.</param>
    /// <param name="namespace">The namespace of the slice the reactor lives in.</param>
    /// <returns>The <see cref="ReactionSyntax"/>, or <see langword="null"/> when it observes no events.</returns>
    public ReactionSyntax? Build(ReactorModel reactor, string @namespace)
    {
        var name = naming.ToDeclarationName(reactor.Name);
        var path = naming.ToFilePath(reactor.SourceFilePath) ?? SourceFilePaths.Conventional(@namespace, name);
        var file = new FileReferenceSyntax(path, SourceLocation.Start);

        var triggers = reactor.ObservedEvents
            .Select(naming.ToDeclarationName)
            .Where(_ => _.Length > 1)
            .Distinct(StringComparer.Ordinal)
            .Select(_ => new ReactionTriggerSyntax(new NamedTriggerSourceSyntax(_, SourceLocation.Start), [], file, null, SourceLocation.Start))
            .ToList();

        if (triggers.Count == 0)
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.ReactorWithoutEvents,
                $"The reactor '{reactor.Name}' observes no events and was left out",
                @namespace);

            return null;
        }

        return new(name, triggers, SourceLocation.Start);
    }
}
