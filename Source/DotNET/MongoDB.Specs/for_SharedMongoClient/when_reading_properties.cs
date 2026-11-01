// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;
using MongoDB.Driver.Core.Clusters;

namespace Cratis.Arc.MongoDB.for_SharedMongoClient;

public class when_reading_properties : given.a_shared_client
{
    readonly MongoClientSettings _settings = new();
    ICluster _cluster;
    ICluster _resultCluster;
    MongoClientSettings _resultSettings;

    void Establish()
    {
        _cluster = Substitute.For<ICluster>();
        _native.Cluster.Returns(_cluster);
        _native.Settings.Returns(_settings);
    }

    void Because()
    {
        _resultCluster = _client.Cluster;
        _resultSettings = _client.Settings;
    }

    [Fact] void should_return_the_native_cluster() => _resultCluster.ShouldBeSame(_cluster);
    [Fact] void should_return_the_native_settings() => _resultSettings.ShouldBeSame(_settings);
    [Fact] void should_read_the_cluster_once() => _ = _native.Received(1).Cluster;
    [Fact] void should_read_the_settings_once() => _ = _native.Received(1).Settings;
}
