// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a result filter that intercepts ISubject results and handles them as streaming results.
/// </summary>
/// <param name="jsonOptions">The JSON options.</param>
public class ObservableQueryResultFilter(IOptions<JsonOptions> jsonOptions) : IAsyncResultFilter
{
    /// <inheritdoc/>
    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ObjectResult objectResult && objectResult.Value is IClientObservable clientObservable)
        {
            context.Result = new ClientObservableResult(clientObservable, jsonOptions.Value);
            await next();
            return;
        }

        await next();
    }
}
