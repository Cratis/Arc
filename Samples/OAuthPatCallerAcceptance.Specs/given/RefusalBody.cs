// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using System.Text.Json;

namespace OAuthPatCallerAcceptance.Specs.given;

public static class RefusalBody
{
    public static bool IsSafe(string path, HttpStatusCode status, string? contentType, string body)
    {
        // Empty native responses convey only their HTTP status (routing or the REST read-scope check).
        if (body.Length == 0) return status is HttpStatusCode.Forbidden or HttpStatusCode.NotFound;
        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return false;
            return path == "/mcp" ? IsMcp(root, status, contentType) : IsRest(root, status, contentType);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    static bool IsMcp(JsonElement root, HttpStatusCode status, string? contentType)
    {
        if (contentType != "application/json" || root.EnumerateObject().Count() != 1) return false;
        if (!root.TryGetProperty("error", out var error) || error.ValueKind != JsonValueKind.String) return false;
        return status switch
        {
            HttpStatusCode.Unauthorized => error.GetString() == "unauthorized",
            HttpStatusCode.Forbidden => error.GetString() == "forbidden" || error.GetString() == "insufficient_scope",
            HttpStatusCode.BadRequest => error.GetString() == "invalid_request",
            _ => false
        };
    }

    static bool IsRest(JsonElement root, HttpStatusCode status, string? contentType)
    {
        if (contentType != "application/problem+json" || root.EnumerateObject().Count() != 3) return false;
        if (!HasString(root, "type", "about:blank") || !HasStatus(root, status)) return false;
        return HasString(root, "title", Title(status));
    }

    static bool HasStatus(JsonElement root, HttpStatusCode status) =>
        root.TryGetProperty("status", out var code) && code.ValueKind == JsonValueKind.Number && code.TryGetInt32(out var value) && value == (int)status;

    static bool HasString(JsonElement root, string name, string? expected) =>
        root.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.String && property.GetString() == expected;

    static string? Title(HttpStatusCode status) => status switch
    {
        HttpStatusCode.Unauthorized => "Unauthorized",
        HttpStatusCode.Forbidden => "Forbidden",
        HttpStatusCode.BadRequest => "Bad Request",
        _ => null
    };
}
