// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Arc.ProxyGenerator.Specs.CommandResponseHandlerDependency;

namespace Cratis.Arc.ProxyGenerator.Specs.FakeCommandResponseHandlerDependency;

/// <summary>
/// Claims attributed values through counterfeit response-handler contracts.
/// </summary>
public class FakeAttributedValueHandler : ICommandResponseValueHandler, ICommandResponseValueHandlerForAttributed<ConsumedEventAttribute>;
