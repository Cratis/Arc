// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using Cratis.Concepts;

namespace Cratis.Arc.MongoDB.for_MongoDBReadModelForCommandResolver;

#pragma warning disable SA1402, SA1649 // File may only contain a single type, File name should match first type name

public record CustomerId(Guid Value) : ConceptAs<Guid>(Value)
{
    public static implicit operator Guid(CustomerId id) => id.Value;
    public static implicit operator CustomerId(Guid value) => new(value);
}

[ReadModel]
public record Customer(Guid Id, string Name);

[ReadModel]
public record Account(CustomerId Id, decimal Balance);

[ReadModel]
public record Preferences(string Theme);

public record NotAReadModel(Guid Id);

[ReadModel]
public abstract record AnAbstractReadModel(Guid Id);

#pragma warning restore SA1402, SA1649
