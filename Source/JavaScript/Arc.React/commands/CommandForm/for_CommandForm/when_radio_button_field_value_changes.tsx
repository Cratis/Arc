// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { fireEvent, render } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { RadioButtonField } from '../fields';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe('when radio button field value changes', given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let firstOptionChecked = false;
    let secondOptionChecked = false;

    beforeEach(() => {
        const TestComponent = () => {
            capturedCommand = useCommandInstance<TestCommand>();
            return React.createElement('div');
        };

        const result = render(
            <CommandForm command={TestCommand}>
                <TestComponent />
                <RadioButtonField
                    value={(c: TestCommand) => c.name}
                    setValue="First option"
                    label="First option"
                />
                <RadioButtonField
                    value={(c: TestCommand) => c.name}
                    setValue="Second option"
                    label="Second option"
                />
            </CommandForm>,
            { wrapper: context.createWrapper() }
        );

        const inputs = result.getAllByRole('radio') as HTMLInputElement[];
        fireEvent.click(inputs[1]);

        firstOptionChecked = inputs[0].checked;
        secondOptionChecked = inputs[1].checked;
    });

    it('should update command instance', () => {
        capturedCommand!.name!.should.equal('Second option');
    });

    it('should check the selected option only', () => {
        firstOptionChecked.should.equal(false);
        secondOptionChecked.should.equal(true);
    });
}));
