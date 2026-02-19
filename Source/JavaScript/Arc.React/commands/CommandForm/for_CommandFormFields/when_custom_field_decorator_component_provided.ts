// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
import { asCommandFormField } from '../asCommandFormField';
import { TestCommand } from '../for_CommandForm/TestCommand';
import { a_command_form_fields_context } from './given/a_command_form_fields_context';
import { given } from '../../../given';

const SimpleTextField = asCommandFormField<{ value: string; onChange: (value: unknown) => void; invalid: boolean; required: boolean; errors: string[] }>(
    (props) => {
        return React.createElement('input', {
            type: 'text',
            value: props.value,
            onChange: props.onChange
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

interface FieldDecoratorProps {
    icon?: React.ReactElement;
    description?: string;
    children: React.ReactNode;
}

const CustomFieldDecorator = (props: FieldDecoratorProps) => {
    return React.createElement('div', {
        'data-testid': 'custom-decorator',
        'data-has-icon': !!props.icon,
        'data-description': props.description,
        className: 'custom-field-decorator'
    }, [
        props.icon && React.createElement('div', { key: 'icon', className: 'decorator-icon' }, props.icon),
        React.createElement('div', { key: 'field', className: 'decorator-field' }, props.children)
    ]);
};

const TestIcon = () => React.createElement('svg', { 'data-testid': 'field-icon' });

describe("when custom field decorator component provided", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    fieldDecoratorComponent: CustomFieldDecorator
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    icon: React.createElement(TestIcon),
                    description: 'Enter your name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should use custom field decorator component", () => {
        const customDecorator = container.querySelector('[data-testid="custom-decorator"]');
        (customDecorator !== null).should.be.true;
    });

    it("should pass icon to custom decorator", () => {
        const customDecorator = container.querySelector('[data-testid="custom-decorator"]');
        customDecorator!.getAttribute('data-has-icon')!.should.equal('true');
    });

    it("should pass description to custom decorator", () => {
        const customDecorator = container.querySelector('[data-testid="custom-decorator"]');
        customDecorator!.getAttribute('data-description')!.should.equal('Enter your name');
    });

    it("should render icon within decorator", () => {
        const icon = container.querySelector('[data-testid="field-icon"]');
        (icon !== null).should.be.true;
    });
}));
