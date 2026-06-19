// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React, { useState } from 'react';
import { fireEvent, render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandFormContext, useCommandInstance } from '../CommandForm';
import { Command } from '@cratis/arc/commands';
import { PropertyDescriptor } from '@cratis/arc/reflection';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

class RequiredNameCommand extends Command {
    readonly route = '/api/test';
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

describe("when currentValues are used without fields", given(a_command_form_context, context => {
    let capturedCommand: RequiredNameCommand | null = null;
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        capturedCommand = useCommandInstance<RequiredNameCommand>();
        capturedIsValid = useCommandFormContext().isValid;
        return React.createElement('div');
    };

    beforeEach(() => {
        capturedCommand = null;
        capturedIsValid = undefined;

        render(
            React.createElement(
                CommandForm,
                {
                    command: RequiredNameCommand,
                    currentValues: { name: 'Jane' }
                },
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should set command properties from currentValues", () => {
        capturedCommand!.name.should.equal('Jane');
    });

    it("should report the form as valid", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });
    });
}));

describe("when currentValues change without fields", given(a_command_form_context, context => {
    let capturedCommand: RequiredNameCommand | null = null;
    let capturedIsValid: boolean | undefined;
    let renderResult: ReturnType<typeof render>;

    const ContextCapture = () => {
        capturedCommand = useCommandInstance<RequiredNameCommand>();
        capturedIsValid = useCommandFormContext().isValid;
        return React.createElement('div');
    };

    const TestWrapper = () => {
        const [currentValues, setCurrentValues] = useState({ name: '' });

        return React.createElement(
            CommandForm,
            {
                command: RequiredNameCommand,
                currentValues
            },
            React.createElement(ContextCapture),
            React.createElement('button', {
                type: 'button',
                onClick: () => setCurrentValues({ name: 'Jane' }),
                'data-testid': 'set-valid-values'
            }, 'Set valid values')
        );
    };

    beforeEach(async () => {
        capturedCommand = null;
        capturedIsValid = undefined;

        renderResult = render(
            React.createElement(TestWrapper),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => {
            expect(capturedIsValid).toBe(false);
        }, { timeout: 2000 });

        fireEvent.click(renderResult.getByTestId('set-valid-values'));
    });

    it("should update command properties from new currentValues", async () => {
        await waitFor(() => {
            expect(capturedCommand!.name).toBe('Jane');
        }, { timeout: 2000 });
    });

    it("should revalidate the command and report the form as valid", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });
    });
}));

describe("when currentValues become invalid without fields", given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;
    let renderResult: ReturnType<typeof render>;

    const ContextCapture = () => {
        capturedIsValid = useCommandFormContext().isValid;
        return React.createElement('div');
    };

    const TestWrapper = () => {
        const [currentValues, setCurrentValues] = useState({ name: 'Jane' });

        return React.createElement(
            CommandForm,
            {
                command: RequiredNameCommand,
                currentValues
            },
            React.createElement(ContextCapture),
            React.createElement('button', {
                type: 'button',
                onClick: () => setCurrentValues({ name: '' }),
                'data-testid': 'set-invalid-values'
            }, 'Set invalid values')
        );
    };

    beforeEach(async () => {
        capturedIsValid = undefined;

        renderResult = render(
            React.createElement(TestWrapper),
            { wrapper: context.createWrapper() }
        );

        await waitFor(() => {
            expect(capturedIsValid).toBe(true);
        }, { timeout: 2000 });

        fireEvent.click(renderResult.getByTestId('set-invalid-values'));
    });

    it("should revalidate the command and report the form as invalid", async () => {
        await waitFor(() => {
            expect(capturedIsValid).toBe(false);
        }, { timeout: 2000 });
    });
}));
