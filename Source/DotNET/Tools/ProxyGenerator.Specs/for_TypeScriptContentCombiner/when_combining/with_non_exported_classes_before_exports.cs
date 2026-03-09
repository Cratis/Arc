// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_non_exported_classes_before_exports : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string QueryContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { ObservableQueryFor, QueryResultWithState, Sorting, Paging } from '@cratis/arc/queries';
        import { useObservableQuery, useObservableQueryWithPaging, SetSorting, SetPage, SetPageSize } from '@cratis/arc.react/queries';
        import { ParameterDescriptor } from '@cratis/arc/reflection';

        class AllCustomersSortBy {
            constructor(readonly query: AllCustomers) {
            }
        }

        class AllCustomersSortByWithoutQuery {
        }

        export class AllCustomers extends ObservableQueryFor<Customer[]> {
            readonly route: string = '/api/customers/all-customers';
        }
        """;

    const string TypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class Customer {
            @field(String)
            name!: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([QueryContent, TypeContent]);

    [Fact] void should_place_imports_before_non_exported_classes() =>
        _result.IndexOf("import { ObservableQueryFor", StringComparison.Ordinal)
            .ShouldBeLessThan(_result.IndexOf("class AllCustomersSortBy", StringComparison.Ordinal));

    [Fact] void should_contain_sort_by_class() => _result.ShouldContain("class AllCustomersSortBy");

    [Fact] void should_contain_sort_by_without_query_class() => _result.ShouldContain("class AllCustomersSortByWithoutQuery");

    [Fact] void should_contain_export_class() => _result.ShouldContain("export class AllCustomers");

    [Fact] void should_contain_customer_export() => _result.ShouldContain("export class Customer");

    [Fact] void should_contain_field_import() => _result.ShouldContain("import { field } from '@cratis/fundamentals';");
}
