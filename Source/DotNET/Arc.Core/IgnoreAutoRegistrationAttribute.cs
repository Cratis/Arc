// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc;

/// <summary>
/// Represents an attribute that indicates that a class should be ignored during automatic registration.
/// </summary>
/// <remarks>
/// This attribute is supported by different systems in the Arc to indicate that a class should be ignored
/// by automatic registration mechanisms.
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class IgnoreAutoRegistrationAttribute : Attribute;
