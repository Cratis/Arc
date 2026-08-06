// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson;

namespace Cratis.Arc.MongoDB.for_ReadModelCollectionsNeverNullConvention.given;

/// <summary>
/// Provides a fully populated document for <see cref="ReadModelWithEveryCollectionShape"/> for a spec to take away
/// from, so what each one asserts about is only what it removed or nulled.
/// </summary>
public class a_read_model_document : a_registered_convention_pack
{
    protected BsonDocument _document;

    void Establish() => _document = FullyPopulated().ToBsonDocument();

    protected static ReadModelWithEveryCollectionShape FullyPopulated() =>
        new(
            "p1",
            [new Child("enumerable")],
            [new Child("optional")],
            [new Child("list")],
            [new Child("ordered")],
            [new Child("collection")],
            [new Child("array")],
            ["tag"],
            new HashSet<string> { "set" },
            new Dictionary<string, Child> { ["key"] = new("mapped") },
            "a label");
}
