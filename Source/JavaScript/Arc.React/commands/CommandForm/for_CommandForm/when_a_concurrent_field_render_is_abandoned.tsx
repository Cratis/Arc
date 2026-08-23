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

const suspendedRender = new Promise<never>(() => undefined);

const SuspendAbandonedRender = ({
    shouldSuspend,
    observed,
}: {
    shouldSuspend: boolean;
    observed: () => void;
}) => {
    if (shouldSuspend) {
        observed();
        throw suspendedRender;
    }
    return null;
};

const ConcurrentForm = ({
    capture,
    deriveInitialName,
    abandonedRenderObserved,
}: {
    capture: (command: TestCommand) => void;
    deriveInitialName: (prefix: string, source: FakePopulateQueryResult) => string;
    abandonedRenderObserved: () => void;
}) => {
    const [prefix, setPrefix] = React.useState('Committed');
    const [shouldSuspend, setShouldSuspend] = React.useState(false);

    return (
        <>
            <button
                type='button'
                onClick={() => {
                    React.startTransition(() => {
                        setPrefix('Abandoned');
                        setShouldSuspend(true);
                    });
                }}
            >
                Start abandoned render
            </button>
            <React.Suspense fallback={<span data-testid='fallback'>Loading</span>}>
                <CommandForm
                    command={TestCommand}
                    populateFromObservableQuery={FakeObservablePopulateQuery}
                >
                    <CommandProbe capture={capture} />
                    <TestField<TestCommand>
                        value={
                            shouldSuspend
                                ? (command) => command.email
                                : (command) => command.name
                        }
                        initialValue={(source) =>
                            deriveInitialName(
                                prefix,
                                source as FakePopulateQueryResult,
                            )
                        }
                        testId='name-field'
                    />
                    <SuspendAbandonedRender
                        shouldSuspend={shouldSuspend}
                        observed={abandonedRenderObserved}
                    />
                </CommandForm>
            </React.Suspense>
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
    'when a concurrent field render is abandoned',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let deriveInitialName: sinon.SinonSpy;
        let abandonedRenderObserved: sinon.SinonSpy;
        let result: ReturnType<typeof render>;

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
            abandonedRenderObserved = sinon.spy();

            result = render(
                <ConcurrentForm
                    capture={(instance) => (command = instance)}
                    deriveInitialName={deriveInitialName}
                    abandonedRenderObserved={abandonedRenderObserved}
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
            await waitFor(() => command.name!.should.equal('Committed: Jane'));

            fireEvent.click(
                result.getByRole('button', { name: 'Start abandoned render' }),
            );
            await waitFor(() => abandonedRenderObserved.called.should.equal(true));

            await act(async () => {
                FakeObservablePopulateQuery.subscribeCallbacks[0](
                    populationResult('Joan'),
                );
            });
            await waitFor(() => command.name!.should.equal('Committed: Joan'));
        });

        it('should keep the callback from the last committed render', () => {
            command.name!.should.equal('Committed: Joan');
        });

        it('should evaluate only committed callbacks once per source', () => {
            deriveInitialName.callCount.should.equal(2);
        });

        it('should not publish the abandoned accessor', () => {
            (command.email === undefined).should.equal(true);
        });

        it('should not commit the suspended fallback', () => {
            (result.queryByTestId('fallback') === null).should.equal(true);
        });
    }),
);
