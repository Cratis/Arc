// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_TypeExtensions.when_getting_an_import_statement;

public class and_the_type_is_generic : Specification
{
    ImportStatement _result;

    void Because() => _result = typeof(KeyValuePair<string, string>).GetImportStatement("/output", "Commands", 0);

    [Fact] void should_use_the_arity_free_generated_file_name() => Assert.EndsWith("/KeyValuePair", _result.Module);
    [Fact] void should_not_use_the_clr_generic_arity() => _result.Module.ShouldNotContain('`');
}
