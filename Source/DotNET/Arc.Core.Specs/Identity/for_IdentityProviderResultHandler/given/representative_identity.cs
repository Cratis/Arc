// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Cratis.Concepts;

namespace Cratis.Arc.Identity.for_IdentityProvider.given;

#pragma warning disable SA1402 // File may only contain a single type

/// <summary>
/// An identity result exercising what the identity JSON depends on: concept identities, the relaxed encoder,
/// the Arc naming policy, the enum and concept converters and ignoring null values.
/// </summary>
public static class representative_identity
{
    public static readonly ProfileDetails Details = new(
        "R&D <Ærlig>",
        new Level(3),
        AccessKind.Administrator,
        null,
        new ProfileAddress("Storgata 1", "Oslo"),
        ["Reader", "Writer"]);

    public static readonly IdentityProviderResult Result = new(
        new IdentityId("user-æøå-123"),
        new IdentityName("Ola \"Nordmann\" <ola@example.com>"),
        true,
        true,
        ["Admin", "Reader"],
        Details);

    /// <summary>
    /// Creates the options <see cref="IdentityProvider"/> serialized with before its own types were source generated:
    /// a copy of the Arc options with the relaxed encoder, resolving every type through reflection.
    /// </summary>
    /// <param name="arcOptions">The <see cref="ArcOptions"/>.</param>
    /// <returns>The <see cref="JsonSerializerOptions"/>.</returns>
    public static JsonSerializerOptions OptionsAsBefore(ArcOptions arcOptions) =>
        new(arcOptions.JsonSerializerOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
        };
}

public record Level(int Value) : ConceptAs<int>(Value);

public enum AccessKind
{
    Reader = 0,
    Administrator = 1
}

public record ProfileAddress(string Street, string City);

public record ProfileDetails(string Department, Level Level, AccessKind Access, string? Nickname, ProfileAddress Address, IEnumerable<string> Groups);

#pragma warning restore SA1402 // File may only contain a single type
