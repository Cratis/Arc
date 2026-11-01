// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Cratis.Arc.Identity;

/// <summary>
/// Holds validated integration keys and an exact private authentication-options snapshot.
/// </summary>
/// <param name="Keys">The decoded keys indexed by case-sensitive ID.</param>
/// <param name="Snapshot">The private framed configuration snapshot.</param>
internal sealed record ForwardedIdentityConfiguration(IReadOnlyDictionary<string, byte[]> Keys, string Snapshot)
{
    static readonly ConditionalWeakTable<ArcOptions, Entry> _cache = new();

    /// <summary>
    /// Validates opt-in configuration, failing closed on any malformed key entry. The result is cached per
    /// <see cref="ArcOptions"/> instance and recomputed whenever any consumed value changes.
    /// </summary>
    /// <param name="options">The host configuration.</param>
    /// <returns>The validated configuration, or null if invalid.</returns>
    internal static ForwardedIdentityConfiguration? Read(ArcOptions options)
    {
        if (_cache.TryGetValue(options, out var entry) && entry.Inputs.Matches(options))
        {
            return entry.Configuration;
        }

        var inputs = Inputs.Capture(options);
        var parsed = Parse(inputs);
        _cache.AddOrUpdate(options, new(inputs, parsed));
        return parsed;
    }

    static ForwardedIdentityConfiguration? Parse(Inputs inputs)
    {
        if (!inputs.Trust || !inputs.HasForwardedIdentity || !inputs.ForwardedEnabled ||
            string.IsNullOrWhiteSpace(inputs.Audience) || inputs.Audience.Length > 128 ||
            inputs.Audience.Any(character => character is < ' ' or >= (char)127) ||
            inputs.Keys is not { Length: >= 1 and <= 8 } || !inputs.HasOnBehalfOf || inputs.SigningKeys is null || !inputs.HasTenancy)
        {
            return null;
        }
        var keys = new Dictionary<string, byte[]>(StringComparer.Ordinal);
        var snapshot = new StringBuilder();
        foreach (var value in new[]
        {
            inputs.Trust.ToString(), inputs.ForwardedEnabled.ToString(), inputs.Audience, inputs.TenancyHeader,
            inputs.OnBehalfEnabled.ToString(), inputs.Issuer, inputs.OnBehalfAudience, inputs.Service,
            inputs.ClockSkew.Ticks.ToString(CultureInfo.InvariantCulture)
        })
        {
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(value ?? string.Empty));
        }
        snapshot.Append(ForwardedIdentityCanonicalization.Frame(inputs.Keys.Length.ToString(CultureInfo.InvariantCulture)));
        foreach (var key in inputs.Keys)
        {
            if (key is null || !ForwardedIdentityEnvelope.IsKeyId(key.Id) || key.Value is not { Length: 44 })
            {
                return null;
            }
            var bytes = new byte[32];
            if (!Convert.TryFromBase64String(key.Value, bytes, out var count) || count != 32 ||
                Convert.ToBase64String(bytes) != key.Value || !keys.TryAdd(key.Id!, bytes))
            {
                return null;
            }
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(key.Id!)).Append(ForwardedIdentityCanonicalization.Frame(Convert.ToHexString(SHA256.HashData(bytes))));
        }
        snapshot.Append(ForwardedIdentityCanonicalization.Frame(inputs.SigningKeys.Length.ToString(CultureInfo.InvariantCulture)));
        foreach (var key in inputs.SigningKeys)
        {
            if (key is null)
            {
                return null;
            }
            snapshot.Append(ForwardedIdentityCanonicalization.Frame(key.Id ?? string.Empty)).Append(ForwardedIdentityCanonicalization.Frame(key.Value ?? string.Empty));
        }
        return new(keys, snapshot.ToString());
    }

    sealed record Entry(Inputs Inputs, ForwardedIdentityConfiguration? Configuration);

    sealed record Pair(string? Id, string? Value);

    /// <summary>
    /// An immutable capture of every option value that configuration depends on, comparable without allocating.
    /// </summary>
    sealed class Inputs
    {
        internal bool Trust { get; private init; }
        internal bool HasForwardedIdentity { get; private init; }
        internal bool ForwardedEnabled { get; private init; }
        internal string? Audience { get; private init; }
        internal Pair?[]? Keys { get; private init; }
        internal bool HasTenancy { get; private init; }
        internal string? TenancyHeader { get; private init; }
        internal bool HasOnBehalfOf { get; private init; }
        internal bool OnBehalfEnabled { get; private init; }
        internal string? Issuer { get; private init; }
        internal string? OnBehalfAudience { get; private init; }
        internal string? Service { get; private init; }
        internal TimeSpan ClockSkew { get; private init; }
        internal Pair?[]? SigningKeys { get; private init; }

        internal static Inputs Capture(ArcOptions options)
        {
            var forwarded = options.ForwardedIdentity;
            var onBehalf = options.OnBehalfOf;
            return new()
            {
                Trust = options.TrustForwardedIdentityHeaders,
                HasForwardedIdentity = forwarded is not null,
                ForwardedEnabled = forwarded?.Enabled ?? false,
                Audience = forwarded?.Audience,
                Keys = forwarded?.Keys is { } keys ? Copy(keys, key => key.KeyId, key => key.SecretBase64) : null,
                HasTenancy = options.Tenancy is not null,
                TenancyHeader = options.Tenancy?.HttpHeader,
                HasOnBehalfOf = onBehalf is not null,
                OnBehalfEnabled = onBehalf?.Enabled ?? false,
                Issuer = onBehalf?.Issuer,
                OnBehalfAudience = onBehalf?.Audience,
                Service = onBehalf?.Service,
                ClockSkew = onBehalf?.ClockSkew ?? default,
                SigningKeys = onBehalf?.SigningKeys is { } signing ? Copy(signing, key => key.KeyId, key => key.PublicKeyPem) : null
            };
        }

        /// <summary>
        /// Compares the capture to the current options, field by field, without allocating.
        /// </summary>
        /// <param name="options">The current options.</param>
        /// <returns>True when every consumed value is unchanged.</returns>
        internal bool Matches(ArcOptions options)
        {
            var forwarded = options.ForwardedIdentity;
            var onBehalf = options.OnBehalfOf;
            var tenancy = options.Tenancy;
            if (Trust != options.TrustForwardedIdentityHeaders ||
                HasForwardedIdentity != (forwarded is not null) ||
                HasTenancy != (tenancy is not null) ||
                HasOnBehalfOf != (onBehalf is not null))
            {
                return false;
            }

            return (forwarded is null || (ForwardedEnabled == forwarded.Enabled && string.Equals(Audience, forwarded.Audience, StringComparison.Ordinal) && Same(Keys, forwarded.Keys, KeyOf))) &&
                (tenancy is null || string.Equals(TenancyHeader, tenancy.HttpHeader, StringComparison.Ordinal)) &&
                (onBehalf is null ||
                    (OnBehalfEnabled == onBehalf.Enabled &&
                    ClockSkew == onBehalf.ClockSkew &&
                    string.Equals(Issuer, onBehalf.Issuer, StringComparison.Ordinal) &&
                    string.Equals(OnBehalfAudience, onBehalf.Audience, StringComparison.Ordinal) &&
                    string.Equals(Service, onBehalf.Service, StringComparison.Ordinal) &&
                    Same(SigningKeys, onBehalf.SigningKeys, SigningKeyOf)));
        }

        static bool KeyOf(Pair? captured, ForwardedIdentityKey? current) =>
            captured is null ? current is null : current is not null &&
                string.Equals(captured.Id, current.KeyId, StringComparison.Ordinal) && string.Equals(captured.Value, current.SecretBase64, StringComparison.Ordinal);

        static bool SigningKeyOf(Pair? captured, OnBehalfOfSigningKey? current) =>
            captured is null ? current is null : current is not null &&
                string.Equals(captured.Id, current.KeyId, StringComparison.Ordinal) && string.Equals(captured.Value, current.PublicKeyPem, StringComparison.Ordinal);

        static bool Same<T>(Pair?[]? captured, IList<T>? current, Func<Pair?, T?, bool> equal)
        {
            if (captured is null || current is null)
            {
                return captured is null && current is null;
            }

            if (captured.Length != current.Count)
            {
                return false;
            }

            for (var index = 0; index < captured.Length; index++)
            {
                if (!equal(captured[index], current[index]))
                {
                    return false;
                }
            }

            return true;
        }

        static Pair?[] Copy<T>(IList<T> source, Func<T, string?> id, Func<T, string?> value)
        {
            var copy = new Pair?[source.Count];
            for (var index = 0; index < copy.Length; index++)
            {
                copy[index] = source[index] is { } item ? new(id(item), value(item)) : null;
            }

            return copy;
        }
    }
}
