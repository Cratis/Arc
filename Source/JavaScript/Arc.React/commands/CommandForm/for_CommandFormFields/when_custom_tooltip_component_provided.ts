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

interface TooltipWrapperProps {
    description: string;
    children: React.ReactNode;
}

const CustomTooltipWrapper = (props: TooltipWrapperProps) => {
    return React.createElement('div', {
        'data-testid': 'custom-tooltip',
        'data-tooltip': props.description,
        className: 'custom-tooltip-wrapper'
    }, props.children);
};

describe("when custom tooltip component provided", given(a_command_form_fields_context, context => {
    let container: HTMLElement;

    beforeEach(() => {
        const result = render(
            React.createElement(
                CommandForm,
                {
                    command: TestCommand,
                    tooltipComponent: CustomTooltipWrapper
                },
                React.createElement(SimpleTextField, {
                    value: (c: TestCommand) => c.name,
                    title: 'Name',
                    description: 'Please enter your full name'
                })
            ),
            { wrapper: context.createWrapper() }
        );
        container = result.container;
    });

    it("should use custom tooltip wrapper component", () => {
        const customTooltip = container.querySelector('[data-testid="custom-tooltip"]');
        (customTooltip !== null).should.be.true;
    });

    it("should pass description to custom tooltip", () => {
        const customTooltip = container.querySelector('[data-testid="custom-tooltip"]');
        customTooltip!.getAttribute('data-tooltip')!.should.equal('Please enter your full name');
    });
}));
