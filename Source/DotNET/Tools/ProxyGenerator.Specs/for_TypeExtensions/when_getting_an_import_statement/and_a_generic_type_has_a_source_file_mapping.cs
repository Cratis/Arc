// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_an_import_statement;

public class and_a_generic_type_has_a_source_file_mapping : Specification
{
    ImportStatement _result;

    void Establish() => TypeExtensions.SetSourceFileMap(new Dictionary<string, string> { [typeof(KeyValuePair<string, string>).FullName!] = "Entries" });

    void Because() => _result = typeof(KeyValuePair<string, string>).GetImportStatement("/output", "System/Collections/Generic", 0);

    [Fact] void should_use_the_exact_mapped_module_name() => _result.Module.ShouldEqual("./Entries");
    [Fact] void should_keep_the_arity_free_imported_symbol() => _result.Type.ShouldEqual("KeyValuePair");

    void Destroy() => TypeExtensions.SetSourceFileMap(null);
}
