// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Filters;

namespace Cratis.Arc;

/// <summary>
/// Attribute to indicate that warnings should be treated as errors for a command or query action.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TreatWarningsAsErrorsAttribute : Attribute, IFilterMetadata;
