// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { CommandFormFieldRegistrationContext } from '../CommandFormFieldRegistrationContext';
import type { CommandFormFieldRegistrationDescriptor } from '../CommandFormFieldRegistrationDescriptor';
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

const RegistrationProbe = ({
    children,
    registered,
    changed,
}: {
    children: React.ReactNode;
    registered: () => void;
    changed: () => void;
}) => {
    useCommandInstance<TestCommand>();
    const parentRegistration = React.useContext(CommandFormFieldRegistrationContext);
    const registration = React.useMemo(
        () =>
            parentRegistration
                ? {
                      register: (
                          id: symbol,
                          descriptor: CommandFormFieldRegistrationDescriptor,
                      ) => {
                          registered();
                          parentRegistration.register(id, descriptor);
                      },
                      notifyChanged: (id: symbol) => {
                          changed();
                          parentRegistration.notifyChanged(id);
                      },
                      unregister: (id: symbol) => parentRegistration.unregister(id),
                  }
                : undefined,
        [parentRegistration, registered, changed],
    );

    return (
        <CommandFormFieldRegistrationContext.Provider value={registration}>
            {children}
        </CommandFormFieldRegistrationContext.Provider>
    );
};

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when unrelated command values change',
    given(a_command_form_context, (context) => {
        let command: TestCommand;
        let registrationCount: sinon.SinonSpy;
        let metadataChangeCount: sinon.SinonSpy;
        let deriveInitialName: sinon.SinonSpy;
        let registrationsAfterPopulation: number;
        let metadataChangesAfterPopulation: number;
        let initialValueCallsAfterPopulation: number;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

        beforeEach(async () => {
            fetchHelper = createFetchHelper();
            fetchHelper.stubFetch().resolves({
                json: async () => ({
                    data: { name: 'Populated name', email: 'populated@example.com' },
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
            registrationCount = sinon.spy();
            metadataChangeCount = sinon.spy();
            deriveInitialName = sinon.spy(
                (source: unknown) => (source as { name: string }).name,
            );

            const renderForm = () => (
                <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                    <CommandProbe capture={(instance) => (command = instance)} />
                    <RegistrationProbe
                        registered={registrationCount}
                        changed={metadataChangeCount}
                    >
                        <TestField<TestCommand>
                            value={(instance) => instance.name}
                            initialValue={(source) => deriveInitialName(source)}
                            testId='name-field'
                        />
                        <TestField<TestCommand>
                            value={(instance) => instance.email}
                            testId='email-field'
                        />
                    </RegistrationProbe>
                </CommandForm>
            );
            const result = render(renderForm(), {
                wrapper: context.createWrapper(),
            });

            await waitFor(() => command.name!.should.equal('Populated name'));
            registrationsAfterPopulation = registrationCount.callCount;
            metadataChangesAfterPopulation = metadataChangeCount.callCount;
            initialValueCallsAfterPopulation = deriveInitialName.callCount;

            result.rerender(renderForm());
            fireEvent.change(result.getByTestId('email-field'), {
                target: { value: 'User edit' },
            });
            await waitFor(() => command.email!.should.equal('User edit'));
        });

        afterEach(() => fetchHelper.restore());

        it('should not register stable field metadata again', () => {
            registrationCount.callCount.should.equal(registrationsAfterPopulation);
        });

        it('should not notify about unchanged metadata', () => {
            metadataChangeCount.callCount.should.equal(metadataChangesAfterPopulation);
        });

        it('should not evaluate initialValue again', () => {
            deriveInitialName.callCount.should.equal(initialValueCallsAfterPopulation);
            deriveInitialName.calledOnce.should.equal(true);
        });
    }),
);
