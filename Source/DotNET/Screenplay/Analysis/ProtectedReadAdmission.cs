// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>
/// Decides which protected command reads Screenplay admits, by asking the Screenplay compiler rather than restating its rules.
/// </summary>
/// <param name="emitter">The emitter that prints a model.</param>
/// <param name="verifier">The verifier that binds a printed document with the pinned compiler.</param>
/// <remarks>
/// The generator decides what only it can see: that a read is protected, its key, and whether its absence is rejected.
/// Whether Screenplay admits the read - the shape of its view, its key type, concurrency - is Screenplay's decision, so
/// no rule of it is mirrored here. The candidate reads are emitted under the <c>command reads protected</c> preamble and
/// bound; every PLAY0780 to PLAY0785 diagnostic is mapped to the read it concerns, which is withheld together with the
/// requirements and mappings depending on it, and the model is emitted again. A refusal of one read never affects
/// another. The compiler states some refusals before it binds, which hides the ones it states while binding, so the
/// withholding is repeated once; a document still refused after that is a defect of the generator and is reported as one.
/// <para>
/// This is the one place that knows how a refusal is attributed. A diagnostic is attributed to a read by the line of the
/// parsed document it points at. Screenplay will carry the read, the view and the reason as typed diagnostic arguments
/// (Cratis/Screenplay#851); the location mapping in <see cref="AttributeToRead"/> is then replaced by reading them.
/// </para>
/// </remarks>
internal sealed class ProtectedReadAdmission(IScreenplayEmitter emitter, IScreenplayVerifier verifier)
{
    const int MaximumRounds = 2;

    static readonly string[] _refusalCodes = ["PLAY0780", "PLAY0781", "PLAY0782", "PLAY0783", "PLAY0784", "PLAY0785"];
    readonly ScreenplayNaming _naming = new();

    /// <summary>
    /// Admits the protected reads of an emission, withholding those the compiler refuses.
    /// </summary>
    /// <param name="model">The analyzed application.</param>
    /// <param name="options">The options the emission was made with.</param>
    /// <param name="emission">The emission of the candidate reads.</param>
    /// <returns>The emission to use, with the refusals and any defect reported.</returns>
    internal ProtectedReadAdmitted Admit(ApplicationModel model, ScreenplayOptions options, ScreenplayEmission emission)
    {
        if (emission.Application.CommandReadMode is null)
        {
            return new(emission, []);
        }

        var diagnostics = new ScreenplayDiagnostics();
        var current = emission;
        var withheld = model;
        for (var round = 0; round < MaximumRounds && current.Application.CommandReadMode is not null; round++)
        {
            var refusals = Refusals(current.Source);
            if (refusals.Count == 0)
            {
                return new(current, diagnostics.All);
            }

            withheld = Withhold(withheld, refusals, diagnostics);
            current = emitter.Emit(withheld, options);
        }

        if (current.Application.CommandReadMode is not null && Refusals(current.Source) is { Count: > 0 } remaining)
        {
            diagnostics.Error(
                ScreenplayDiagnosticCodes.DocumentDidNotBind,
                $"The generated document still has protected read refusals after the refused reads were withheld ({string.Join(", ", remaining.Select(refusal => refusal.Diagnostic.Code))}). That is the generator being wrong rather than anything the source declared",
                null);
        }

        return new(current, diagnostics.All);
    }

    static IEnumerable<CommandSyntax> Commands(FeatureSyntax feature) => feature.Slices.SelectMany(slice => slice.Commands).Concat(feature.Features.SelectMany(Commands));

    static string? PathAlias(string message)
    {
        var start = message.IndexOf('\'', StringComparison.Ordinal);
        var end = start < 0 ? -1 : message.IndexOf('\'', start + 1);

        return end > start ? message[(start + 1)..end].Split('.')[0] : null;
    }

    static IEnumerable<ReadsSyntax> Concerned(CommandSyntax command, Diagnostic diagnostic) => diagnostic.Code switch
    {
        "PLAY0783" => command.Reads ?? [],
        "PLAY0784" or "PLAY0785" => (command.Reads ?? []).Where(read => (read.Alias ?? read.ReadModel) == PathAlias(diagnostic.Message)),
        _ => (command.Reads ?? []).Where(read => read.Location.Line == diagnostic.Location.Line)
    };

    List<ProtectedReadRefusal> Refusals(string source)
    {
        var verification = verifier.Verify(source);
        var commands = verification.Application?.Modules.SelectMany(module => module.Features).SelectMany(Commands).OrderBy(command => command.Location.Line).ToArray() ?? [];
        var readLines = commands.SelectMany(command => command.Reads ?? []).Select(read => read.Location.Line).ToHashSet();

        // A refusal the compiler states before binding (an unusable key, for instance) is an error on the line of the read itself.
        return [.. verification.BindingDiagnostics.Concat(verification.Errors)
            .Where(diagnostic => _refusalCodes.Contains(diagnostic.Code) || (diagnostic.Severity == DiagnosticSeverity.Error && readLines.Contains(diagnostic.Location.Line)))
            .SelectMany(diagnostic => AttributeToRead(commands, diagnostic))];
    }

    IEnumerable<ProtectedReadRefusal> AttributeToRead(CommandSyntax[] commands, Diagnostic diagnostic) =>
        commands.LastOrDefault(command => command.Location.Line <= diagnostic.Location.Line) is { } owner
            ? Concerned(owner, diagnostic).Select(read => new ProtectedReadRefusal(owner.Name, read.Alias ?? read.ReadModel, diagnostic))
            : [];

    ApplicationModel Withhold(ApplicationModel model, List<ProtectedReadRefusal> refusals, ScreenplayDiagnostics diagnostics)
    {
        var pass = new WithholdingPass(model, refusals, diagnostics);

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => Withhold(command, slice.Namespace, pass)).ToList()
            }).ToList()
        };
    }

    CommandModel Withhold(CommandModel command, string location, WithholdingPass pass)
    {
        var concerned = pass.Refusals.Where(refusal => refusal.Command == _naming.ToDeclarationName(command.Name)).ToArray();
        var reads = command.Authoring?.Reads.Where(read => read.Protected && concerned.Any(refusal => refusal.Alias == _naming.ToPropertyName(read.Alias))).ToArray() ?? [];
        foreach (var read in reads)
        {
            var reasons = concerned.Where(refusal => refusal.Alias == _naming.ToPropertyName(read.Alias)).Select(refusal => $"{refusal.Diagnostic.Code}: {refusal.Diagnostic.Message}").Distinct(StringComparer.Ordinal);
            pass.Diagnostics.Information(
                ScreenplayDiagnosticCodes.UnprotectableCommandRead,
                $"Protected read '{read.Alias}' of command '{command.Name}' was refused by the Screenplay compiler and left in code, never emitted as an unguarded read - {string.Join("; ", reasons)}",
                $"{location}.{command.Name}");
        }

        return reads.Length == 0
            ? command
            : AuthoringDeclarations.WithoutReads(command, pass.Model, reads.Select(read => read.Alias).ToHashSet(StringComparer.Ordinal), pass.Diagnostics, location, $"Command '{command.Name}': refused protected reads and the requirements and mappings depending on them were left in code");
    }

    sealed record WithholdingPass(ApplicationModel Model, List<ProtectedReadRefusal> Refusals, ScreenplayDiagnostics Diagnostics);
}
