// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render } from '@testing-library/react';
import { CommandForm } from '../CommandForm';
// eslint-disable-next-line @typescript-eslint/no-unused-vars
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
            onChange: props.onChange
        });
    },
    {
        defaultValue: '',
        extractValue: (e: unknown) => (e as React.ChangeEvent<HTMLInputElement>).target.value
    }
);

interface FieldContainerProps {
    title?: string;
    errorMessage?: string;
    children: React.ReactNode;
}

const CustomFieldContainer = (props: FieldContainerProps) => {
    return React.createElement('div', {
        'data-testid': 'custom-container',
        'data-title': props.title,
        className: 'custom-field-container'
    }, props.children);
};

describe("when custom field container provided", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    fieldContainerComponent: CustomFieldContainer
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should use custom field container", () => {
        const customContainer = container.querySelector('[data-testid="custom-container"]');
        (customContainer !== null).should.be.true;
    });

    it("should pass title to custom container", () => {
        const customContainer = container.querySelector('[data-testid="custom-container"]');
        customContainer!.getAttribute('data-title')!.should.equal('Name');
    });
}));
