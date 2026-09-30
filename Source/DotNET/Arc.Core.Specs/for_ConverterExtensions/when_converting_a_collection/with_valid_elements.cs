// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.ObjectModel;

namespace Cratis.Arc.for_ConverterExtensions.when_converting_a_collection;

public class with_valid_elements : Specification
{
    object? _array;
    object? _list;
    object? _set;
    object? _enumerable;
    object? _readOnlyList;
    object? _queue;
    object? _collection;
    object? _concepts;
    object? _timeSpans;
    object? _nullableFromValues;
    object? _empty;

    void Because()
    {
        _array = "1,2,3".ConvertTo(typeof(int[]));
        _list = "1, 2".ConvertTo(typeof(List<int>));
        _set = "1,1,2".ConvertTo(typeof(HashSet<int>));
        _enumerable = "1,2".ConvertTo(typeof(IEnumerable<int>));
        _readOnlyList = "1,2".ConvertTo(typeof(IReadOnlyList<int>));
        _queue = "1,2".ConvertTo(typeof(Queue<int>));
        _collection = "1,2".ConvertTo(typeof(Collection<int>));
        _concepts = "4,5".ConvertTo(typeof(IEnumerable<Count>));
        _timeSpans = "00:01:00,00:02:00".ConvertTo(typeof(TimeSpan[]));
        _nullableFromValues = new object?[] { 1, null }.ConvertTo(typeof(int?[]));
        _empty = string.Empty.ConvertTo(typeof(int[]));
    }

    [Fact] void should_create_an_array() => ((int[])_array!).ShouldContainOnly(1, 2, 3);
    [Fact] void should_create_a_list() => _list.ShouldBeOfExactType<List<int>>();
    [Fact] void should_trim_the_list_elements() => ((List<int>)_list!).ShouldContainOnly(1, 2);
    [Fact] void should_create_a_hash_set() => ((HashSet<int>)_set!).ShouldContainOnly(1, 2);
    [Fact] void should_satisfy_an_enumerable_with_an_array() => _enumerable.ShouldBeOfExactType<int[]>();
    [Fact] void should_satisfy_a_read_only_list_with_an_array() => _readOnlyList.ShouldBeOfExactType<int[]>();
    [Fact] void should_construct_a_queue_from_its_enumerable_constructor() => ((Queue<int>)_queue!).ShouldContainOnly(1, 2);
    [Fact] void should_fall_back_to_an_array_without_an_enumerable_constructor() => _collection.ShouldBeOfExactType<int[]>();
    [Fact] void should_create_the_concepts() => ((IEnumerable<Count>)_concepts!).ShouldContainOnly(new Count(4), new Count(5));
    [Fact] void should_convert_elements_through_the_type_converter() => ((TimeSpan[])_timeSpans!).ShouldContainOnly(TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2));
    [Fact] void should_keep_null_elements_of_a_nullable_element_type() => ((int?[])_nullableFromValues!).ShouldContainOnly(1, null);
    [Fact] void should_create_an_empty_array() => ((int[])_empty!).ShouldBeEmpty();
}
