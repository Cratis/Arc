// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act } from '@testing-library/react';
import sinon from 'sinon';
import type { ExceptionDisplayProps } from '../../index';
import { given } from '../../../../given';
import { a_command_form_with_exception_feedback } from '../given/a_command_form_with_exception_feedback';

describe('when displaying exception feedback with a custom display', given(a_command_form_with_exception_feedback, context => {
    const localizedMessage = 'Une erreur inattendue est survenue. Veuillez réessayer.';
    let display: sinon.SinonSpy;

    beforeEach(async () => {
        display = sinon.spy((props: ExceptionDisplayProps) => <aside role='alert'>{props.message}</aside>);
        await context.renderForm({ exceptionMessage: localizedMessage, exceptionDisplayComponent: display });
        await act(async () => context.setResult(context.exceptionResult()));
    });

    it('should replace the fallback panel with the custom display', () => {
        context.rendered.getAllByRole('alert').length.should.equal(1);
        context.rendered.getByRole('alert').tagName.should.equal('ASIDE');
        context.rendered.getByRole('alert').parentElement!.tagName.should.equal('FORM');
        context.rendered.container.textContent!.should.equal(localizedMessage);
    });
    it('should pass only the safe message prop on every invocation', () => {
        display.called.should.equal(true);
        display.getCalls().forEach(call => call.args[0].should.deep.equal({ message: localizedMessage }));
    });
    it('should not render the default text or private diagnostics', () => {
        const markup = context.rendered.container.innerHTML;
        markup.should.not.contain(context.defaultMessage);
        markup.should.not.contain(context.privateMessage);
        markup.should.not.contain(context.privateStack);
        markup.should.not.contain('The server responded with');
    });
}));
