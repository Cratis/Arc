// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient.given;

public class a_shared_client : Specification
{
    protected IMongoClient _native;
    protected IMongoClient _client;
    protected IClientSessionHandle _session;
    protected CancellationToken _token;
    CancellationTokenSource _cancellation;

    void Establish()
    {
        _native = Substitute.For<IMongoClient>();
        _client = new SharedMongoClient(_native);
        _session = Substitute.For<IClientSessionHandle>();
        _cancellation = new CancellationTokenSource();
        _token = _cancellation.Token;
    }

    void Destroy() => _cancellation.Dispose();
}
