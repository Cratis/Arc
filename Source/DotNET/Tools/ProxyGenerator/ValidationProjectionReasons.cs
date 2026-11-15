// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Stable reasons tied to existing extraction decisions.</summary>
internal static class ValidationProjectionReasons
{
    /// <summary>The extractor produced a descriptor.</summary>
    public const string Projected = "projected";

    /// <summary>A block-level condition prevents projection.</summary>
    public const string BlockCondition = "server-only-block-condition";

    /// <summary>A component condition prevents projection.</summary>
    public const string ChainedCondition = "server-only-chained-condition";

    /// <summary>An asynchronous condition prevents projection.</summary>
    public const string AsyncCondition = "server-only-async-condition";

    /// <summary>A concept condition prevents projection.</summary>
    public const string ConceptCondition = "concept-condition-server-only";

    /// <summary>The extractor has no mapping for the validator.</summary>
    public const string UnsupportedValidator = "unsupported-validator";

    /// <summary>The extractor produced a concept-contributed descriptor.</summary>
    public const string ConceptProjected = "concept-contribution-projected";

    /// <summary>Presence rules are omitted for an optional concept.</summary>
    public const string OptionalConceptPresence = "optional-concept-presence";

    /// <summary>Explicit or concept rules replace the annotation source.</summary>
    public const string OverriddenAnnotation = "overridden-annotation";

    /// <summary>The endpoint has no projectable rules.</summary>
    public const string NoProjectableRules = "no-projectable-rules";
}
