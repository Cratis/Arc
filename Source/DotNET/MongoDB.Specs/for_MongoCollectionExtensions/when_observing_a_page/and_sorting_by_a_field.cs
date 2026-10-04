// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Bson.Serialization;

namespace Cratis.Arc.MongoDB.for_MongoCollectionExtensions.when_observing_a_page;

public class and_sorting_by_a_field : given.a_server_backed_observation
{
    string[] _sortFields;
    int[] _sortDirections;

    void Establish() => PageByName(0, 2);

    async Task Because()
    {
        await StartObserving();
        var sort = RenderedSort()!;
        _sortFields = [.. sort.Names];
        _sortDirections = [.. sort.Values.Select(_ => _.ToInt32())];
    }

    /// <summary>
    /// Gets the element name the class map assigned to the name - other specs in this assembly install naming policies
    /// process-wide, so the name the document is stored under depends on which ran first.
    /// </summary>
    static string NameElement => BsonClassMap.LookupClassMap(typeof(ObservedDocument)).GetMemberMap(nameof(ObservedDocument.Name)).ElementName;

    [Fact] void should_sort_by_the_field_first() => _sortFields[0].ShouldEqual(NameElement);
    [Fact] void should_break_ties_on_the_id() => Assert.Equal([NameElement, "_id"], _sortFields);
    [Fact] void should_break_ties_in_ascending_order() => Assert.Equal([1, 1], _sortDirections);
}
