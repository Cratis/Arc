// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Describes a generated artifact that differs from the expected output.</summary>
/// <param name="Kind">The missing, changed or obsolete category.</param>
/// <param name="RelativePath">The output-relative artifact path.</param>
internal sealed record DriftFinding(string Kind, string RelativePath);
