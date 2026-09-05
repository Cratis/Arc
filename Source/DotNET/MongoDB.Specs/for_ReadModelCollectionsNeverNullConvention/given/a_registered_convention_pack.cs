// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries.ModelBound;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;

/// <summary>
/// Registers the convention the way <see cref="MongoDBDefaults"/> does — scoped to <c>[ReadModel]</c> types — narrowed
/// to this namespace so the rest of the assembly is unaffected by the process-wide convention registry.
/// </summary>
/// <remarks>
/// A class map freezes its conventions the first time it is built, so the registration has to happen before anything
/// touches these types and exactly once for the whole run.
/// </remarks>
public class a_registered_convention_pack : Specification
{
    static bool _registered;

    void Establish()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;

        var pack = new ConventionPack { new ReadModelCollectionsNeverNullConvention() };
        ConventionRegistry.Register(
            ConventionPacks.ReadModelCollectionsNeverNull,
            pack,
            type => type.IsReadModel() && type.Namespace == typeof(Child).Namespace);
    }

    /// <summary>
    /// Resolves the element name a member is stored under.
    /// </summary>
    /// <param name="memberName">Name of the member.</param>
    /// <returns>The element name from the class map.</returns>
    /// <remarks>
    /// Documents are built through the class map rather than by hand, because other specs in this assembly register
    /// naming conventions process-wide and a hard-coded <c>"Children"</c> would stop matching the moment one applied.
    /// </remarks>
    protected static string ElementNameFor(string memberName) =>
        BsonClassMap.LookupClassMap(typeof(ReadModelWithEveryCollectionShape)).GetMemberMap(memberName).ElementName;
}
