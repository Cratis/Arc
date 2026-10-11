// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Defines a command response value handler for enumerables of attributed reference values.
/// </summary>
/// <typeparam name="TAttribute">The attribute identifying elements consumed by the handler on the server.</typeparam>
/// <remarks>
/// The declaration lets build-time tooling distinguish enumerables compatible with <see cref="IEnumerable{Object}"/>
/// whose declared reference element type carries the attribute from values returned to the client. Inherited attributes
/// count only when their attribute usage permits inheritance. Rectangular arrays, value-type element collections and
/// object-element collections are not implied. Scalar values require a separate
/// <see cref="ICommandResponseValueHandlerForAttributed{TAttribute}"/> declaration. Runtime handlers continue to implement
/// <see cref="ICommandResponseValueHandler"/> directly; the marker does not change runtime value handling.
/// </remarks>
public interface ICommandResponseValueHandlerForAttributedEnumerable<TAttribute>
    where TAttribute : Attribute;
