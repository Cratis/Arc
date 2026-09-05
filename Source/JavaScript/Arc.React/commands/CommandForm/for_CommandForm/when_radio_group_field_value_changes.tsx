// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { RadioGroupField } from '../fields';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe('when radio group field value changes', given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let selectedOptionChecked = false;

    beforeEach(() => {
        const TestComponent = () => {
            capturedCommand = useCommandInstance<TestCommand>();
            return React.createElement('div');
        };

        const result = render(
            <CommandForm command={TestCommand} initialValues={{ name: 'First option' }}>
                <TestComponent />
                <RadioGroupField
                    value={(c: TestCommand) => c.name}
                    options={[
                        { value: 'First option', label: 'First option' },
                        { value: 'Second option', label: 'Second option' }
                    ]}
                />
            </CommandForm>,
            { wrapper: context.createWrapper() }
        );

        const inputs = result.getAllByRole('radio') as HTMLInputElement[];
        fireEvent.click(inputs[1]);

        selectedOptionChecked = inputs[1].checked;
    });

    it('should update command instance', () => {
        capturedCommand!.name!.should.equal('Second option');
    });

    it('should check the selected option', () => {
        selectedOptionChecked.should.equal(true);
    });
}));
