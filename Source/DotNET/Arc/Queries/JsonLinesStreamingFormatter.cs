// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace Cratis.Arc.Queries;

/// <summary>
/// Represents a formatter for streaming JSON lines.
/// </summary>
public class JsonLinesStreamingFormatter : TextOutputFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonLinesStreamingFormatter"/> class.
    /// </summary>
    public JsonLinesStreamingFormatter()
    {
        SupportedMediaTypes.Add("application/json");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    /// <inheritdoc/>
    public override bool CanWriteResult(OutputFormatterCanWriteContext context) => context.Object is IAsyncEnumerable<object>;

    /// <inheritdoc/>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var response = context.HttpContext.Response;
        if (context.Object is not IAsyncEnumerable<object> asyncEnumerable)
        {
            return;
        }

        var jsonSerializerOptions = context.HttpContext.RequestServices
            .GetRequiredService<IOptions<ArcOptions>>()
            .Value
            .JsonSerializerOptions;

        // Mime type application/jsonl is not ratified as a standard yet: https://jsonlines.org - https://github.com/wardi/jsonlines/issues/19
        response.ContentType = "application/jsonl";
        await foreach (var item in asyncEnumerable.WithCancellation(context.HttpContext.RequestAborted))
        {
            var json = JsonSerializer.Serialize(item, jsonSerializerOptions);
            var bytes = Encoding.UTF8.GetBytes(json);
            await response.Body.WriteAsync(bytes);
            await response.Body.WriteAsync("\n"u8.ToArray());
        }
    }
}
