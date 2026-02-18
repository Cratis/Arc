// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { CommandFormField } from '../CommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when showTitles is false", given(a_command_form_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand, showTitles: false },
                React.createElement(CommandFormField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should not render field titles", () => {
        const labels = container.querySelectorAll('label');
        labels.should.have.lengthOf(0);
    });
}));
