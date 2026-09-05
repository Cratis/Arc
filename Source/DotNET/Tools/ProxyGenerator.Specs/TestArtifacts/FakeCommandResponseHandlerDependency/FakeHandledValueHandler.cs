// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency;

/// <summary>
/// Claims a value through counterfeit response-handler contracts.
/// </summary>
public class FakeHandledValueHandler : ICommandResponseValueHandler, ICommandResponseValueHandler<FakeHandledValue>;
