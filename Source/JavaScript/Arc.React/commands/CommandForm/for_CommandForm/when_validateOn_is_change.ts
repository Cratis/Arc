// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

class ValidatedTestCommandValidator extends CommandValidator<ValidatedTestCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
    }
}

class ValidatedTestCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new ValidatedTestCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false)
    ];

    name = '';

    get properties(): string[] {
        return ['name'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            'data-testid': 'text-input'
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when validateOn is change", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(() => {
        result = render(
            React.createElement(
                CommandForm,
                { 
                    command: ValidatedTestCommand,
                    validateOn: 'change',
                    initialValues: { name: '' }
                },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedTestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should validate on change", async () => {
        const input = result.getByTestId('text-input') as HTMLInputElement;
        
        fireEvent.change(input, { target: { value: 'ab' } });
        
        // Wait for validation to complete
        await waitFor(() => {
            const errorElement = result.queryByText(/at least 3 characters/i);
            return errorElement !== null;
        }, { timeout: 2000 });
    });
}));
