// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import { CommandResult } from '@cratis/arc/commands';
import sinon from 'sinon';
import type { ExceptionDisplayProps } from '../../index';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback and a descendant replaces failure with success', given(a_command_form_with_exception_feedback, context => {
    let display: sinon.SinonSpy;
    let previousMessage: string;
    let previousProps: unknown;

    beforeEach(async () => {
        display = sinon.spy((props: ExceptionDisplayProps) => <aside role='alert'>{props.message}</aside>);
        await context.renderForm({ exceptionDisplayComponent: display });
        await act(async () => context.setResult(context.exceptionResult()));
        previousMessage = context.rendered.container.textContent!;
        previousProps = display.lastCall?.args[0];
        display.resetHistory();
        await act(async () => context.setResult(CommandResult.empty));
    });

    it('should supply only the default safe message to the custom renderer', () => {
        (previousProps as object).should.deep.equal({ message: context.defaultMessage });
    });
    it('should remove the previous custom exception display', () => {
        previousMessage.should.equal(context.defaultMessage);
        context.rendered.container.textContent!.should.equal('');
        (context.rendered.queryByRole('alert') === null).should.equal(true);
    });
    it('should stop invoking the custom renderer for the successful result', () => {
        display.called.should.equal(false);
        context.formContext.commandResult!.should.equal(CommandResult.empty);
    });
}));
