// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Controls the existing validation extraction sources and optional observation.</summary>
/// <param name="IncludeConceptRules">Whether to merge concept rules.</param>
/// <param name="IncludeDataAnnotations">Whether to merge annotation rules.</param>
/// <param name="Capture">Optional allowlisted metadata capture.</param>
internal sealed record ValidationExtractionOptions(bool IncludeConceptRules, bool IncludeDataAnnotations, ValidationProjectionCapture? Capture);
