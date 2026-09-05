// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
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
        <input
            data-testid={props.testId}
            value={props.value}
            onChange={(event) => props.onChange(event)}
        />
    ),
    {
        defaultValue: '',
        extractValue: (event) =>
            (event as React.ChangeEvent<HTMLInputElement>).target.value,
    },
);

const Layout = ({ children }: { children: React.ReactNode }) => (
    <section>{children}</section>
);
const LayoutRow = ({ children }: { children: React.ReactNode }) => <div>{children}</div>;

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when extracting values from nested fields',
    given(a_command_form_context, (context) => {
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
        let capturedCommand: TestCommand;
        let deriveAge: sinon.SinonSpy;

        beforeEach(async () => {
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: { name: 'Jane Austen', email: 'jane@example.com' },
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
            deriveAge = sinon.spy(
                (source: { name?: string }) => source.name?.length ?? 0,
            );

            render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{ email: 'Current email' }}
                    populateFromQuery={FakePopulateQuery}
                >
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <Layout>
                        <LayoutRow>
                            <TestField<TestCommand>
                                value={(command) => command.name}
                                currentValue='Initial name'
                                noInitialValue
                                testId='name-field'
                            />
                            <TestField<TestCommand>
                                value={(command) => command.email}
                                noInitialValue
                                testId='email-field'
                            />
                            <TestField<TestCommand>
                                value={(command) => command.age}
                                initialValue={deriveAge}
                                testId='age-field'
                            />
                        </LayoutRow>
                    </Layout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            await waitFor(() => capturedCommand.age!.should.equal('Jane Austen'.length));
        });

        afterEach(() => fetchHelper.restore());

        it('should extract a field current value as an initial value', () => {
            capturedCommand.name!.should.equal('Initial name');
        });

        it('should apply current values to the nested field', () => {
            capturedCommand.email!.should.equal('Current email');
        });

        it('should populate the nested field from the query', () => {
            capturedCommand.age!.should.equal('Jane Austen'.length);
        });
    }),
);
