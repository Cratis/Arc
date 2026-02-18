// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
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
            'data-testid': 'text-input',
            'aria-invalid': props.invalid
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when custom validation returns error", given(a_command_form_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    onFieldValidate: (cmd, fieldName, oldValue, newValue) => {
                        if (newValue === 'invalid') {
                            return 'This value is not allowed';
                        }
                        return undefined;
                    }
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );

        container = result.container;
        const input = result.getByTestId('text-input') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'invalid' } });
    });

    it("should display error message", () => {
        const errorText = container.querySelector('small');
        errorText!.textContent.should.equal('This value is not allowed');
    });

    it("should mark field as invalid", () => {
        const input = container.querySelector('[data-testid="text-input"]') as HTMLInputElement;
        input.getAttribute('aria-invalid').should.equal('true');
    });
}));
