// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Servers;

namespace Cratis.Arc.MongoDB.for_MongoDBDatabaseWatch.when_classifying_resume_failures;

public class with_driver_error_codes_and_names : Specification
{
    bool[] _invalidCodes;
    bool[] _invalidNames;
    bool _transient;

    void Because()
    {
        _invalidCodes = new[] { 260, 280, 286 }.Select(code => MongoDBDatabaseWatch.HasInvalidResumeToken(Error(new BsonDocument("code", code)))).ToArray();
        _invalidNames = new[] { "InvalidResumeToken", "ChangeStreamHistoryLost", "ChangeStreamFatalError" }.Select(name => MongoDBDatabaseWatch.HasInvalidResumeToken(Error(new BsonDocument("codeName", name)))).ToArray();
        _transient = MongoDBDatabaseWatch.HasInvalidResumeToken(Error(new BsonDocument("code", 91)));
    }

    [Fact] void should_reset_each_known_invalid_token_code() => _invalidCodes.ShouldEachConformTo(invalid => invalid);
    [Fact] void should_reset_each_known_invalid_token_name() => _invalidNames.ShouldEachConformTo(invalid => invalid);
    [Fact] void should_preserve_tokens_for_transient_failures() => _transient.ShouldBeFalse();

    static MongoCommandException Error(BsonDocument result) => new(
        new ConnectionId(new ServerId(new ClusterId(), new DnsEndPoint("localhost", 27017))),
        "watch failed",
        new BsonDocument(),
        result);
}
