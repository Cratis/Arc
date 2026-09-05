// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { CommandFormFieldWrapper } from '../CommandFormFields';
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

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when a field is explicitly wrapped',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let consoleWarning: sinon.SinonStub;
        let container: HTMLElement;

        beforeEach(async () => {
            setCommandFormDevelopmentWarningsForTesting(true);
            consoleWarning = sinon.stub(console, 'warn');
            const result = render(
                <CommandForm command={TestCommand} currentValues={{ name: 'Before' }}>
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <CommandFormFieldWrapper
                        field={
                            <TestField<TestCommand>
                                value={(command) => command.name}
                                testId='explicit-field'
                            />
                        }
                    />
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );
            container = result.container;

            await waitFor(() => {
                (
                    container.querySelector(
                        '[data-testid="explicit-field"]',
                    ) as HTMLInputElement
                ).value.should.equal('Before');
            });
            fireEvent.change(container.querySelector('[data-testid="explicit-field"]')!, {
                target: { value: 'After' },
            });
        });

        afterEach(() => {
            setCommandFormDevelopmentWarningsForTesting(undefined);
            consoleWarning.restore();
        });

        it('should render one input and one field container', () => {
            container
                .querySelectorAll('[data-testid="explicit-field"]')
                .should.have.lengthOf(1);
            container.querySelectorAll('.w-full').should.have.lengthOf(1);
        });

        it('should bind without a warning', () => {
            capturedCommand.name!.should.equal('After');
            consoleWarning.notCalled.should.equal(true);
        });
    }),
);
