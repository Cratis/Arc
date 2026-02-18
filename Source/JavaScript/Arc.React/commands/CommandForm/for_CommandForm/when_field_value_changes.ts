// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { CommandFormField } from '../CommandFormField';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            'data-testid': 'text-input'
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when field value changes", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;

    beforeEach(() => {
        const TestComponent = () => {
            const command = useCommandInstance<TestCommand>();
            capturedCommand = command;
            return React.createElement('div');
        };

        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(TestComponent),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );

        const input = result.getByTestId('text-input') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'Updated Name' } });
    });

    it("should update command instance", () => {
        capturedCommand!.name.should.equal('Updated Name');
    });
}));
