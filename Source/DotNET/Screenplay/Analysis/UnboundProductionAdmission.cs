// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Commands;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Analysis;

/// <summary>Leaves binding-invalid command productions in their C# handler.</summary>
/// <param name="emitter">The emitter to rebuild the admitted document with.</param>
/// <param name="verifier">The language verifier.</param>
internal sealed class UnboundProductionAdmission(IScreenplayEmitter emitter, IScreenplayVerifier verifier)
{
    /// <summary>Withholds only attributable productions, with a strictly decreasing command budget.</summary>
    /// <param name="model">The model after protected-read admission.</param>
    /// <param name="options">The resolved generation options.</param>
    /// <param name="candidate">The candidate emission.</param>
    /// <returns>The final emission and production-loss warnings.</returns>
    internal ProtectedReadAdmitted Admit(ApplicationModel model, ScreenplayOptions options, ScreenplayEmission candidate)
    {
        var current = model;
        var emission = candidate;
        var diagnostics = new ScreenplayDiagnostics();
        ScreenplayVerification? verified = null;
        var budget = model.Slices.Sum(slice => slice.Commands.Count(command => command.Produces.Any()));
        while (budget-- > 0)
        {
            verified = GeneratedCommandReads.Verify(verifier, emission);
            if (verified.Application is null || !verified.Compiles)
            {
                break;
            }

            var refused = verified.UnexpectedBindingErrors(options.AuthoringOnlyConstructs)
                .Select(diagnostic => (Diagnostic: diagnostic, Owner: EmittedCommandLocation.Find(verified.Application, diagnostic, current, options)))
                .Where(entry => CanWithhold(entry.Owner))
                .ToArray();
            if (refused.Length == 0)
            {
                break;
            }

            foreach (var refusal in refused)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.DocumentDidNotBind,
                    $"Command '{refusal.Owner!.Value.Command.Name}' productions were withheld and left in its C# handler because {refusal.Diagnostic.Code}: {refusal.Diagnostic.Message} (on line {refusal.Diagnostic.Location.Line}, column {refusal.Diagnostic.Location.Column})",
                    refusal.Owner.Value.Command.SourceFilePath);
            }

            var commands = refused.Select(entry => (entry.Owner!.Value.Slice.Namespace, entry.Owner.Value.Slice.Name, entry.Owner.Value.Command.Name)).ToHashSet();
            var pruned = current with
            {
                Slices = current.Slices.Select(slice => slice with
                {
                    Commands = slice.Commands.Select(command => commands.Contains((slice.Namespace, slice.Name, command.Name))
                        ? command with { Produces = [] } : command).ToList()
                }).ToList()
            };
            current = AuthoringDeclarations.RemoveOrphans(current, CommandProductionPruning.Complete(current, pruned, diagnostics));
            emission = emitter.Emit(current, options);
            verified = null;
        }

        return new(emission, diagnostics.All) { Model = current, Verification = verified };
    }

    static bool CanWithhold((SliceModel Slice, CommandModel Command, CommandSyntax Printed)? candidate) =>
        candidate is { } owner && HasProductions(owner.Command, owner.Printed) &&
        new ScreenplayNaming().ToFilePath(owner.Command.SourceFilePath) is not null;

    static bool HasProductions(CommandModel command, CommandSyntax printed) => command.Produces.Any() && printed.Produces.Any();
}
