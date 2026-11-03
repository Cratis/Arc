// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_deriving_a_client : given.a_shared_client
{
    IMongoClient _derived;
    IMongoClient _readConcernResult;
    IMongoClient _readPreferenceResult;
    IMongoClient _writeConcernResult;

    void Establish()
    {
        _derived = Substitute.For<IMongoClient>();
        _native.WithReadConcern(ReadConcern.Majority).Returns(_derived);
        _native.WithReadPreference(ReadPreference.Secondary).Returns(_derived);
        _native.WithWriteConcern(WriteConcern.WMajority).Returns(_derived);
    }

    void Because()
    {
        _readConcernResult = _client.WithReadConcern(ReadConcern.Majority);
        _readPreferenceResult = _client.WithReadPreference(ReadPreference.Secondary);
        _writeConcernResult = _client.WithWriteConcern(WriteConcern.WMajority);
    }

    [Fact] void should_forward_read_concern() => _native.Received(1).WithReadConcern(ReadConcern.Majority);
    [Fact] void should_forward_read_preference() => _native.Received(1).WithReadPreference(ReadPreference.Secondary);
    [Fact] void should_forward_write_concern() => _native.Received(1).WithWriteConcern(WriteConcern.WMajority);
    [Fact] void should_return_the_native_read_concern_client() => _readConcernResult.ShouldBeSame(_derived);
    [Fact] void should_return_the_native_read_preference_client() => _readPreferenceResult.ShouldBeSame(_derived);
    [Fact] void should_return_the_native_write_concern_client() => _writeConcernResult.ShouldBeSame(_derived);
}
