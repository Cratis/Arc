// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm, useCommandFormContext } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when accessing context from child component", given(a_command_form_context, context => {
    let contextValue: ReturnType<typeof useCommandFormContext> | null = null;

    beforeEach(() => {
        const TestComponent = () => {
            contextValue = useCommandFormContext();
            return React.createElement('div');
        };

        render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(TestComponent)
            ),
            { wrapper: context.createWrapper() }
        );
    });

    it("should provide command constructor in context", () => {
        contextValue!.command.should.equal(TestCommand);
    });

    it("should provide command instance in context", () => {
        (contextValue!.commandInstance as object).should.not.be.null;
    });

    it("should provide isValid flag in context", () => {
        contextValue!.isValid.should.be.a('boolean');
    });

    it("should provide setCommandValues function in context", () => {
        contextValue!.setCommandValues.should.be.a('function');
    });

    it("should provide getFieldError function in context", () => {
        contextValue!.getFieldError.should.be.a('function');
    });

    it("should provide showTitles default value true in context", () => {
        contextValue!.showTitles.should.be.true;
    });

    it("should provide showErrors default value true in context", () => {
        contextValue!.showErrors.should.be.true;
    });
}));
