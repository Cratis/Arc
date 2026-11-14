// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Coarse rule families, without validator values or names.</summary>
internal enum ValidationRuleKind
{
    /// <summary>Presence constraints.</summary>
    Required,
    /// <summary>Length constraints.</summary>
    Length,
    /// <summary>Range constraints.</summary>
    Range,
    /// <summary>Comparisons.</summary>
    Comparison,
    /// <summary>Pattern matching.</summary>
    Regex,
    /// <summary>Email formatting.</summary>
    Email,
    /// <summary>Annotation-based constraints.</summary>
    DataAnnotation,
    /// <summary>Concept constraints.</summary>
    Concept,
    /// <summary>Unmapped custom rules.</summary>
    Custom,
    /// <summary>No known rule family.</summary>
    Unknown
}
