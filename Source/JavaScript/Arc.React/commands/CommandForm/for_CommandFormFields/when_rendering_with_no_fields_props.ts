// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { CommandFormField } from '../CommandFormField';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange,
            'data-testid': 'text-input'
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when rendering with no fields props", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(CommandForm, { command: TestCommand }),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should render without errors", () => {
        container.should.not.be.null;
    });

    it("should not render any inputs", () => {
        const inputs = container.querySelectorAll('input');
        inputs.should.have.lengthOf(0);
    });
}));
