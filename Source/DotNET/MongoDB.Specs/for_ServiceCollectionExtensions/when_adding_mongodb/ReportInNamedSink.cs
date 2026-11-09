// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Sinks;

namespace Cratis.Arc.MongoDB.for_ServiceCollectionExtensions.when_adding_mongodb;

[ReadModelSink("reporting")]
public record ReportInNamedSink(Guid Id);
