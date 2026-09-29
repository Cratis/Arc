// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain.given;
using Cratis.Arc.for_JsonSerializerOptionsConfiguration.given;

namespace Cratis.Arc.for_ArcOptions.when_appending_to_the_type_info_resolver_chain;

/// <summary>
/// A copy of Arc's options carries Arc's resolver too. A resolver appended to the copy wins for the copy, as it would
/// for Arc's options, and leaves Arc's options alone.
/// </summary>
public class of_a_copy_of_the_options : Specification
{
    ArcOptions _arcOptions;
    JsonSerializerOptions _copy;
    string _jsonOfCopy;
    string _jsonOfOriginal;

    void Establish()
    {
        _arcOptions = new ArcOptions();
        _copy = new JsonSerializerOptions(_arcOptions.JsonSerializerOptions);
    }

    void Because()
    {
        _copy.TypeInfoResolverChain.Add(renaming_resolvers.RenamingQueryResultData("payload"));
        _jsonOfCopy = JsonSerializer.Serialize(representative_wire_values.QueryResult, _copy);
        _jsonOfOriginal = JsonSerializer.Serialize(representative_wire_values.QueryResult, _arcOptions.JsonSerializerOptions);
    }

    [Fact] void should_apply_the_appended_resolver_to_the_copy() => _jsonOfCopy.ShouldContain("\"payload\":");
    [Fact] void should_not_apply_it_to_the_original() => _jsonOfOriginal.ShouldNotContain("\"payload\":");
}
