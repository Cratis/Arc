// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.Sinks.for_MongoDBSinks.given;

public class mongodb_sinks : Specification
{
    protected MongoDBOptions _options;
    protected IMongoDBClientFactory _clientFactory;
    protected List<MongoUrl> _createdFor;
    protected MongoDBSinks _sinks;

    void Establish()
    {
        _options = new() { Server = "mongodb://default:27017", Database = "app" };
        _createdFor = [];
        _clientFactory = Substitute.For<IMongoDBClientFactory>();
        _clientFactory.Create(Arg.Do<MongoUrl>(_createdFor.Add)).Returns(_ => Substitute.For<IMongoClient>());
        _sinks = new(Options.Create(_options), _clientFactory);
    }
}
