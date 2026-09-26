// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Testing.Queries;

/// <summary>
/// The exception that is thrown when a query scenario is configured with a pipeline other than Arc's hosted query pipeline.
/// </summary>
public sealed class QueryScenarioRequiresArcQueryPipeline()
    : Exception("Query scenarios require Arc's QueryPipeline to execute hosted authorization. Register Arc's QueryPipeline as IQueryPipeline.");
