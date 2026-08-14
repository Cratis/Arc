// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

/// <summary>
/// Declares a handled type without implementing the runtime handler contract.
/// </summary>
public class MarkerOnlyValueDeclaration : ICommandResponseValueHandler<MarkerOnlyValue>;
