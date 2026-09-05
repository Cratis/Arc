// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Provider-neutral keys for values held on a <see cref="CommandContext"/>.
/// </summary>
public static class CommandContextKeys
{
    /// <summary>
    /// The key for the provider-neutral resolved key in the command context values.
    /// </summary>
    /// <remarks>
    /// The resolved key is the command's key expressed as a plain string, independent of any backing store. A provider
    /// that owns the key resolution (for example the Chronicle integration, which resolves an event source id) writes it
    /// so that a read model backing provider — which need not depend on the resolving provider — can load a read model
    /// by the same key.
    /// </remarks>
    public const string ResolvedKey = "resolvedKey";
}
