// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField, type WrappedFieldProps } from '../asCommandFormField';
import { setCommandFormDevelopmentWarningsForTesting } from '../commandFormRuntime';
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

const OuterLayout = ({ children }: { children: React.ReactNode }) => (
    <article data-testid='outer-layout'>{children}</article>
);

const InnerLayout = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='inner-layout'>
        <h3>Nested column</h3>
        {children}
    </section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when a column is nested in custom layout',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let consoleWarning: sinon.SinonStub;
        let container: HTMLElement;

        beforeEach(async () => {
            setCommandFormDevelopmentWarningsForTesting(true);
            consoleWarning = sinon.stub(console, 'warn');
            const result = render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{ email: 'before@example.com' }}
                >
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <OuterLayout>
                        <CommandForm.Column>
                            <InnerLayout>
                                <TestField<TestCommand>
                                    value={(command) => command.email}
                                    testId='nested-column-field'
                                />
                            </InnerLayout>
                        </CommandForm.Column>
                    </OuterLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="nested-column-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('before@example.com');
            });
            fireEvent.change(
                container.querySelector('[data-testid="nested-column-field"]')!,
                {
                    target: { value: 'after@example.com' },
                },
            );
        });

        afterEach(() => {
            setCommandFormDevelopmentWarningsForTesting(undefined);
            consoleWarning.restore();
        });

        it('should preserve both custom layouts and the column shell', () => {
            const outer = container.querySelector('[data-testid="outer-layout"]')!;
            const inner = container.querySelector('[data-testid="inner-layout"]')!;
            outer.contains(inner).should.equal(true);
            const columnShell = inner.parentElement!;
            columnShell.style.display.should.equal('flex');
            columnShell.style.flexDirection.should.equal('column');
        });

        it('should bind its nested field without warning', () => {
            capturedCommand.email!.should.equal('after@example.com');
            consoleWarning.notCalled.should.equal(true);
        });
    }),
);
