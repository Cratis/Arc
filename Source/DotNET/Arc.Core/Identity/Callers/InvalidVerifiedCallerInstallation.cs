// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Identity.Callers;

/// <summary>
/// The exception that is thrown when a verified caller installation is incomplete or duplicated.
/// </summary>
/// <param name="reason">The configuration requirement that was not satisfied.</param>
public class InvalidVerifiedCallerInstallation(string reason) : Exception($"Invalid verified caller installation: {reason}");
