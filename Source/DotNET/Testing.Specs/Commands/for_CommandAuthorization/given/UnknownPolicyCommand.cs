// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Authorization;

namespace Cratis.Arc.Testing.Commands.for_CommandAuthorization.given;

[Authorize(Policy = "missing")]
public class UnknownPolicyCommand;
