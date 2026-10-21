// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_an_import_statement;

public class and_source_file_grouping_is_disabled : Specification
{
    ImportStatement _result;

    void Establish()
    {
        TypeExtensions.SetSourceFileMap(new Dictionary<string, string> { [typeof(KeyValuePair<string, string>).FullName!] = "Grouped" });
        TypeExtensions.SetSourceFileMap(null);
    }

    void Because() => _result = typeof(KeyValuePair<string, string>).GetImportStatement("/output", "System/Collections/Generic", 0);

    [Fact] void should_use_the_original_arity_free_module() => _result.Module.ShouldEqual("./KeyValuePair");
    [Fact] void should_not_reuse_the_previous_source_file_map() => _result.Module.ShouldNotContain("Grouped");
}
