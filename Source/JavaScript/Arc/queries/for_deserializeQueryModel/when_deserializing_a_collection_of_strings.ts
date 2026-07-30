// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { deserializeQueryModels } from '../deserializeQueryModel';

describe('when deserializing a collection of strings', () => {
    const result = deserializeQueryModels<string>(String as Constructor, ['first', 'second']);

    it('should keep every item a primitive string', () => result.every(_ => typeof _ === 'string').should.be.true);
    it('should keep the values intact', () => result.should.deep.equal(['first', 'second']));
});
