// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_finding_by_id;

public class with_existing_document : Specification
{
    IMongoCollection<TestDocument> _collection;
    TestDocument _document;
    Guid _id;
    TestDocument _result;

    void Establish()
    {
        _id = Guid.NewGuid();
        _document = new TestDocument(_id, "Test", 42);
        _collection = Substitute.For<IMongoCollection<TestDocument>>();

        var cursor = Substitute.For<IAsyncCursor<TestDocument>>();
        cursor.Current.Returns([_document]);
        cursor.MoveNext(Arg.Any<CancellationToken>()).Returns(true, false);

        _collection
            .FindSync(Arg.Any<FilterDefinition<TestDocument>>(), Arg.Any<FindOptions<TestDocument, TestDocument>>(), Arg.Any<CancellationToken>())
            .Returns(cursor);
    }

    void Because() => _result = _collection.FindById(_id);

    [Fact] void should_return_the_document() => _result.ShouldEqual(_document);
}
