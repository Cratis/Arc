// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { SomeCommand } from './SomeCommand.js';

describe('when registering the same property changed callback twice', () => {
    it('should notify the callback once per change', () => {
        const command = new SomeCommand();
        const callback = sinon.spy();
        command.onPropertyChanged(callback, command);
        command.onPropertyChanged(callback, command);

        command.propertyChanged('someProperty');

        callback.calledOnceWithExactly('someProperty').should.be.true;
        command.propertyChanged('someProperty');
        callback.callCount.should.equal(2);
    });

    it('should retain registrations with different receivers', () => {
        const command = new SomeCommand();
        const callback = sinon.spy();
        const otherReceiver = {};
        command.onPropertyChanged(callback, command);
        command.onPropertyChanged(callback, otherReceiver);

        command.propertyChanged('someProperty');

        callback.calledTwice.should.be.true;
        callback.calledOn(command).should.be.true;
        callback.calledOn(otherReceiver).should.be.true;
    });

    it('should retain different callbacks for the same receiver', () => {
        const command = new SomeCommand();
        const first = sinon.spy();
        const second = sinon.spy();
        command.onPropertyChanged(first, command);
        command.onPropertyChanged(second, command);

        command.propertyChanged('someProperty');

        first.calledOnce.should.be.true;
        second.calledOnce.should.be.true;
    });
});
