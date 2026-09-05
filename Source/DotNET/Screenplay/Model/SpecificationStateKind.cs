// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.Model;

/// <summary>
/// Represents what a state a specification names actually is.
/// </summary>
public enum SpecificationStateKind
{
    /// <summary>
    /// An event, which is what a <c>given</c> and a <c>then</c> name by default.
    /// </summary>
    Event = 0,

    /// <summary>
    /// A read model, which a <c>given</c> and a <c>then</c> name behind the <c>readmodel</c> keyword.
    /// </summary>
    ReadModel = 1,

    /// <summary>
    /// A command, which is what the single <c>when</c> of a specification names.
    /// </summary>
    Command = 2
}
