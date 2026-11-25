// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Availability.Audiences;
using Cratis.Arc.Availability.Context;
using Cratis.Arc.Availability.Suspensions;

namespace Cratis.Arc.Availability.ManifestFile;

/// <summary>
/// Decides whether a part is available from a manifest file and a <see cref="ManifestContext"/>, with the same engine that decides in a running application.
/// </summary>
/// <remarks>
/// It reads only what the file holds, so a condition that is code in the application that wrote the file cannot be decided here.
/// The shared test vectors of the repository run through it.
/// </remarks>
public class ManifestFileEvaluator
{
    readonly ManifestFileDocument _document;
    readonly AvailabilityEvaluator _evaluator;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManifestFileEvaluator"/> class.
    /// </summary>
    /// <param name="document">The <see cref="ManifestFileDocument"/>.</param>
    public ManifestFileEvaluator(ManifestFileDocument document)
    {
        _document = document;
        var catalog = new AvailabilityCatalog();
        foreach (var block in document.Blocks)
        {
            catalog.Add(new AvailabilityDefinition(block.Name, block.Gate.ToExpression()) { Description = block.Description });
        }

        _evaluator = new AvailabilityEvaluator(catalog);
    }

    /// <summary>
    /// Decides whether a part is available.
    /// </summary>
    /// <param name="part">The full name of the part. A part the manifest does not know is available.</param>
    /// <param name="context">The <see cref="ManifestContext"/>.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="AvailabilityDecision"/>.</returns>
    public async ValueTask<AvailabilityDecision> Decide(string part, ManifestContext context, CancellationToken cancellationToken = default)
    {
        var entry = _document.Parts.FirstOrDefault(_ => _.Name == part);
        if (entry is null)
        {
            return AvailabilityDecision.Available;
        }

        // A suspension is decided first, before any gate.
        var path = PartPaths.For(entry.Module, entry.Feature, entry.Slice, entry.Name);
        if (context.Suspended.Any(suspended => PartPaths.Covers(suspended, path, entry.Name)))
        {
            return new AvailabilityDecision(AvailabilityVerdict.Suspended, [AvailabilityReason.Suspended]);
        }

        var held = new EffectiveAudiences(new HashSet<AudienceName>(context.Audiences.Select(_ => new AudienceName(_))), []);
        var resolver = new ContextValueCatalog().CreateResolver(ContextScope.Application, Ambient(context), _ => ValueTask.FromResult<EffectiveAudiences?>(held));
        var result = await _evaluator.EvaluatePath(entry.Gates.Select(_ => _.ToExpression()), resolver, cancellationToken);
        return result.Outcome switch
        {
            AvailabilityOutcome.Available => AvailabilityDecision.Available,
            AvailabilityOutcome.Unavailable => new AvailabilityDecision(AvailabilityVerdict.Unavailable, result.Causes),
            _ => new AvailabilityDecision(AvailabilityVerdict.Unknown, result.Causes)
        };
    }

    static Dictionary<string, object?> Ambient(ManifestContext context)
    {
        // The values come first so that nothing in them, and nothing in the arguments, can replace the tenant or the identity.
        var ambient = new Dictionary<string, object?>(context.Values)
        {
            ["tenant"] = context.Tenant,
            ["identity"] = new Dictionary<string, object?> { ["user"] = context.User, ["isSystem"] = false }
        };
        if (context.Arguments is not null)
        {
            ambient["arguments"] = context.Arguments;
        }

        return ambient;
    }
}
