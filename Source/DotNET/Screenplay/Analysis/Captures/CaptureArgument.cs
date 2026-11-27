// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Analysis.Captures;

/// <summary>
/// The captured field an argument of the appended event is read from.
/// </summary>
/// <param name="Property">The name of the property the argument fills.</param>
/// <param name="Type">The fully qualified type of the argument.</param>
/// <param name="Field">The name of the captured field.</param>
public record CaptureArgument(string Property, string Type, string Field);
