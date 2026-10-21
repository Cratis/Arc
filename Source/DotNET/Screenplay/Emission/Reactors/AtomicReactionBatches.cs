// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Reactors;

/// <summary>
/// Selects atomic mode only for multi-production batches actually retained in the document.
/// </summary>
internal static class AtomicReactionBatches
{
    /// <summary>
    /// Determines whether the emitted application contains a direct or refusal batch of multiple events.
    /// </summary>
    /// <param name="modules">The emitted modules.</param>
    /// <returns>True when atomic mode is needed.</returns>
    internal static bool Required(IEnumerable<ModuleSyntax> modules) => modules.SelectMany(module => Triggers(module.Features))
        .Any(trigger => (trigger.Produces ?? []).Count() > 1 || (trigger.Invokes ?? []).SelectMany(invocation => invocation.OnRefused).Any(branch => branch.Produces.Count() > 1));

    static IEnumerable<ReactionTriggerSyntax> Triggers(IEnumerable<FeatureSyntax> features) => features.SelectMany(feature =>
        feature.Slices.SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers).Concat(Triggers(feature.Features)));
}
