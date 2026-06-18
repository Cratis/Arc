// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: unknown; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[]; 'data-testid'?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: (props.value ?? '') as string | number,
            onChange: props.onChange,
            'data-testid': props['data-testid']
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when initializing with current values", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let renderResult: ReturnType<typeof render>;

    beforeEach(async () => {
        const TestComponent = () => {
            const command = useCommandInstance<TestCommand>();
            capturedCommand = command;
            return React.createElement('div');
        };

        renderResult = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    currentValues: { name: 'Jane', age: 30 }
                },
                React.createElement(TestComponent),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    'data-testid': 'name-input'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.age,
                    title: 'Age',
                    'data-testid': 'age-input'
                })
            ),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => {
            expect(capturedCommand!.name).toBe('Jane');
        });
    });

    it("should set name from current values", () => {
        capturedCommand!.name!.should.equal('Jane');
    });

    it("should set age from current values", () => {
        capturedCommand!.age!.should.equal(30);
    });

    it("should render field values from current values", () => {
        const nameInput = renderResult.getByTestId('name-input') as HTMLInputElement;
        const ageInput = renderResult.getByTestId('age-input') as HTMLInputElement;

        nameInput.value.should.equal('Jane');
        ageInput.value.should.equal('30');
    });
}));
