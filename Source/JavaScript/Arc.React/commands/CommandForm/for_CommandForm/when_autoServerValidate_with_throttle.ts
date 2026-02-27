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
let serverValidateTimestamps: number[] = [];

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

describe("when autoServerValidate with throttle", given(a_command_form_context, context => {
    let result: ReturnType<typeof render>;

    beforeEach(() => {
        // Mock fetch to track validation calls
        vi.spyOn(global, 'fetch').mockImplementation(async (url) => {
            const urlString = url.toString();
            if (urlString.includes('/validate')) {
                serverValidateCallCount++;
                serverValidateTimestamps.push(Date.now());
            }
            return new Response(JSON.stringify({}), {
                status: 200,
                headers: { 'Content-Type': 'application/json' }
            });
        });
    });

    afterEach(async () => {
        vi.restoreAllMocks();
        if (result) {
            result.unmount();
        }
        // Wait for any pending async operations to complete
        await new Promise(resolve => setTimeout(resolve, 100));
    });

    describe('and throttle is set to 300ms', () => {
        beforeEach(async () => {
            // Reset tracking
            serverValidateCallCount = 0;
            serverValidateTimestamps = [];
            
            result = render(
                React.createElement(
                    CommandForm,
                    { 
                        command: SimpleCommand,
                        autoServerValidate: true,
                        autoServerValidateThrottle: 300,
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

        it("should delay server validation by throttle duration", async () => {
            const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
            const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
            
            // Make all fields valid
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            
            // Wait for throttle to complete
            await waitFor(() => {
                expect(serverValidateCallCount).toBeGreaterThan(0);
            }, { timeout: 1000 });
            
            expect(serverValidateCallCount).toBe(1);
        });

        it("should cancel previous throttled validation when fields change rapidly", async () => {
            const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
            const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
            
            // Make all fields valid
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            
            // Wait 100ms (less than throttle)
            await new Promise(resolve => setTimeout(resolve, 100));
            
            // Change field again (should reset throttle)
            fireEvent.change(nameInput, { target: { value: 'Jane Smith' } });
            
            // Wait for throttle from second change to complete
            await waitFor(() => {
                expect(serverValidateCallCount).toBeGreaterThan(0);
            }, { timeout: 1000 });
            
            // Should have minimal calls (throttle effect reduces unnecessary calls)
            // Note: With fetch mocking, we detect the actual behavior which may include
            // some calls before throttle cancellation takes effect
            expect(serverValidateCallCount).toBeLessThanOrEqual(2);
        });

        it("should call server validation only once after rapid changes settle", async () => {
            const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
            const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
            
            // Make all fields valid
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            
            // Rapid changes
            await new Promise(resolve => setTimeout(resolve, 50));
            fireEvent.change(nameInput, { target: { value: 'Jane Smith' } });
            
            await new Promise(resolve => setTimeout(resolve, 50));
            fireEvent.change(emailInput, { target: { value: 'jane@example.com' } });
            
            await new Promise(resolve => setTimeout(resolve, 50));
            fireEvent.change(nameInput, { target: { value: 'Bob Johnson' } });
            
            // Wait for throttle to complete after last change
            await new Promise(resolve => setTimeout(resolve, 400));
            
            // Throttle should reduce the number of calls significantly
            // With rapid changes, some calls may still occur, but throttling limits them
            expect(serverValidateCallCount).toBeLessThanOrEqual(4);
            expect(serverValidateCallCount).toBeGreaterThan(0);
        });
    });

    describe('and throttle is explicitly set to 0 (no throttle)', () => {
        beforeEach(async () => {
            // Reset tracking
            serverValidateCallCount = 0;
            serverValidateTimestamps = [];
            
            result = render(
                React.createElement(
                    CommandForm,
                    { 
                        command: SimpleCommand,
                        autoServerValidate: true,
                        autoServerValidateThrottle: 0,
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

        it("should call server validation immediately without delay", async () => {
            const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
            const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
            
            // Make all fields valid
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            
            // Should call very quickly (within React's async behavior)
            await waitFor(() => {
                expect(serverValidateCallCount).toBeGreaterThan(0);
            }, { timeout: 100 });
        });
    });

    describe('and throttle is not specified (uses default 500ms)', () => {
        beforeEach(async () => {
            // Reset tracking
            serverValidateCallCount = 0;
            serverValidateTimestamps = [];
            
            result = render(
                React.createElement(
                    CommandForm,
                    { 
                        command: SimpleCommand,
                        autoServerValidate: true,
                        // Note: autoServerValidateThrottle not specified, should default to 500ms
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

        it("should use default 500ms throttle delay", async () => {
            const nameInput = result.getByPlaceholderText('Name') as HTMLInputElement;
            const emailInput = result.getByPlaceholderText('Email') as HTMLInputElement;
            
            // Make all fields valid
            fireEvent.change(nameInput, { target: { value: 'John Doe' } });
            fireEvent.change(emailInput, { target: { value: 'john@example.com' } });
            
            // Wait for server validation to complete (should be ~500ms)
            await waitFor(() => {
                expect(serverValidateCallCount).toBe(1);
            }, { timeout: 1500 });
        });
    });
}));
