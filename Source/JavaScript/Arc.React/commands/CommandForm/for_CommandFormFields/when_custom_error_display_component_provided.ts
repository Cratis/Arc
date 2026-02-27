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

interface ErrorDisplayProps {
    errors: string[];
    fieldName?: string;
}

const CustomErrorDisplay = (props: ErrorDisplayProps) => {
    return React.createElement('div', {
        'data-testid': 'custom-error',
        'data-field': props.fieldName,
        className: 'custom-error-display'
    }, props.errors.map((error, idx) => 
        React.createElement('span', { key: idx, className: 'error-item' }, error)
    ));
};

describe("when custom error display component provided", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    errorDisplayComponent: CustomErrorDisplay,
                    onFieldValidate: (_cmd, fieldName) => {
                        if (fieldName === 'name') {
                            return 'Name is invalid';
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

        // Trigger validation by changing the field
        const input = container.querySelector('[data-testid="text-input"]') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'test' } });
    });

    it("should use custom error display component", () => {
        const customError = container.querySelector('[data-testid="custom-error"]');
        (customError !== null).should.be.true;
    });

    it("should pass field name to custom error display", () => {
        const customError = container.querySelector('[data-testid="custom-error"]');
        customError!.getAttribute('data-field')!.should.equal('name');
    });

    it("should pass errors array to custom error display", () => {
        const errorItems = container.querySelectorAll('.error-item');
        errorItems.length.should.equal(1);
        errorItems[0].textContent!.should.equal('Name is invalid');
    });
}));
