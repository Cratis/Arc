// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Concepts;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions;

public record ObservedDocumentId(Guid Value) : ConceptAs<Guid>(Value);
