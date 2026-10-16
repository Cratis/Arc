// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// The exception that is thrown when explicit-offset binding is applied to an unsupported property type.
/// </summary>
/// <param name="type">The unsupported type.</param>
public sealed class ExplicitOffsetRequiresDateTimeOrDateTimeOffset(Type type)
    : Exception($"ExplicitOffset requires DateTime, DateTimeOffset, or their nullable forms, not '{type}'.");
