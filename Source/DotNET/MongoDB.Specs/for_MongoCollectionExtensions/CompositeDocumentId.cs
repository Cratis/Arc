// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization.Attributes;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public record CompositeDocumentId([property: BsonElement("a")] string A, [property: BsonElement("b")] int B);
