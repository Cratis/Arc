// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.for_TypeScriptContentCombiner.when_combining;

public class with_import_for_type_defined_in_same_file : Specification
{
#pragma warning disable MA0136 // Raw String contains an implicit end of line character
    const string EventContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';
        import { ProposedConsultantEntry } from './ProposedConsultantEntry';

        export class ConsultantProposedForMission {
            @field(String)
            consultant!: string;
        }
        """;

    const string EntryContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';

        export class ProposedConsultantEntry {
            @field(String)
            consultant!: string;
        }
        """;

    const string StateContent = """
        /*---------------------------------------------------------------------------------------------
         *  **DO NOT EDIT** - This file is an automatically generated file.
         *--------------------------------------------------------------------------------------------*/

        /* eslint-disable sort-imports */
        // eslint-disable-next-line header/header
        import { field } from '@cratis/fundamentals';
        import { ProposedConsultantEntry } from './ProposedConsultantEntry';

        export class ProposedConsultantsState {
            @field(String)
            id!: string;

            @field(ProposedConsultantEntry, true)
            consultants!: ProposedConsultantEntry[];
        }
        """;
#pragma warning restore MA0136 // Raw String contains an implicit end of line character

    string _result = null!;

    void Because() => _result = TypeScriptContentCombiner.Combine([EventContent, EntryContent, StateContent]);

    [Fact] void should_not_import_type_defined_in_same_file() => _result.ShouldNotContain("import { ProposedConsultantEntry }");
    [Fact] void should_keep_external_import() => _result.ShouldContain("import { field } from '@cratis/fundamentals';");
    [Fact] void should_contain_event_export() => _result.ShouldContain("export class ConsultantProposedForMission");
    [Fact] void should_contain_entry_export() => _result.ShouldContain("export class ProposedConsultantEntry");
    [Fact] void should_contain_state_export() => _result.ShouldContain("export class ProposedConsultantsState");
}
