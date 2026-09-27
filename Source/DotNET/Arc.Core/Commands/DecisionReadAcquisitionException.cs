// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Signals that a protected decision could not be acquired. A validator must not turn this into a filterable
/// validation result, even if the caller allows validation errors.
/// </summary>
/// <param name="innerException">The underlying refusal or read failure.</param>
public sealed class DecisionReadAcquisitionException(Exception innerException) : InvalidOperationException(innerException.Message, innerException);
