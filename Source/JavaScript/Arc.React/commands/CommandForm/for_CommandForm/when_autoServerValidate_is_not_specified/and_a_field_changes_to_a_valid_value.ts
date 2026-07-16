// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, fireEvent } from '@testing-library/react';
import { CommandForm } from '../../CommandForm';
import { asCommandFormField } from '../../asCommandFormField';
import { Command, CommandValidator } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from '../given/a_command_form_context';
import { given } from '../../../../given';
import { vi } from 'vitest';

let serverValidateCallCount = 0;

class ValidatedCommandValidator extends CommandValidator<ValidatedCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
    }
}

class ValidatedCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new ValidatedCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false)
    ];

    name = '';

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

describe("when autoServerValidate is not specified and a field changes to a valid value", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(() => {
        serverValidateCallCount = 0;

        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            if (url.toString().includes('/validate')) {
                serverValidateCallCount++;
            }
            return new Response(JSON.stringify({}), {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            });
        });

        result = render(
            React.createElement(
                CommandForm,
                { command: ValidatedCommand },
                React.createElement(SimpleTextField, {
                    value: (c: ValidatedCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("should not contact the server on change or blur", async () => {
        const input = result.getByTestId('text-input') as HTMLInputElement;
        fireEvent.change(input, { target: { value: 'John Doe' } });
        fireEvent.blur(input);

        await new Promise(resolve => setTimeout(resolve, 100));
        serverValidateCallCount.should.equal(0);
    });
}));
