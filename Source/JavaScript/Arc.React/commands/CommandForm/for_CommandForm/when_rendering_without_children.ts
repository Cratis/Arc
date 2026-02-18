// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

describe("when rendering without children", given(a_command_form_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand }
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should render without errors", () => {
        container.should.not.be.null;
    });

    it("should not render any field labels", () => {
        const labels = container.querySelectorAll('label');
        labels.should.have.lengthOf(0);
    });
}));

