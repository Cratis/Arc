// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents how strongly an <see cref="ICanResolveReadModelForCommand"/> claims the read model types it reports.
/// </summary>
/// <remarks>
/// More than one provider can be able to resolve the same read model type, and the application does not control the
/// order its providers are registered in. The ownership a provider declares — rather than that order — decides which one
/// resolves the type.
/// </remarks>
public enum ReadModelForCommandOwnership
{
    /// <summary>
    /// An artifact in the application says the provider owns the read model: a Chronicle projection or reducer targeting
    /// it, or a <c>DbSet</c> carrying it on a read model <c>DbContext</c>. A declaring provider claims the type even when
    /// something else already resolves it.
    /// </summary>
    Declared = 0,

    /// <summary>
    /// The provider can resolve any read model it reports — its store simply holds a document per read model — but
    /// nothing in the application says it owns them. It claims only the read model types nothing else already resolves,
    /// leaving both a declaring provider and the application's own registration untouched.
    /// </summary>
    Fallback = 1
}
