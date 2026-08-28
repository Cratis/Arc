// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Core.Generators.Integration.Specs.Testing;

/// <summary>
/// The exception that is thrown when package-graph integration setup fails.
/// </summary>
/// <param name="message">The failure message.</param>
public sealed class PackageGraphFailure(string message) : Exception(message);
