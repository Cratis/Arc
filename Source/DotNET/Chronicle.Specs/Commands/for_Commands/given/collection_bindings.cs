// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Chronicle.ReadModels;
using Cratis.Arc.Commands;
using Cratis.Arc.Commands.ModelBound;
using Cratis.Arc.Queries.ModelBound;
using MongoDB.Driver;

namespace Cratis.Arc.Chronicle.Testing.Commands.for_Commands.given;

public static class collection_bindings
{
    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class OnCommand
    {
        public void Handle(IMongoCollection<a_wiring_check.Model> collection) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class NullableCommand
    {
        public void Handle(IMongoCollection<a_wiring_check.Model>? collection) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    [ReadProtection(ReadProtectionMode.Off)]
    public class OffCommand
    {
        public void Handle(IMongoCollection<a_wiring_check.Model> collection) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    [ReadProtection(ReadProtectionMode.Measure)]
    public class MeasureCommand
    {
        public void Handle(IMongoCollection<a_wiring_check.Model> collection) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class ParameterAdvisoryCommand
    {
        public void Handle([Unprotected("Reference data")] IMongoCollection<a_wiring_check.Model> collection) => throw new InvalidOperationException("Must not execute");
    }

    [ReadModel]
    [Unprotected("Reference data")]
    public record AdvisoryModel(string Id);

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class ModelAdvisoryCommand
    {
        public void Handle(IMongoCollection<AdvisoryModel> collection) => throw new InvalidOperationException("Must not execute");
    }

    public record OtherModel(string Id);

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class OtherCollectionCommand
    {
        public void Handle(IMongoCollection<OtherModel> collection) => throw new InvalidOperationException("Must not execute");
    }

    [Command]
    [ReadProtection(ReadProtectionMode.On)]
    public class QueryableCommand
    {
        public void Handle(IQueryable<a_wiring_check.Model> query) => throw new InvalidOperationException("Must not execute");
    }
}
