// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;
using Cratis.Arc.Tenancy;

namespace Cratis.Arc.Identity.Callers;

/// <summary>Resolves raw credentials lazily without altering existing authentication.</summary>
/// <remarks>Internal until Cratis/Arc#3701 locks; then publish.</remarks>
/// <param name="requests">The current request accessor.</param>
/// <param name="sources">Explicitly registered credential verifiers.</param>
/// <param name="gate">The credential presentation boundary.</param>
/// <param name="builder">The verified-facts construction boundary.</param>
/// <param name="installation">Explicit installation binding.</param>
/// <param name="tenants">The independently resolved tenant.</param>
internal sealed class VerifiedCallerResolver(
    IHttpRequestContextAccessor requests,
    IEnumerable<IVerifiedCredentialSource> sources,
    CredentialPresentationGate gate,
    VerifiedCallerBuilder builder,
    VerifiedCallerInstallation installation,
    ITenantIdAccessor tenants) : IVerifiedCallerAccessor
{
    static readonly object _cacheKey = new();

    /// <inheritdoc/>
    public Task<CallerResolution> GetCurrent()
    {
        var request = requests.Current;
        if (request is null)
        {
            return Task.FromResult(new CallerResolution(null, CallerRefusal.NoCredential));
        }
        var tenant = CurrentTenant();
        var inputs = CallerResolutionInputs.Capture(request, sources, tenant);
        Entry entry;
        lock (request.Items)
        {
            if (request.Items.TryGetValue(_cacheKey, out var cached) && cached is Entry existing)
            {
                if (!existing.CanReuse(inputs, installation))
                {
                    existing.Poisoned = true;
                    return Task.FromResult(new CallerResolution(null, CallerRefusal.RequestChanged));
                }
                return existing.Resolution.Task;
            }
            entry = new(inputs, installation);
            request.Items[_cacheKey] = entry;
        }
        _ = ResolveBound(request, entry);
        return entry.Resolution.Task;
    }

    static async Task<CredentialVerification> Verify(IVerifiedCredentialSource source, CredentialPresentation presented, CancellationToken cancellationToken)
    {
        try
        {
            return await source.Verify(presented, cancellationToken) ?? CredentialVerification.Refused(CallerRefusal.SourceRefused);
        }
        catch (Exception)
        {
            // Verification outages must not become weaker-source fallback or a partially populated caller.
            return CredentialVerification.Refused(CallerRefusal.SourceRefused);
        }
    }

    async Task ResolveBound(IHttpRequestContext request, Entry entry)
    {
        try
        {
            var resolution = await Resolve(request);
            var current = CallerResolutionInputs.Capture(request, sources, CurrentTenant());
            lock (request.Items)
            {
                if (!entry.Inputs.Matches(current)) entry.Poisoned = true;
                entry.Resolution.SetResult(entry.Poisoned ? new(null, CallerRefusal.RequestChanged) : resolution);
            }
        }
        catch (Exception exception)
        {
            entry.Resolution.SetException(exception);
        }
    }

    async Task<CallerResolution> Resolve(IHttpRequestContext request)
    {
        // The gate and source capture all credential request inputs synchronously before the first await,
        // matching the cached snapshot. ASP.NET does not support concurrent HttpContext writes.
        var presented = gate.Inspect(request);
        if (presented.Refusal is not null)
        {
            return new(null, presented.Refusal);
        }
        var candidates = sources.Where(source => source.Shape == presented.Shape).ToArray();
        if (candidates.Length == 0)
        {
            return new(null, CallerRefusal.NoMatchingSource);
        }
        if (candidates.Length > 1)
        {
            return new(null, CallerRefusal.SourceConflict);
        }
        var verification = await Verify(candidates[0], presented, request.RequestAborted);
        return Construct(verification);
    }

    CallerResolution Construct(CredentialVerification verification)
    {
        if (!verification.Applicable)
        {
            return new(null, CallerRefusal.NoMatchingSource);
        }
        if (verification.Refusal is not null || verification.Facts is null)
        {
            return new(null, verification.Refusal ?? CallerRefusal.SourceRefused);
        }
        var result = builder.Build(verification.Facts, installation, CurrentTenant());
        return new(result.Caller, result.Refusal);
    }

    /// <summary>Reads the tenant without a membership check, which runs once the verified principal exists.</summary>
    /// <returns>The tenant candidate.</returns>
    TenantId CurrentTenant() => tenants is TenantIdAccessor accessor ? accessor.Candidate : tenants.Current;

    sealed class Entry(CallerResolutionInputs inputs, VerifiedCallerInstallation installation)
    {
        internal CallerResolutionInputs Inputs { get; } = inputs;
        internal VerifiedCallerInstallation Installation { get; } = installation;
        internal TaskCompletionSource<CallerResolution> Resolution { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal bool Poisoned { get; set; }

        internal bool CanReuse(CallerResolutionInputs current, VerifiedCallerInstallation currentInstallation)
        {
            if (Poisoned) return false;
            if (!ReferenceEquals(Installation, currentInstallation)) return false;
            return Inputs.Matches(current);
        }
    }
}
