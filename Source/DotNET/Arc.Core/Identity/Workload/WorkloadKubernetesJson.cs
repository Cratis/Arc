// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Arc.Identity.Workload;

/// <summary>Strict parsing of the authenticated Kubernetes API responses used by workload verification.</summary>
internal static class WorkloadKubernetesJson
{
    /// <summary>Reads only an authenticated, correctly audience-bound service account.</summary>
    /// <param name="root">The TokenReview response.</param>
    /// <param name="audience">The receiving audience.</param>
    /// <returns>The authenticated Pod-bound identity, or none.</returns>
    internal static WorkloadTokenReview? Review(JsonElement root, string audience)
    {
        var status = root.GetProperty("status");
        if (!AuthenticatedFor(status, audience)) return null;
        var user = status.GetProperty("user");
        var parts = Text(user, "username").Split(':');
        if (!CanonicalServiceAccount(parts)) return null;
        if (!ExpectedGroups(user, parts[2])) return null;
        var extra = user.GetProperty("extra");
        return new(parts[2], parts[3], Text(user, "uid"), Single(extra, "authentication.kubernetes.io/pod-name"), Single(extra, "authentication.kubernetes.io/pod-uid"));
    }

    /// <summary>Checks the current Pod against the authenticated binding.</summary>
    /// <param name="root">The Pod response.</param>
    /// <param name="review">The authenticated binding.</param>
    /// <returns>Whether the exact Pod is live and unchanged.</returns>
    internal static bool LivePod(JsonElement root, WorkloadTokenReview review)
    {
        var metadata = root.GetProperty("metadata");
        if (metadata.TryGetProperty("deletionTimestamp", out var deletion) && deletion.ValueKind != JsonValueKind.Null) return false;
        return Text(metadata, "uid") == review.PodUid && Text(metadata, "name") == review.PodName &&
            Text(metadata, "namespace") == review.Namespace && Text(root.GetProperty("spec"), "serviceAccountName") == review.ServiceAccount;
    }

    /// <summary>Reads an expiry bound, never using local JWT claims as identity authority.</summary>
    /// <param name="token">The projected JWT.</param>
    /// <returns>The token expiry bound.</returns>
    /// <exception cref="FormatException">The token is not a three-part JWT.</exception>
    internal static DateTimeOffset Expiry(string token)
    {
        var parts = token.Split('.');
        if (parts.Length != 3) throw new FormatException("A projected token must be a JWT.");
        var payload = parts[1].Replace('-', '+').Replace('_', '/');
        payload = payload.PadRight((payload.Length + 3) / 4 * 4, '=');
        using var json = JsonDocument.Parse(Convert.FromBase64String(payload));
        return DateTimeOffset.FromUnixTimeSeconds(json.RootElement.GetProperty("exp").GetInt64());
    }

    static bool AuthenticatedFor(JsonElement status, string audience)
    {
        if (!status.GetProperty("authenticated").GetBoolean()) return false;
        if (status.TryGetProperty("error", out var error) && !string.IsNullOrEmpty(error.GetString())) return false;
        return status.GetProperty("audiences").EnumerateArray().Any(value => value.GetString() == audience);
    }

    static bool CanonicalServiceAccount(string[] parts) => parts.Length == 4 && parts[0] == "system" &&
        parts[1] == "serviceaccount" && ValidSegment(parts[2]) && ValidSegment(parts[3]);

    static bool ExpectedGroups(JsonElement user, string namespaceName)
    {
        var groups = user.GetProperty("groups").EnumerateArray().Select(value => value.GetString()).ToHashSet(StringComparer.Ordinal);
        return new[] { "system:authenticated", "system:serviceaccounts", $"system:serviceaccounts:{namespaceName}" }.All(groups.Contains);
    }

    static string Text(JsonElement parent, string name)
    {
        var value = parent.GetProperty(name).GetString();
        return string.IsNullOrWhiteSpace(value) ? throw new JsonException("Required Kubernetes identity field missing.") : value;
    }

    static string Single(JsonElement parent, string name)
    {
        var values = parent.GetProperty(name);
        if (values.GetArrayLength() != 1) throw new JsonException("Ambiguous Kubernetes binding.");
        var value = values[0].GetString();
        if (!ValidSegment(value)) throw new JsonException("Invalid Kubernetes binding.");
        return value!;
    }

    static bool ValidSegment(string? value) => !string.IsNullOrWhiteSpace(value) && value.All(character => char.IsAsciiLetterOrDigit(character) || character is '-' or '.');
}
