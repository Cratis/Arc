// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Samples.Features.CallerKeyedLookups;

/// <summary>
/// Declares an application-selected caller claim for the published lookup refusal sample.
/// </summary>
public sealed class LoginClaim() : CallerClaim("login");
