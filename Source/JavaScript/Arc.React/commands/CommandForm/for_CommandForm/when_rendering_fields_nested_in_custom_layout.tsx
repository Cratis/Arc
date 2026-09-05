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

const Section = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='section'>
        <h2 data-testid='heading'>Contact details</h2>
        {children}
        <hr data-testid='divider' />
    </section>
);

const SectionRow = ({ children }: { children: React.ReactNode }) => (
    <div data-testid='section-row'>{children}</div>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when rendering fields nested in custom layout',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let container: HTMLElement;
        let fieldChange: sinon.SinonSpy;

        beforeEach(async () => {
            fieldChange = sinon.spy();
            const result = render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{ name: 'Initial name' }}
                    onFieldChange={fieldChange}
                >
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <Section>
                        <>
                            {[
                                <span key='intro' data-testid='intro'>
                                    Complete every field
                                </span>,
                                <SectionRow key='row'>
                                    <TestField<TestCommand>
                                        value={(command) => command.name}
                                        testId='name-field'
                                    />
                                </SectionRow>,
                            ]}
                            {null}
                        </>
                    </Section>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="name-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('Initial name');
            });

            fireEvent.change(container.querySelector('[data-testid="name-field"]')!, {
                target: { value: 'Edited name' },
            });
        });

        it('should bind the current value through both custom layout levels', () => {
            (
                container.querySelector('[data-testid="name-field"]') as HTMLInputElement
            ).value.should.equal('Edited name');
        });

        it('should update the command', () => {
            capturedCommand.name!.should.equal('Edited name');
        });

        it('should notify the field change callback', () => {
            fieldChange.calledOnce.should.equal(true);
            fieldChange.firstCall.args[1].should.equal('name');
            fieldChange.firstCall.args[3].should.equal('Edited name');
        });

        it('should preserve the nested layout elements in their original order', () => {
            const section = container.querySelector('[data-testid="section"]')!;
            const heading = container.querySelector('[data-testid="heading"]')!;
            const intro = container.querySelector('[data-testid="intro"]')!;
            const row = container.querySelector('[data-testid="section-row"]')!;
            const divider = container.querySelector('[data-testid="divider"]')!;
            section.contains(heading).should.equal(true);
            section.contains(intro).should.equal(true);
            section.contains(row).should.equal(true);
            section.contains(divider).should.equal(true);
            (
                heading.compareDocumentPosition(intro) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                intro.compareDocumentPosition(row) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
            (
                row.compareDocumentPosition(divider) & Node.DOCUMENT_POSITION_FOLLOWING
            ).should.not.equal(0);
        });
    }),
);
