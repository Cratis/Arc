// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Slices;
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
/// The compiler's named, typed arguments identify the command and read of PLAY0781 to PLAY0785, independently of
/// their wording or location. Earlier syntax errors on the read itself are attributed by their source line.
/// </para>
/// </remarks>
internal sealed class ProtectedReadAdmission(IScreenplayEmitter emitter, IScreenplayVerifier verifier)
{
    const int MaximumRounds = 2;

    static readonly string[] _defectCodes = ["PLAY0784", "PLAY0785"];
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
            ReportUnmapped(diagnostics, refusals);
            if (refusals.Mapped.Count == 0)
            {
                return new(current, diagnostics.All);
            }

            withheld = Withhold(withheld, refusals.Mapped, diagnostics);
            current = emitter.Emit(withheld, options);
        }

        if (current.Application.CommandReadMode is not null)
        {
            ReportRemaining(diagnostics, Refusals(current.Source));
        }

        return new(current, diagnostics.All);
    }

    static void ReportUnmapped(ScreenplayDiagnostics diagnostics, AttributedRefusals refusals)
    {
        if (refusals.Unmapped.Count > 0)
        {
            ReportDefect(diagnostics, refusals.Unmapped.Select(diagnostic => diagnostic.Code), "could not be attributed to a read");
        }
    }

    static void ReportRemaining(ScreenplayDiagnostics diagnostics, AttributedRefusals remaining)
    {
        var codes = remaining.Mapped.Select(refusal => refusal.Diagnostic.Code).Concat(remaining.Unmapped.Select(diagnostic => diagnostic.Code)).ToArray();
        if (codes.Length > 0)
        {
            ReportDefect(diagnostics, codes, "remain after the refused reads were withheld");
        }
    }

    static void ReportDefect(ScreenplayDiagnostics diagnostics, IEnumerable<string> codes, string what) => diagnostics.Error(
        ScreenplayDiagnosticCodes.DocumentDidNotBind,
        $"Protected read refusals ({string.Join(", ", codes.Distinct(StringComparer.Ordinal))}) {what}. That is the generator being wrong rather than anything the source declared",
        null);

    static IEnumerable<(SliceSyntax Slice, CommandSyntax Command)> Commands(FeatureSyntax feature) =>
        feature.Slices.SelectMany(slice => slice.Commands.Select(command => (slice, command))).Concat(feature.Features.SelectMany(Commands));

    static bool IsDefect(string code) => _defectCodes.Contains(code);

    static void Report(RefusedRead refused, WithholdingPass pass)
    {
        var (command, read, reasons, location) = refused;

        // A refused read no longer resolves its paths, so PLAY0784 and PLAY0785 follow from any other refusal; alone they mean the generator wrote a bad path.
        var primary = reasons.Where(reason => !IsDefect(reason.Code)).ToArray();
        var text = string.Join("; ", (primary.Length > 0 ? primary : reasons).Select(reason => $"{reason.Code}: {ProtectedReadDiagnostic.Message(reason)}"));
        if (primary.Length == 0)
        {
            pass.Diagnostics.Error(
                ScreenplayDiagnosticCodes.DocumentDidNotBind,
                $"The generator wrote a read path for protected read '{read.Alias}' of command '{command.Name}' that the Screenplay compiler refused, which is the generator being wrong rather than anything the source declared - {text}",
                location);
            return;
        }

        pass.Diagnostics.Information(
            ScreenplayDiagnosticCodes.UnprotectableCommandRead,
            $"Protected read '{read.Alias}' of command '{command.Name}' was refused by the Screenplay compiler and left in code, never emitted as an unguarded read - {text}",
            location);
    }

    AttributedRefusals Refusals(string source)
    {
        var verification = verifier.Verify(source);
        var commands = verification.Application?.Modules.SelectMany(module => module.Features).SelectMany(Commands).OrderBy(placed => placed.Command.Location.Line).ToArray() ?? [];
        var readLines = commands.SelectMany(placed => placed.Command.Reads ?? []).Select(read => read.Location.Line).ToHashSet();

        // A refusal the compiler states before binding (an unusable key, for instance) is an error on the line of the read itself.
        var refused = verification.BindingDiagnostics.Concat(verification.Errors)
            .Where(diagnostic => _refusalCodes.Contains(diagnostic.Code) || (diagnostic.Severity == DiagnosticSeverity.Error && readLines.Contains(diagnostic.Location.Line)))
            .Select(diagnostic => (Diagnostic: diagnostic, Reads: (ProtectedReadDiagnostic.HasSubjects(diagnostic) ? ProtectedReadDiagnostic.Attribute(verification.Application, diagnostic) : AttributeToRead(commands, diagnostic)).ToArray()))
            .ToArray();

        return new(
            [.. refused.SelectMany(entry => entry.Reads)],
            [.. refused.Where(entry => entry.Reads.Length == 0 && _refusalCodes.Contains(entry.Diagnostic.Code)).Select(entry => entry.Diagnostic)]);
    }

    IEnumerable<ProtectedReadRefusal> AttributeToRead((SliceSyntax Slice, CommandSyntax Command)[] commands, Diagnostic diagnostic) =>
        commands.LastOrDefault(placed => placed.Command.Location.Line <= diagnostic.Location.Line) is { Command: { } owner } placed
            ? (owner.Reads ?? []).Where(read => read.Location.Line == diagnostic.Location.Line).Select(read => new ProtectedReadRefusal(placed.Slice.Name, owner.Name, read.Alias ?? read.ReadModel, diagnostic))
            : [];

    ApplicationModel Withhold(ApplicationModel model, IReadOnlyList<ProtectedReadRefusal> refusals, ScreenplayDiagnostics diagnostics)
    {
        var pass = new WithholdingPass(model, refusals, diagnostics);

        return model with
        {
            Slices = model.Slices.Select(slice => slice with
            {
                Commands = slice.Commands.Select(command => Withhold(command, slice.Namespace, SliceSyntaxBuilder.NameOf(slice, _naming), pass)).ToList()
            }).ToList()
        };
    }

    CommandModel Withhold(CommandModel command, string location, string slice, WithholdingPass pass)
    {
        var concerned = pass.Refusals.Where(refusal => refusal.Slice == slice && refusal.Command == _naming.ToDeclarationName(command.Name)).ToArray();
        var reads = command.Authoring?.Reads.Where(read => read.Protected && concerned.Any(refusal => refusal.Alias == EmittedName(read))).ToArray() ?? [];
        foreach (var read in reads)
        {
            Report(new(command, read, concerned.Where(refusal => refusal.Alias == EmittedName(read)).Select(refusal => refusal.Diagnostic).ToArray(), $"{location}.{command.Name}"), pass);
        }

        return reads.Length == 0
            ? command
            : AuthoringDeclarations.WithoutReads(command, pass.Model, reads.Select(read => read.Alias).ToHashSet(StringComparer.Ordinal), pass.Diagnostics, location, $"Command '{command.Name}': refused protected reads and the requirements and mappings depending on them were left in code");
    }

    string EmittedName(CommandReadModel read) => read.NamedByView ? _naming.ToDeclarationName(read.Name) : _naming.ToPropertyName(read.Alias);

    sealed record WithholdingPass(ApplicationModel Model, IReadOnlyList<ProtectedReadRefusal> Refusals, ScreenplayDiagnostics Diagnostics);

    sealed record RefusedRead(CommandModel Command, CommandReadModel Read, Diagnostic[] Reasons, string Location);

    sealed record AttributedRefusals(IReadOnlyList<ProtectedReadRefusal> Mapped, IReadOnlyList<Diagnostic> Unmapped);
}
