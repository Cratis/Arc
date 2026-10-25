// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Delegates shape and generation-delivery admission to Chronicle.
/// </summary>
internal static class DecisionReadVerdict
{
    /// <summary>
    /// Checks a discovered model without materializing it or enrolling a read.
    /// </summary>
    /// <param name="reads">Chronicle's admission authority.</param>
    /// <param name="model">The declared model type.</param>
    /// <param name="streamScoped">Whether the parameter supplies stream scope.</param>
    /// <returns>The refusal, when the shape is not admitted.</returns>
    internal static DecisionReadRefusalReason? For(IDecisionReads reads, Type model, bool streamScoped) =>
        ReadModelDispatch.For(model).Refusal(reads, streamScoped);

    /// <summary>
    /// Checks a closed read-model declaration.
    /// </summary>
    /// <typeparam name="T">The declared model type.</typeparam>
    /// <param name="reads">Chronicle's admission authority.</param>
    /// <param name="streamScoped">Whether the parameter supplies stream scope.</param>
    /// <returns>The refusal, when the shape is not admitted.</returns>
    internal static DecisionReadRefusalReason? Refusal<T>(IDecisionReads reads, bool streamScoped)
        where T : class
    {
        var description = reads.Describe<T>();
        var admission = streamScoped ? reads.AdmitForStream<T>() : description.Admission;
        return description.UsesPinnedGenerationDelivery
            ? DecisionReadRefusalReason.GenerationDeliveryNotSupported
            : admission.IsAdmitted ? null : admission.Reason;
    }
}
