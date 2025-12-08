// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc;

/// <summary>
/// Represents an attribute that indicates that the validation should be ignored for actions in a controller. This means you'll get the default behavior of ASP.NET Core.
/// </summary>
/// <remarks>
/// Can be used for an entire controller or individual actions.
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class IgnoreValidationAttribute : Attribute, IFilterMetadata;
