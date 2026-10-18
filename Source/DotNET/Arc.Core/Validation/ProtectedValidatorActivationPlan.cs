// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Activates only an already inspected constructor or an already verified, DI-owned read leaf.
/// </summary>
/// <param name="Type">The constructed implementation or read contract.</param>
/// <param name="Constructor">The single eligible constructor, absent for a read leaf.</param>
/// <param name="Arguments">The fully inspected constructor dependencies.</param>
/// <param name="Configure">The captured Arc configuration callback.</param>
/// <param name="VerifyRead">Chronicle's verifier, present only on read leaves.</param>
internal sealed record ProtectedValidatorActivationPlan(
    Type Type,
    ConstructorInfo? Constructor,
    ProtectedValidatorActivationPlan[] Arguments,
    Action<object>? Configure,
    Action<object?>? VerifyRead)
{
    /// <summary>
    /// Gets or sets the DI-owned leaf resolved after preflight.
    /// </summary>
    internal object? Value { get; set; }

    /// <summary>
    /// Constructs a fresh graph and records ownership before callbacks execute.
    /// </summary>
    /// <param name="frame">The current model-validation lifetime.</param>
    /// <returns>The fresh graph root, or the verified read leaf.</returns>
    internal object Activate(ProtectedValidatorActivationFrame frame)
    {
        if (VerifyRead is not null)
        {
            return Value!;
        }
        var arguments = Arguments.Select(argument => argument.Activate(frame)).ToArray();
        var value = Constructor!.Invoke(BindingFlags.DoNotWrapExceptions, null, arguments, null);
        frame.Own(value);
        Configure?.Invoke(value);
        return value;
    }
}
