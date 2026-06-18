// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
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
