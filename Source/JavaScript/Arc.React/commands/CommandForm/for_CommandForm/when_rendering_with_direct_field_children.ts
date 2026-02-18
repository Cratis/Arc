// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from './TestCommand';
import { a_command_form_context } from './given/a_command_form_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: string) => void; invalid: boolean; required: boolean; errors: string[]; title?: string }>(
    (props) => {
        return React.createElement('div', {},
            props.title && React.createElement('label', {}, props.title),
            React.createElement('input', {
                type: 'text',
                value: props.value,
                onChange: (e: React.ChangeEvent<HTMLInputElement>) => props.onChange(e.target.value)
            })
        );
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

describe("when rendering with direct field children", given(a_command_form_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                { command: TestCommand },
                React.createElement(SimpleTextField, { 
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                }),
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.email,
                    title: 'Email'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should render the form", () => {
        container.should.not.be.null;
    });

    it("should render field titles when showTitles is default true", () => {
        const labels = container.querySelectorAll('label');
        labels.should.have.lengthOf(2);
    });

    it("should render name field title", () => {
        const labels = container.querySelectorAll('label');
        Array.from(labels).some(label => label.textContent === 'Name').should.be.true;
    });

    it("should render email field title", () => {
        const labels = container.querySelectorAll('label');
        Array.from(labels).some(label => label.textContent === 'Email').should.be.true;
    });
}));
