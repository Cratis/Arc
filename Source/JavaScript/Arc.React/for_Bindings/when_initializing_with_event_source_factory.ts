// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { Bindings } from '../Bindings';
import { Globals, EventSourceFactory } from '@cratis/arc';
import { bindings_context } from './given/bindings_context';
import { given } from '../given';

describe('when initializing bindings with a custom event source factory', given(bindings_context, () => {
    let originalFactory: EventSourceFactory | undefined;
    let factory: sinon.SinonStub;

    beforeEach(() => {
        originalFactory = Globals.eventSourceFactory;
        factory = sinon.stub();

        Bindings.initialize('test-microservice', '/test/api', 'http://test.com', undefined, undefined, undefined, undefined, undefined, factory as unknown as EventSourceFactory);
    });

    afterEach(() => {
        Globals.eventSourceFactory = originalFactory;
        sinon.restore();
    });

    it('should set globals event source factory', () => Globals.eventSourceFactory!.should.equal(factory));
}));
