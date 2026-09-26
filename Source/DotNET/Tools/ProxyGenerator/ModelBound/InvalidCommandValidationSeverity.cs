// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// The exception that is thrown when a model-bound command declares an unsupported blocking validation severity during proxy generation.
/// </summary>
/// <param name="commandType">The command declaring the unsupported severity.</param>
/// <param name="severity">The unsupported numeric severity.</param>
public sealed class InvalidCommandValidationSeverity(Type commandType, int severity) : Exception($"Command '{commandType}' declares unsupported blocking validation severity '{severity}'. Expected a value between 0 (Unknown) and 3 (Error).");
