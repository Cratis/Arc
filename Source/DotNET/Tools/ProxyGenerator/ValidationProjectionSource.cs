// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Identifies a source inspected by validation extraction.</summary>
internal enum ValidationProjectionSource
{
    /// <summary>A FluentValidation validator.</summary>
    Validator,
    /// <summary>A concept validator.</summary>
    Concept,
    /// <summary>A DataAnnotations attribute.</summary>
    DataAnnotation
}
