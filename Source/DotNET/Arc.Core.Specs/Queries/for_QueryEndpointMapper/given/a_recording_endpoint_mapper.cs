// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_QueryEndpointMapper.given;

/// <summary>
/// A hand-written <see cref="IEndpointMapper"/> that records the endpoints it is asked to map.
/// Used instead of a substitute because the mapper invokes <see cref="IEndpointMapper.MapMethod"/>,
/// a default interface method that mocking frameworks cannot proxy.
/// </summary>
public class a_recording_endpoint_mapper : IEndpointMapper
{
    public List<MappedEndpoint> Mapped { get; } = [];
    public HashSet<string> ExistingEndpoints { get; } = new(StringComparer.OrdinalIgnoreCase);

    public void MapGet(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        MapMethod("GET", pattern, handler, metadata);

    public void MapPost(string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        MapMethod("POST", pattern, handler, metadata);

    public void MapMethod(string httpMethod, string pattern, Func<IHttpRequestContext, Task> handler, EndpointMetadata? metadata = null) =>
        Mapped.Add(new MappedEndpoint(httpMethod, pattern, metadata));

    public bool EndpointExists(string name) => ExistingEndpoints.Contains(name);

    public int CountFor(string httpMethod) => Mapped.Count(m => m.HttpMethod == httpMethod);

    public record MappedEndpoint(string HttpMethod, string Pattern, EndpointMetadata? Metadata);
}
