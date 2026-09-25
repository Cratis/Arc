// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// The exception that is thrown when multiple operations fail while stopping an observable query.
/// </summary>
/// <param name="failures">The connection and cleanup failures, in observation order.</param>
internal class ObservableQueryTeardownFailed(IEnumerable<Exception> failures)
    : AggregateException("Multiple operations failed while stopping the observable query.", failures);
