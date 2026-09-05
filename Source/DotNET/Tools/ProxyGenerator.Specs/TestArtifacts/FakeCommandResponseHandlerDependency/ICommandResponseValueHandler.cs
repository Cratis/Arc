// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Counterfeit runtime response handler contract with the same full name as the Arc contract.
/// </summary>
public interface ICommandResponseValueHandler;

/// <summary>
/// Counterfeit typed response handler declaration with the same full name as the Arc declaration.
/// </summary>
/// <typeparam name="TValue">The allegedly handled value.</typeparam>
public interface ICommandResponseValueHandler<TValue>;
