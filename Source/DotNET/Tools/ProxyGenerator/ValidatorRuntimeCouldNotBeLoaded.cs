// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Identifies an executable validation discovery failure that must stop proxy generation.
/// </summary>
/// <param name="context">The assembly, type, or validator being discovered.</param>
/// <param name="innerException">The runtime failure.</param>
internal sealed class ValidatorRuntimeCouldNotBeLoaded(string context, Exception innerException)
    : Exception($"Runtime validation discovery failed for {context}: {innerException.GetBaseException().Message}", innerException);
