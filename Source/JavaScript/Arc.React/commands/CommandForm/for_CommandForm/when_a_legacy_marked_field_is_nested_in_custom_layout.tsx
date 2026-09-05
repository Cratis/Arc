// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import type { CommandFormFieldProps } from '../CommandFormField';
import { markAsCommandFormField } from '../commandFormMarkers';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

interface LegacyFieldProps extends CommandFormFieldProps<TestCommand> {
    testId: string;
}

const LegacyField = markAsCommandFormField((props: LegacyFieldProps) => (
    <input
        data-testid={props.testId}
        value={String(props.currentValue ?? '')}
        onChange={(event) => props.onValueChange?.(event.target.value)}
    />
));

const PassThroughLayout = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='legacy-layout'>{children}</section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when a legacy marked field is nested in custom layout',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let input: HTMLInputElement;

        beforeEach(async () => {
            const result = render(
                <CommandForm command={TestCommand} currentValues={{ name: 'Before' }}>
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <PassThroughLayout>
                        <LegacyField
                            value={(command) => command.name}
                            testId='legacy-field'
                        />
                    </PassThroughLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            input = result.getByTestId('legacy-field') as HTMLInputElement;
            await waitFor(() => input.value.should.equal('Before'));
            fireEvent.change(input, { target: { value: 'After' } });
        });

        it('should bind through the marker-only compatibility fallback', () => {
            capturedCommand.name!.should.equal('After');
            input.value.should.equal('After');
        });
    }),
);
