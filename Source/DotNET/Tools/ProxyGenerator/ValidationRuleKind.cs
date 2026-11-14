// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Coarse rule families, without validator values or names.</summary>
internal enum ValidationRuleKind
{
    /// <summary>Presence constraints.</summary>
    Required = 0,

    /// <summary>Length constraints.</summary>
    Length = 1,

    /// <summary>Range constraints.</summary>
    Range = 2,

    /// <summary>Comparisons.</summary>
    Comparison = 3,

    /// <summary>Pattern matching.</summary>
    Regex = 4,

    /// <summary>Email formatting.</summary>
    Email = 5,

    /// <summary>Annotation-based constraints.</summary>
    DataAnnotation = 6,

    /// <summary>Concept constraints.</summary>
    Concept = 7,

    /// <summary>Unmapped custom rules.</summary>
    Custom = 8,

    /// <summary>No known rule family.</summary>
    Unknown = 9
}
