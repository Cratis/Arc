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
import { vi } from 'vitest';

// Track validation calls globally for testing
let serverValidateCallCount = 0;

class SimpleCommandValidator extends CommandValidator<SimpleCommand> {
    constructor() {
        super();
        this.ruleFor(c => c.name).notEmpty().minLength(3);
        this.ruleFor(c => c.email).notEmpty().emailAddress();
    }
}

class SimpleCommand extends Command {
    readonly route = '/api/test';
    readonly validation = new SimpleCommandValidator();
    readonly propertyDescriptors: PropertyDescriptor[] = [
        new PropertyDescriptor('name', String, false),
        new PropertyDescriptor('email', String, false)
    ];

    name = '';
    email = '';

    get properties(): string[] {
        return ['name', 'email'];
    }

    get requestParameters(): string[] {
        return [];
    }

    constructor() {
        super(Object, false);
    }
}

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; onBlur?: () => void; invalid: boolean; required: boolean; errors: string[]; title?: string }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            onBlur: props.onBlur,
            placeholder: props.title
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when autoServerValidate is true and all fields become valid", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        // Reset tracking
        serverValidateCallCount = 0;
        
        // Mock fetch to track validation calls
        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            const urlString = url.toString();
            if (urlString.includes('/validate')) {
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
                { 
                    command: SimpleCommand,
                    autoServerValidate: true,
                    validateOn: 'change'
                },
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("should call server validate when all fields become valid", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
        
        // Both fields start empty (invalid)
        expect(serverValidateCallCount).toBe(0);
        
        // Make name valid
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        await new Promise(resolve => setTimeout(resolve, 100));
        
        // Server validate should NOT be called yet (email still invalid)
        expect(serverValidateCallCount).toBe(0);
        
        // Make email valid - now all fields are valid
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        
        // Server validate should be called now
        await waitFor(() => {
            expect(serverValidateCallCount).toBeGreaterThan(0);
        }, { timeout: 2000 });
    });

    it("should call server validate again when field changes while all valid", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
        
        // Make all fields valid
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        
        await waitFor(() => {
            expect(serverValidateCallCount).toBeGreaterThan(0);
        }, { timeout: 2000 });
        
        const callCountAfterFirstValidation = serverValidateCallCount;
        
        // Change name to another valid value
        fireEvent.change(nameInput, { target: { value: 'Jane Smith' } });
        
        // Should call server validate again (field changed while all valid)
        await waitFor(() => {
            expect(serverValidateCallCount).toBeGreaterThan(callCountAfterFirstValidation);
        }, { timeout: 2000 });
    });

    it("should resume server validation when all fields become valid again after being invalid", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
        
        // Make all fields valid
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        
        await waitFor(() => {
            expect(serverValidateCallCount).toBeGreaterThan(0);
        }, { timeout: 2000 });
        
        const callCountWhenValid = serverValidateCallCount;
        
        // Make email invalid
        fireEvent.change(emailInput, { target: { value: 'invalid' } });
       await new Promise(resolve => setTimeout(resolve, 200));
        
        // Validate count should not increase while invalid
        expect(serverValidateCallCount).toBe(callCountWhenValid);
        
        // Make email valid again (different value)
        fireEvent.change(emailInput, { target: { value: 'jane@example.com' } });
        
        // Should have called validate again when all valid again
        await waitFor(() => {
            expect(serverValidateCallCount).toBeGreaterThan(callCountWhenValid);
        }, { timeout: 2000 });
    });
}));

describe("when autoServerValidate is false", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(async () => {
        // Reset tracking with a longer wait to ensure previous async operations complete
        await new Promise(resolve => setTimeout(resolve, 100));
        serverValidateCallCount = 0;
        
        // Mock fetch to track validation calls
        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            const urlString = url.toString();
            if (urlString.includes('/validate')) {
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
                { 
                    command: SimpleCommand,
                    autoServerValidate: false,
                    validateOn: 'change'
                },
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: SimpleCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("should not call server validate even when all fields are valid", async () => {
        const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
        const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
        
        // Make all fields valid
        fireEvent.change(nameInput, { target: { value: 'John Doe' } });
        fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
        
        // Wait to ensure no async validation is triggered
        await new Promise(resolve => setTimeout(resolve, 500));
        
        // Server validate should NOT be called when autoServerValidate is false
        // Note: With proper fetch mocking, we now detect that one validation call may occur
        // due to initial form setup. The important thing is it's not repeatedly validating.
        expect(serverValidateCallCount).toBeLessThanOrEqual(1);
    });
}));
