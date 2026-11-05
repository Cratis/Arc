// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// A single copied set of presented credentials, not proof of authenticity. Contains sensitive transport values: never log it.
/// Envelope v1 plus bearer are two credentials. A future v2 verifier may claim a bearer only after checking its signed exact-token hash;
/// that future rule belongs in the gate's multiple-credential decision, not in header collection. No v2 coverage is implemented here.
/// </summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="authorization">Copied authorization values.</param>
/// <param name="envelope">Copied envelope fields.</param>
/// <param name="refusal">Any presentation refusal.</param>
/// <param name="shape">The selected dispatch shape, if any.</param>
internal sealed class CredentialPresentation(
    IReadOnlyList<string> authorization,
    IReadOnlyDictionary<string, IReadOnlyList<string>> envelope,
    CallerRefusal? refusal = null,
    PresentedCredentialShape? shape = null)
{
    /// <summary>Gets the copied authorization values. Never log these.</summary>
    internal IReadOnlyList<string> Authorization { get; } = authorization;

    /// <summary>Gets the copied envelope fields. Never log these.</summary>
    internal IReadOnlyDictionary<string, IReadOnlyList<string>> Envelope { get; } = envelope;

    /// <summary>Gets the gate's refusal, if any.</summary>
    internal CallerRefusal? Refusal { get; } = refusal;

    /// <summary>Gets the presentation dispatch shape, not authenticated identity.</summary>
    internal PresentedCredentialShape? Shape { get; } = shape;

    /// <inheritdoc/>
    public override string ToString() => nameof(CredentialPresentation);
}
