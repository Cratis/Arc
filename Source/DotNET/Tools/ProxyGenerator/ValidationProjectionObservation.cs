// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Observes only metadata at the extractor's existing omission decisions.</summary>
internal static class ValidationProjectionObservation
{
    internal static void ObserveOmission(object validator, ValidationProjectionCapture? capture)
    {
        if (capture is null) return;
        var interfaces = validator.GetType().GetInterfaces().Select(_ => _.FullName ?? string.Empty).ToArray();
        if (interfaces.Any(_ => _.StartsWith("FluentValidation.Validators.IAsyncPropertyValidator", StringComparison.Ordinal)))
        {
            capture.Omitted(ValidationRuleKind.Custom, ValidationProjectionReasons.Async);
            return;
        }
        if (interfaces.Contains("FluentValidation.Validators.IComparisonValidator"))
        {
            var value = validator.GetType().GetProperty("ValueToCompare")?.GetValue(validator);
            var numeric = value is byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal;
            capture.Omitted(ValidationRuleKind.Comparison, numeric ? ValidationProjectionReasons.UnsupportedComparison : ValidationProjectionReasons.NonNumericComparison);
            return;
        }
        capture.Omitted(ValidationRuleKind.Custom, ValidationProjectionReasons.UnsupportedValidator);
    }

    internal static void ObserveDetails(object component, ValidationProjectionCapture? capture)
    {
        if (capture is null) return;
        var factory = component.GetType().GetField("_errorMessageFactory", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(component);
        if (factory is Delegate) capture.Omitted(ValidationRuleKind.Unknown, ValidationProjectionReasons.DeferredMessage);
    }
}
