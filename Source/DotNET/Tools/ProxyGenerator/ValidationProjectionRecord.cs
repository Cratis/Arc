// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator;

/// <summary>Associates allowlisted extraction metadata with an endpoint.</summary>
/// <param name="Endpoint">The endpoint identity.</param>
/// <param name="Projection">The safe projection metadata.</param>
internal sealed record ValidationProjectionRecord(ValidationProjectionEndpoint Endpoint, ValidationProjectionDetail Projection);
