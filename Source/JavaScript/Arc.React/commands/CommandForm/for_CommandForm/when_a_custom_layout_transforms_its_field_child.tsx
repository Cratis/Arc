// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { isCommandFormField } from '../commandFormMarkers';
import { FakePopulateQuery } from '../for_usePopulateFromQuery/FakePopulateQuery';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string> {
    testId: string;
}

const TransformableInput = asCommandFormField<TestFieldProps>(
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

const TransformingLayout = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='transforming-layout'>
        {React.Children.map(children, (child) => {
            if (
                !React.isValidElement(child) ||
                !isCommandFormField(child.type as React.ComponentType<unknown>)
            ) {
                return null;
            }

            return React.cloneElement(
                child as React.ReactElement<{
                    testId: string;
                    title?: string;
                    value: (command: TestCommand) => unknown;
                }>,
                {
                    testId: 'transformed-field',
                    title: 'Transformed title',
                    value: (command: TestCommand) => command.email,
                },
            );
        })}
    </section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when a custom layout transforms its field child',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let container: HTMLElement;
        let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };

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
            const result = render(
                <CommandForm command={TestCommand} populateFromQuery={FakePopulateQuery}>
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <TransformingLayout>
                        <TransformableInput<TestCommand>
                            value={(command) => command.name}
                            testId='original-field'
                            title='Original title'
                        />
                    </TransformingLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="transformed-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('jane@example.com');
            });

            fireEvent.change(
                container.querySelector('[data-testid="transformed-field"]')!,
                {
                    target: { value: 'After' },
                },
            );
        });

        afterEach(() => fetchHelper.restore());

        it('should hand the original marked child type to the layout', () => {
            container
                .querySelectorAll('[data-testid="transformed-field"]')
                .should.have.lengthOf(1);
            container
                .querySelectorAll('[data-testid="original-field"]')
                .should.have.lengthOf(0);
        });

        it('should apply the transformed props to the actual field', () => {
            const label = container.querySelector('label');
            label!.textContent!.should.equal('Transformed title');
            capturedCommand.email!.should.equal('After');
            (capturedCommand.name === undefined).should.equal(true);
        });

        it('should render exactly one field container', () => {
            container.querySelectorAll('.w-full').should.have.lengthOf(1);
        });
    }),
);
