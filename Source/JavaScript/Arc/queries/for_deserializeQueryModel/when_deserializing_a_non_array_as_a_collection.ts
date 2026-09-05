// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';
import { deserializeQueryModels } from '../deserializeQueryModel';

describe('when deserializing a non array as a collection', () => {
    const result = deserializeQueryModels<string>(String as Constructor, null);

    it('should produce an empty collection', () => result.should.be.empty);
});
