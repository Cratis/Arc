// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Sinks;

namespace Cratis.Arc.Chronicle.ReadModels.for_ChronicleReadModelSinkNames.given;

[Sink("reporting")]
public record ReadModelInNamedSink(Guid Id);
