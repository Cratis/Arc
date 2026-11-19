// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>Immutable, server-owned credential routing metadata. Never populated from request data.</summary>
/// <param name="Shape">The credential shape selected by the server endpoint.</param>
internal sealed record ExpectedCredentialShape(PresentedCredentialShape Shape)
{
    /// <summary>The private request-items key for server-owned metadata.</summary>
    internal static readonly object Key = new();

    /// <summary>Reads only the server-owned request item.</summary>
    /// <param name="request">The current request.</param>
    /// <returns>The selected shape, or none.</returns>
    internal static ExpectedCredentialShape? Read(Http.IHttpRequestContext request) =>
        request.Items.TryGetValue(Key, out var value) ? value as ExpectedCredentialShape : null;
}
