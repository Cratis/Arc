// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reactive.Subjects;
using System.Text;
using Cratis.Arc.AspNetCore.Http;
using Cratis.Arc.Http;
using Cratis.Reflection;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a formatter for Subject-based observable query results.
/// </summary>
public class ObservableQueryResultFormatter : TextOutputFormatter
{
    readonly IObservableQueryHandler _observableQueryHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="ObservableQueryResultFormatter"/> class.
    /// </summary>
    /// <param name="observableQueryHandler">The observable query handler.</param>
    public ObservableQueryResultFormatter(IObservableQueryHandler observableQueryHandler)
    {
        _observableQueryHandler = observableQueryHandler;
        SupportedMediaTypes.Add("application/json");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    /// <inheritdoc/>
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        if (context.Object is null)
        {
            return false;
        }

        var objectType = context.Object.GetType();
        return objectType.ImplementsOpenGeneric(typeof(ISubject<>));
    }

    /// <inheritdoc/>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        if (context.Object is null || !CanWriteResult(context))
        {
            return;
        }

        var httpRequestContext = new AspNetCoreHttpRequestContext(context.HttpContext);

        await _observableQueryHandler.HandleStreamingResult(
            httpRequestContext,
            new QueryName(string.Empty),
            context.Object);
    }
}
