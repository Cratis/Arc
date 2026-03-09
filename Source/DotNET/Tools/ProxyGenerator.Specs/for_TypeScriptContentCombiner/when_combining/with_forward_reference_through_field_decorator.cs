// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_forward_reference_through_field_decorator : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string QueryContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { ObservableQueryFor, QueryResultWithState } from '@cratis/arc/queries';
        import { useObservableQuery } from '@cratis/arc.react/queries';
        import { ParameterDescriptor } from '@cratis/arc/reflection';
        import { field } from '@cratis/fundamentals';

        export class ConsultantHistory {
            @field(String)
            id!: string;

            @field(ProposalRecord, true)
            proposals!: ProposalRecord[];
        }
        """;

    const string ChildTypeContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class ProposalRecord {
            @field(String)
            missionProspectId!: string;
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([QueryContent, ChildTypeContent]);

    [Fact] void should_declare_child_type_before_parent() =>
        _result.IndexOf("export class ProposalRecord", StringComparison.Ordinal)
            .ShouldBeLessThan(_result.IndexOf("export class ConsultantHistory", StringComparison.Ordinal));

    [Fact] void should_contain_both_types() =>
        _result.ShouldContain("export class ConsultantHistory");

    [Fact] void should_contain_child_type() =>
        _result.ShouldContain("export class ProposalRecord");
}
