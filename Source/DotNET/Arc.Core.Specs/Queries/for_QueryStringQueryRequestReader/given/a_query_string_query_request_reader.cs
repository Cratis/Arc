// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Http;

namespace Cratis.Arc.Queries.for_QueryStringQueryRequestReader.given;

public class a_query_string_query_request_reader : Specification
{
    protected QueryStringQueryRequestReader _reader;
    protected IHttpRequestContext _context;
    protected IQueryPerformer _performer;

    void Establish()
    {
        _reader = new QueryStringQueryRequestReader();
        _context = Substitute.For<IHttpRequestContext>();
        _performer = Substitute.For<IQueryPerformer>();
        _performer.Parameters.Returns(new QueryParameters(
        [
            new QueryParameter("id", typeof(string))
        ]));
    }
}
