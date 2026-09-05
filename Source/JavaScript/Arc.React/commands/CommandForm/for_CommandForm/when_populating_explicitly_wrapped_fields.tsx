// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { render, waitFor } from '@testing-library/react';
import type sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { CommandFormFieldWrapper } from '../CommandFormFields';
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

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when populating explicitly wrapped fields',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: { name: 'Jane Austen', email: 'jane@example.com', age: 41 },
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

            render(
                <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <CommandFormFieldWrapper
                        field={
                            <TestField<TestCommand>
                                value={(command) => command.name}
                                currentValue='Explicit seed'
                                noInitialValue
                                testId='name-field'
                            />
                        }
                    />
                    <CommandFormFieldWrapper
                        field={
                            <TestField<TestCommand>
                                value={(command) => command.email}
                                initialValue={(source) =>
                                    `${(source as { name?: string }).name} transformed`
                                }
                                testId='email-field'
                            />
                        }
                    />
                    <CommandFormFieldWrapper
                        field={
                            <TestField<TestCommand>
                                value={(command) => command.age}
                                noInitialValue
                                testId='age-field'
                            />
                        }
                    />
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            await waitFor(() =>
                capturedCommand.email!.should.equal('Jane Austen transformed'),
            );
        });

        afterEach(() => fetchHelper.restore());

        it('should preserve the explicit field current value', () => {
            capturedCommand.name!.should.equal('Explicit seed');
        });

        it('should apply the explicit field population override', () => {
            capturedCommand.email!.should.equal('Jane Austen transformed');
        });

        it('should honor the explicit field population opt out', () => {
            (capturedCommand.age === undefined).should.equal(true);
        });
    }),
);
