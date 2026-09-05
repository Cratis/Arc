// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type React from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { RadioButtonField, RadioGroupField } from '../fields';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

const CustomLayout = ({ children }: { children: React.ReactNode }) => (
    <section data-testid='custom-radio-layout'>{children}</section>
);

const CommandProbe = ({ capture }: { capture: (command: TestCommand) => void }) => {
    capture(useCommandInstance<TestCommand>());
    return null;
};

describe(
    'when radio fields are nested in custom layout',
    given(a_command_form_context, (context) => {
        let capturedCommand: TestCommand;
        let radioButtons: HTMLInputElement[];

        beforeEach(async () => {
            const result = render(
                <CommandForm
                    command={TestCommand}
                    currentValues={{ name: 'First', email: 'Primary' }}
                >
                    <CommandProbe capture={(command) => (capturedCommand = command)} />
                    <CustomLayout>
                        <RadioButtonField
                            value={(command: TestCommand) => command.name}
                            setValue='First'
                            label='First name option'
                        />
                        <RadioButtonField
                            value={(command: TestCommand) => command.name}
                            setValue='Second'
                            label='Second name option'
                        />
                        <RadioGroupField
                            value={(command: TestCommand) => command.email}
                            options={[
                                { value: 'Primary', label: 'Primary email option' },
                                { value: 'Secondary', label: 'Secondary email option' },
                            ]}
                        />
                    </CustomLayout>
                </CommandForm>,
                { wrapper: context.createWrapper() },
            );

            radioButtons = result.getAllByRole('radio') as HTMLInputElement[];
            await waitFor(() => radioButtons[0].checked.should.equal(true));
            fireEvent.click(radioButtons[1]);
            fireEvent.click(radioButtons[3]);
        });

        it('should bind the nested radio button field', () => {
            capturedCommand.name!.should.equal('Second');
            radioButtons[1].checked.should.equal(true);
        });

        it('should bind the nested radio group field', () => {
            capturedCommand.email!.should.equal('Secondary');
            radioButtons[3].checked.should.equal(true);
        });
    }),
);
