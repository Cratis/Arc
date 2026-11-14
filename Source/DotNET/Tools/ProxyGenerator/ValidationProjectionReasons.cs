// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Stable reasons for client validation projection decisions.</summary>
internal static class ValidationProjectionReasons
{
    /// <summary>The rule is projectable.</summary>
    public const string Supported = "supported";
    /// <summary>The rule depends on a condition.</summary>
    public const string Conditional = "conditional";
    /// <summary>The rule uses asynchronous validation.</summary>
    public const string Async = "async";
    /// <summary>The validator kind has no client mapping.</summary>
    public const string UnsupportedValidator = "unsupported-validator";
    /// <summary>The comparison is not numeric.</summary>
    public const string NonNumericComparison = "non-numeric-comparison";
    /// <summary>The comparison operator has no client mapping.</summary>
    public const string UnsupportedComparison = "unsupported-comparison";
    /// <summary>The message requires a runtime delegate.</summary>
    public const string DeferredMessage = "deferred-message";
    /// <summary>The severity requires a runtime delegate.</summary>
    public const string DynamicSeverity = "dynamic-severity";
    /// <summary>Presence is not required for an optional concept.</summary>
    public const string OptionalConceptPresence = "optional-concept-presence";
    /// <summary>Explicit rules replace the annotation source.</summary>
    public const string OverriddenAnnotation = "overridden-annotation";
    /// <summary>The endpoint has no projectable client rules.</summary>
    public const string NoProjectableRules = "no-projectable-rules";
}
