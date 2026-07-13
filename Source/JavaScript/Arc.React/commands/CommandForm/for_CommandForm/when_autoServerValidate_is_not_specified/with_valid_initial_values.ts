// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../../CommandForm';
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

describe("when autoServerValidate is not specified with valid initial values", given(a_command_form_context, context => {
    let capturedIsValid: boolean | undefined;

    const ContextCapture = () => {
        const ctx = useCommandFormContext();
        capturedIsValid = ctx.isValid;
        return React.createElement('div');
    };

    beforeEach(() => {
        capturedIsValid = undefined;
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

        render(
            React.createElement(
                CommandForm,
                {
                    command: ValidatedCommand,
                    initialValues: { name: 'John Doe' }
                },
                React.createElement(ContextCapture)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it("should report isValid as true without contacting the server", async () => {
        await waitFor(() => {
            return capturedIsValid === true;
        }, { timeout: 2000 });
        serverValidateCallCount.should.equal(0);
    });
}));
