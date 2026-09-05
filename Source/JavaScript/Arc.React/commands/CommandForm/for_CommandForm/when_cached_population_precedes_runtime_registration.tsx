// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { render, waitFor } from '@testing-library/react';
import type sinon from 'sinon';
import {
    QueryInstanceCache,
    QueryResult,
    QueryResultWithState,
} from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { FakePopulateQuery } from '../for_usePopulateFromQuery/FakePopulateQuery';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string | number> {
    testId: string;
}

const TestField = asCommandFormField<TestFieldProps>(
    (props: TestFieldProps) => (
        <input data-testid={props.testId} value={props.value} onChange={props.onChange} />
    ),
    { defaultValue: '' },
);

const OpaqueLayout = ({ children }: { children: React.ReactNode }) => (
    <section>{children}</section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when cached population precedes runtime field registration',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            const source = {
                name: 'Cached name',
                email: 'cached@example.com',
                age: 42,
            };
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: source,
                    isSuccess: true,
                    isAuthorized: true,
                    isValid: true,
                    hasExceptions: false,
                    validationResults: [],
                    exceptionMessages: [],
                    exceptionStackTrace: '',
                    paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                }),
            } as Response);

            const cache = new QueryInstanceCache();
            const key = cache.buildKey(FakePopulateQuery.name);
            cache.getOrCreate(key, () => new FakePopulateQuery());
            cache.setLastResult(
                key,
                QueryResultWithState.fromQueryResult(
                    new QueryResult(
                        {
                            data: source,
                            isSuccess: true,
                            isReady: true,
                            isAuthorized: true,
                            isValid: true,
                            hasExceptions: false,
                            validationResults: [],
                            exceptionMessages: [],
                            exceptionStackTrace: '',
                            paging: {
                                page: 0,
                                size: 0,
                                totalItems: 0,
                                totalPages: 0,
                            },
                        },
                        Object,
                        false,
                    ),
                    false,
                ),
            );

            const ArcWrapper = context.createWrapper();
            const Wrapper = ({ children }: { children: React.ReactNode }) => (
                <ArcWrapper>
                    <QueryInstanceCacheContext.Provider value={cache}>
                        {children}
                    </QueryInstanceCacheContext.Provider>
                </ArcWrapper>
            );

            render(
                <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                    <CommandProbe capture={(instance) => (command = instance)} />
                    <OpaqueLayout>
                        <TestField<TestCommand>
                            value={(instance) => instance.name}
                            initialValue={(value) =>
                                `${(value as { email?: string }).email} transformed`
                            }
                            testId='name-field'
                        />
                        <TestField<TestCommand>
                            value={(instance) => instance.age}
                            noInitialValue
                            testId='age-field'
                        />
                    </OpaqueLayout>
                </CommandForm>,
                { wrapper: Wrapper },
            );

            await waitFor(() =>
                command.name!.should.equal('cached@example.com transformed'),
            );
        });

        afterEach(() => fetchHelper.restore());

        it('should apply transformed cached population as a pristine baseline', () => {
            command.name!.should.equal('cached@example.com transformed');
            command.hasChanges.should.equal(false);
        });

        it('should honor the population opt out', () => {
            (command.age === undefined).should.equal(true);
        });
    }),
);
