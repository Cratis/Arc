// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention;

#pragma warning disable SA1402, SA1649, CA1819, RCS1181

// Spec fixtures: several small types in one file, an array-typed collection member because that is one of the shapes
// the convention has to get right, and plain comments rather than XML docs on records with many positional members.
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public record Child(string Name);

// One member per collection shape the convention claims to support, plus the shapes it must leave alone —
// a nullable collection, a dictionary, and a string.
//
// The naming policy is process-wide state (DatabaseExtensions.SetNamingPolicy) and other specs in this assembly
// replace it with a bare substitute that answers null for every name. Once any spec has called AddCratisMongoDB,
// NamingPolicyNameConvention is registered for every type, and a class map built after both of those would have
// every element renamed to null and fail to freeze. Opting out of that one pack — through the very mechanism a
// consumer would use — keeps these fixtures independent of the order the assembly happens to run in.
[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
[ReadModel]
public record ReadModelWithEveryCollectionShape(
    string Id,
    IEnumerable<Child> Children,
    IEnumerable<Child>? OptionalChildren,
    List<Child> ChildList,
    IReadOnlyList<Child> OrderedChildren,
    ICollection<Child> ChildCollection,
    Child[] ChildArray,
    HashSet<string> Tags,
    ISet<string> TagSet,
    IDictionary<string, Child> ChildrenByName,
    string Label);

[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
[ReadModel]
public record ReadModelWithOwnDefault(string Id, [property: BsonDefaultValue(null)] IEnumerable<Child> Children);

[IgnoreConventions(NamingPolicyNameConvention.ConventionName)]
public record NotAReadModel(string Id, IEnumerable<Child> Children);

#pragma warning restore SA1402, SA1649, CA1819, RCS1181
