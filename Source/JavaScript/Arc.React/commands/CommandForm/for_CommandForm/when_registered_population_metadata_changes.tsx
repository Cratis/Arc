// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import type sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { FakePopulateQuery } from '../for_usePopulateFromQuery/FakePopulateQuery';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string> {
    testId: string;
}

const TestField = asCommandFormField<TestFieldProps>(
    (props: TestFieldProps) => (
        <input data-testid={props.testId} value={props.value} onChange={props.onChange} />
    ),
    {
        defaultValue: '',
        extractValue: (event) =>
            (event as React.ChangeEvent<HTMLInputElement>).target.value,
    },
);

let nameInitialValueCallCount = 0;
let emailInitialValueCallCount = 0;

const TransformingLayout = ({
    children,
    useEmail,
}: {
    children: React.ReactElement;
    useEmail: boolean;
}) =>
    React.cloneElement(
        children as React.ReactElement<{
            value: (command: TestCommand) => unknown;
            initialValue: (source: unknown) => unknown;
        }>,
        useEmail
            ? {
                  value: (command: TestCommand) => command.email,
                  initialValue: (source: unknown) => {
                      emailInitialValueCallCount++;
                      return `Email: ${(source as { email: string }).email}`;
                  },
              }
            : {
                  value: (command: TestCommand) => command.name,
                  initialValue: (source: unknown) => {
                      nameInitialValueCallCount++;
                      return `Name: ${(source as { name: string }).name}`;
                  },
              },
    );

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

const TransformableForm = ({ capture }: { capture: (command: TestCommand) => void }) => {
    const [useEmail, setUseEmail] = React.useState(false);
    return (
        <>
            <button type='button' onClick={() => setUseEmail(true)}>
                Use email
            </button>
            <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                <CommandProbe capture={capture} />
                <TransformingLayout useEmail={useEmail}>
                    <TestField<TestCommand>
                        value={(command) => command.name}
                        testId='transformed-field'
                    />
                </TransformingLayout>
            </CommandForm>
        </>
    );
};

describe(
    'when registered population metadata changes',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let result: ReturnType<typeof render>;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            nameInitialValueCallCount = 0;
            emailInitialValueCallCount = 0;
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: { name: 'Jane', email: 'jane@example.com' },
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

            result = render(
                <TransformableForm capture={(instance) => (command = instance)} />,
                { wrapper: context.createWrapper() },
            );
            await waitFor(() => command.name!.should.equal('Name: Jane'));

            fireEvent.change(result.getByTestId('transformed-field'), {
                target: { value: 'User name edit' },
            });
            fireEvent.click(result.getByRole('button', { name: 'Use email' }));
            await waitFor(() => command.email!.should.equal('Email: jane@example.com'));
        });

        afterEach(() => fetchHelper.restore());

        it('should apply the changed accessor and initialValue', () => {
            command.email!.should.equal('Email: jane@example.com');
            emailInitialValueCallCount.should.equal(1);
        });

        it('should preserve an unrelated edit', () => {
            command.name!.should.equal('User name edit');
        });

        it('should evaluate each transformed source once', () => {
            nameInitialValueCallCount.should.equal(1);
            emailInitialValueCallCount.should.equal(1);
        });
    }),
);
