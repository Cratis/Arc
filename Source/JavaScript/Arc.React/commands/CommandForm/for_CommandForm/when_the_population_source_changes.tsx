// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResult } from '@cratis/arc/queries';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import {
    FakeObservablePopulateQuery,
    type FakePopulateQueryResult,
} from '../for_usePopulateFromQuery/FakePopulateQuery';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string> {
    testId: string;
}

const TestField = asCommandFormField<TestFieldProps>(
    (props: TestFieldProps) => (
        <input data-testid={props.testId} value={props.value} onChange={props.onChange} />
    ),
    { defaultValue: '' },
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

const SourceChangingForm = ({
    capture,
    deriveInitialName,
}: {
    capture: (command: TestCommand) => void;
    deriveInitialName: (prefix: string, source: FakePopulateQueryResult) => string;
}) => {
    const [prefix, setPrefix] = React.useState('Original');
    return (
        <>
            <button type='button' onClick={() => setPrefix('Latest')}>
                Change prefix
            </button>
            <CommandForm
                command={TestCommand}
                populateFromObservableQuery={FakeObservablePopulateQuery}
            >
                <CommandProbe capture={capture} />
                <TestField<TestCommand>
                    value={(command) => command.name}
                    initialValue={(source) =>
                        deriveInitialName(prefix, source as FakePopulateQueryResult)
                    }
                    testId='name-field'
                />
            </CommandForm>
        </>
    );
};

const populationResult = (name: string) =>
    new QueryResult<FakePopulateQueryResult>(
        {
            data: { name, email: `${name.toLowerCase()}@example.com` },
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
        },
        Object,
        false,
    );

describe(
    'when the population source changes',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let deriveInitialName: sinon.SinonSpy;
        let callsAfterClosureChange: number;

        beforeEach(async () => {
            const queryCache = new QueryInstanceCache();
            const ArcWrapper = context.createWrapper();
            const wrapper = ({ children }: { children: React.ReactNode }) => (
                <ArcWrapper>
                    <QueryInstanceCacheContext.Provider value={queryCache}>
                        {children}
                    </QueryInstanceCacheContext.Provider>
                </ArcWrapper>
            );
            FakeObservablePopulateQuery.reset();
            deriveInitialName = sinon.spy(
                (prefix: string, source: FakePopulateQueryResult) =>
                    `${prefix}: ${source.name}`,
            );

            const result = render(
                <SourceChangingForm
                    capture={(instance) => (command = instance)}
                    deriveInitialName={deriveInitialName}
                />,
                { wrapper },
            );
            await waitFor(() =>
                FakeObservablePopulateQuery.subscribeCallbacks.length.should.equal(1),
            );

            await act(async () => {
                FakeObservablePopulateQuery.subscribeCallbacks[0](
                    populationResult('Jane'),
                );
            });
            await waitFor(() => command.name!.should.equal('Original: Jane'));

            fireEvent.click(result.getByRole('button', { name: 'Change prefix' }));
            callsAfterClosureChange = deriveInitialName.callCount;

            await act(async () => {
                FakeObservablePopulateQuery.subscribeCallbacks[0](
                    populationResult('Joan'),
                );
            });
            await waitFor(() => command.name!.should.equal('Latest: Joan'));
        });

        it('should not evaluate the callback for the closure change alone', () => {
            callsAfterClosureChange.should.equal(1);
        });

        it('should evaluate the latest closure once for the changed source', () => {
            deriveInitialName.callCount.should.equal(2);
            command.name!.should.equal('Latest: Joan');
        });

        it('should preserve the changed source value as the baseline', () => {
            command.hasChanges.should.equal(false);
        });
    }),
);
