// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// A typed generated call into an operation method, after the pipeline has bound all arguments.
/// </summary>
/// <param name="operation">The business declaration.</param>
/// <param name="arguments">Preflighted arguments in signature order.</param>
/// <returns>Completion of the user method.</returns>
public delegate ValueTask CommandOperationMethod(ICommandOperation operation, object?[] arguments);
