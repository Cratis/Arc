// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a command response value handler for values whose type carries an attribute.
/// </summary>
/// <typeparam name="TAttribute">The attribute identifying values consumed by the handler on the server.</typeparam>
/// <remarks>
/// The declaration lets build-time tooling distinguish values whose own type carries the attribute from values
/// returned to the client. Inherited attributes count only when their attribute usage permits inheritance.
/// Collections are not implied; declare <see cref="ICommandResponseValueHandlerForAttributedEnumerable{TAttribute}"/>
/// separately for supported enumerable shapes. Runtime handlers continue to implement
/// <see cref="ICommandResponseValueHandler"/> directly so type discovery and dependency injection retain their
/// existing contract. The marker does not change runtime value handling.
/// </remarks>
public interface ICommandResponseValueHandlerForAttributed<TAttribute>
    where TAttribute : Attribute;
