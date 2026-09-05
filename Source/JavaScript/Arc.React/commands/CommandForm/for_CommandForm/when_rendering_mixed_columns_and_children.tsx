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

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when rendering mixed columns and top level children',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let container: HTMLElement;
        let fieldChange: sinon.SinonSpy;

        beforeEach(async () => {
            fieldChange = sinon.spy();
            const result = render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{
                        name: 'Before',
                        email: 'before@example.com',
                        age: 20,
                    }}
                    onFieldChange={fieldChange}
                >
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <span key='before' data-testid='before-content'>
                        Before content
                    </span>
                    <TestField<TestCommand>
                        key='name'
                        value={(command) => command.name}
                        testId='name-field'
                    />
                    <span key='middle' data-testid='middle-content'>
                        Middle content
                    </span>
                    <CommandForm.Column key='email-column'>
                        <TestField<TestCommand>
                            value={(command) => command.email}
                            testId='email-field'
                        />
                    </CommandForm.Column>
                    <CommandForm.Column key='age-column'>
                        <span data-testid='column-content'>Column content</span>
                        <TestField<TestCommand>
                            value={(command) => command.age}
                            testId='age-field'
                        />
                    </CommandForm.Column>
                    <span key='after' data-testid='after-content'>
                        After content
                    </span>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="name-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('Before');
            });

            fireEvent.change(container.querySelector('[data-testid="name-field"]')!, {
                target: { value: 'After' },
            });
            fireEvent.change(container.querySelector('[data-testid="email-field"]')!, {
                target: { value: 'after@example.com' },
            });
            fireEvent.change(container.querySelector('[data-testid="age-field"]')!, {
                target: { value: '21' },
            });
        });

        it('should preserve every top level child in its original order', () => {
            const before = container.querySelector('[data-testid="before-content"]')!;
            const name = container.querySelector('[data-testid="name-field"]')!;
            const middle = container.querySelector('[data-testid="middle-content"]')!;
            const email = container.querySelector('[data-testid="email-field"]')!;
            const age = container.querySelector('[data-testid="age-field"]')!;
            const after = container.querySelector('[data-testid="after-content"]')!;

            (
                before.compareDocumentPosition(name) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                name.compareDocumentPosition(middle) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                middle.compareDocumentPosition(email) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                email.compareDocumentPosition(age) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                age.compareDocumentPosition(after) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
        });

        it('should keep one stable mixed-content column shell', () => {
            container.querySelectorAll('.card').should.have.lengthOf(1);
            const card = container.querySelector('.card')!;
            card.classList.contains('flex').should.equal(true);
            card.classList.contains('md:flex-row').should.equal(true);
            card.classList.contains('flex-wrap').should.equal(true);
            card.querySelectorAll('.flex-1').should.have.lengthOf(2);
            (card.querySelector('[data-testid="column-content"]') !== null).should.equal(
                true,
            );
        });

        it('should bind direct and column fields', () => {
            capturedCommand.name!.should.equal('After');
            capturedCommand.email!.should.equal('after@example.com');
            capturedCommand.age!.should.equal('21');
            fieldChange.callCount.should.equal(3);
        });
    }),
);
