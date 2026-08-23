// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface TestFieldProps extends WrappedFieldProps<string> {
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

const ColumnSection = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='column-section'>{children}</section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when rendering nested fields in a column',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let container: HTMLElement;
        let fieldChange: sinon.SinonSpy;

        beforeEach(async () => {
            fieldChange = sinon.spy();
            const result = render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{ email: 'before@example.com' }}
                    onFieldChange={fieldChange}
                >
                    <CommandForm.Column>
                        <CommandProbe
                            capture={(command) => (capturedCommand = command)}
                        />
                        <h3 data-testid='column-heading'>Email</h3>
                        <ColumnSection>
                            <span data-testid='column-help'>Used for notifications</span>
                            <TestField<TestCommand>
                                value={(command) => command.email}
                                testId='email-field'
                            />
                        </ColumnSection>
                    </CommandForm.Column>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="email-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('before@example.com');
            });

            fireEvent.change(container.querySelector('[data-testid="email-field"]')!, {
                target: { value: 'after@example.com' },
            });
        });

        it('should preserve the existing column shells', () => {
            const card = container.querySelector('.card')!;
            card.classList.contains('flex').should.equal(true);
            card.classList.contains('md:flex-row').should.equal(true);
            const column = card.querySelector('.flex-1')!;
            column.classList.contains('flex-column').should.equal(true);
            column.classList.contains('gap-3').should.equal(true);
        });

        it('should preserve non-field descendants in order', () => {
            const heading = container.querySelector('[data-testid="column-heading"]')!;
            const help = container.querySelector('[data-testid="column-help"]')!;
            const field = container.querySelector('[data-testid="email-field"]')!;
            (
                heading.compareDocumentPosition(help) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                help.compareDocumentPosition(field) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
        });

        it('should update the command through the nested column field', () => {
            capturedCommand.email!.should.equal('after@example.com');
        });

        it('should notify the field change callback once', () => {
            fieldChange.calledOnce.should.equal(true);
        });
    }),
);
