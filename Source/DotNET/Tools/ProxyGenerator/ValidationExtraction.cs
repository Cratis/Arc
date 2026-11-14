// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Pairs existing rule descriptors with their safe projection metadata.</summary>
/// <param name="Rules">Unchanged client rule descriptors.</param>
/// <param name="Projection">Safe extraction metadata.</param>
internal sealed record ValidationExtraction(IEnumerable<PropertyValidationDescriptor> Rules, IReadOnlyList<ValidationProjectionDetail> Projection);
