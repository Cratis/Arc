// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Availability.Audiences;

/// <summary>
/// An in-memory <see cref="IDeclaredAudiences"/>.
/// </summary>
/// <param name="declarations">The declared audiences with their descriptions and sources.</param>
[IgnoreConvention]
public class DeclaredAudiences(IEnumerable<AudienceDeclaration> declarations) : IDeclaredAudiences
{
    readonly HashSet<AudienceName> _audiences = [.. declarations.Select(_ => _.Name)];

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredAudiences"/> class.
    /// </summary>
    /// <param name="audiences">The declared audiences.</param>
    public DeclaredAudiences(IEnumerable<AudienceName> audiences)
        : this(audiences.Select(_ => new AudienceDeclaration(_)))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DeclaredAudiences"/> class.
    /// </summary>
    /// <param name="audiences">The declared audiences.</param>
    public DeclaredAudiences(params AudienceName[] audiences)
        : this((IEnumerable<AudienceName>)audiences)
    {
    }

    /// <inheritdoc/>
    public IReadOnlyCollection<AudienceName> All => _audiences;

    /// <inheritdoc/>
    public IReadOnlyCollection<AudienceDeclaration> Declarations { get; } = [.. declarations.DistinctBy(_ => _.Name)];

    /// <inheritdoc/>
    public bool IsDeclared(AudienceName audience) => _audiences.Contains(audience);
}
