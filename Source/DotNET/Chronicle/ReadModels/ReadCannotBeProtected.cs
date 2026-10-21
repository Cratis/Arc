// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// The exception that is thrown when a command parameter cannot be guarded by Chronicle.
/// </summary>
/// <param name="commandType">The command making the decision.</param>
/// <param name="parameter">The parameter being resolved.</param>
/// <param name="modelType">The read model being requested.</param>
/// <param name="reason">Why the read cannot be protected.</param>
/// <param name="fix">How to make the read protectable.</param>
public class ReadCannotBeProtected(Type commandType, string parameter, Type modelType, string reason, string fix)
    : Exception($"{commandType.Name} reads '{parameter}' ({modelType.Name}) {reason}, so read protection cannot guard it. If it is only context, mark it [Unprotected(\"why\")]. Otherwise {fix}.");
