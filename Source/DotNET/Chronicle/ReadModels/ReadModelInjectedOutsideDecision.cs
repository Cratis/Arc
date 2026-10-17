// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a protected command resolves a read model through dependency injection outside the
/// parameters Arc binds for its decision, for example in a command filter.
/// </summary>
/// <param name="commandType">The command making the decision.</param>
/// <param name="modelType">The read model being resolved.</param>
public class ReadModelInjectedOutsideDecision(Type commandType, Type modelType)
    : Exception($"{commandType.Name} resolved {modelType.Name} through dependency injection outside Handle() and its validators, for example in a command filter, so read protection cannot guard it. Take {modelType.Name} as a parameter of Handle() or of a CommandValidator<{commandType.Name}> constructor. If it is only context, mark {modelType.Name} [Unprotected(\"why\")].");
