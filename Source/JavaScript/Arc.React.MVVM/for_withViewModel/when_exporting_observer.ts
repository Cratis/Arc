// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { observer } from '../index';

describe('when exporting observer', () => {
    it('should export the mobx observer function', () => {
        observer.should.be.a('function');
    });

    it('should wrap a component into an observer component', () => {
        const Wrapped = observer(() => null);
        Wrapped.should.not.be.null;
    });
});
