// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { render, fireEvent, waitFor } from '@testing-library/react';
import { CommandForm, useCommandInstance } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

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

describe("when currentValues changes", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let renderResult: ReturnType<typeof render>;

    beforeEach(async () => {
        const TestWrapper = () => {
            const [currentData, setCurrentData] = useState({ name: 'Initial Name' });

            const TestComponent = () => {
                const command = useCommandInstance<TestCommand>();
                capturedCommand = command;
                return React.createElement('button', {
                    onClick: () => setCurrentData({ name: 'Updated Name' }),
                    'data-testid': 'update-button'
                }, 'Update');
            };

            return React.createElement(
                CommandForm,
                { command: TestCommand, currentValues: currentData },
                React.createElement(TestComponent),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            );
        };

        renderResult = render(
            React.createElement(TestWrapper),
            { wrapper: context.createWrapper() }
        );

        // Wait for initial render
        await waitFor(() => {
            return capturedCommand !== null;
        });

        // Click button to update currentValues
        const button = renderResult.getByTestId('update-button');
        fireEvent.click(button);

        // Wait for update to propagate
        await waitFor(() => {
            return capturedCommand!.name === 'Updated Name';
        });
    });

    it("should update command instance with new current values", () => {
        capturedCommand!.name!.should.equal('Updated Name');
    });

    it("should update input field value", () => {
        const input = renderResult.getByTestId('text-input') as HTMLInputElement;
        input.value.should.equal('Updated Name');
    });
}));

describe("when currentValues clear a property to null", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let renderResult: ReturnType<typeof render>;

    beforeEach(async () => {
        const TestWrapper = () => {
            const [currentData, setCurrentData] = useState<Partial<TestCommand>>({ name: 'Initial Name' });

            const TestComponent = () => {
                const command = useCommandInstance<TestCommand>();
                capturedCommand = command;
                return React.createElement('button', {
                    onClick: () => setCurrentData({ name: null as unknown as string }),
                    'data-testid': 'clear-button'
                }, 'Clear');
            };

            return React.createElement(
                CommandForm,
                { command: TestCommand, currentValues: currentData },
                React.createElement(TestComponent)
            );
        };

        renderResult = render(
            React.createElement(TestWrapper),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => {
            return capturedCommand !== null;
        });

        fireEvent.click(renderResult.getByTestId('clear-button'));

        await waitFor(() => {
            expect(capturedCommand!.name).toBeNull();
        });
    });

    it("should update command instance with null value", () => {
        expect(capturedCommand!.name).toBeNull();
    });
}));

describe("when currentValues clear a property to undefined", given(a_command_form_context, context => {
    let capturedCommand: TestCommand | null = null;
    let renderResult: ReturnType<typeof render>;

    beforeEach(async () => {
        const TestWrapper = () => {
            const [currentData, setCurrentData] = useState<Partial<TestCommand>>({ name: 'Initial Name' });

            const TestComponent = () => {
                const command = useCommandInstance<TestCommand>();
                capturedCommand = command;
                return React.createElement('button', {
                    onClick: () => setCurrentData({ name: undefined }),
                    'data-testid': 'clear-button'
                }, 'Clear');
            };

            return React.createElement(
                CommandForm,
                { command: TestCommand, currentValues: currentData },
                React.createElement(TestComponent)
            );
        };

        renderResult = render(
            React.createElement(TestWrapper),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => {
            return capturedCommand !== null;
        });

        fireEvent.click(renderResult.getByTestId('clear-button'));

        await waitFor(() => {
            expect(capturedCommand!.name).toBeUndefined();
        });
    });

    it("should update command instance with undefined value", () => {
        expect(capturedCommand!.name).toBeUndefined();
    });
}));
