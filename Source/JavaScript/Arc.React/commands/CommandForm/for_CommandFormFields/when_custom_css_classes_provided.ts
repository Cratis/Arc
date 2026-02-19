// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
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

const TestIcon = () => React.createElement('svg', { 'data-testid': 'field-icon' });

describe("when custom css classes provided", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    errorClassName: 'my-custom-error',
                    iconAddonClassName: 'my-custom-icon-addon',
                    onFieldValidate: (_cmd, fieldName) => {
                        if (fieldName === 'name') {
                            return 'Name is invalid';
                        }
                        return undefined;
                    }
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    icon: React.createElement(TestIcon)
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;

        // Trigger validation by changing the field
        const input = container.querySelector('[data-testid="text-input"]') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'test' } });
    });

    it("should apply custom error class name", () => {
        const errorElement = container.querySelector('.my-custom-error');
        (errorElement !== null).should.be.true;
    });

    it("should apply custom icon addon class name", () => {
        const iconAddon = container.querySelector('.my-custom-icon-addon');
        (iconAddon !== null).should.be.true;
    });
}));
