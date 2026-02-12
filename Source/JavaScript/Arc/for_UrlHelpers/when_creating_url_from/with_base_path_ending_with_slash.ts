// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { UrlHelpers } from '../../UrlHelpers';


describe("with_base_path_ending_with_slash", () => {
    let origin: string;
    let apiBasePath: string;
    let route: string;
    let result: URL;

    beforeEach(() => {
        origin = 'https://example.com';
        apiBasePath = '/api/v1/'; // base path ending with slash
        route = 'users/123'; // relative route
    
        result = UrlHelpers.createUrlFrom(origin, apiBasePath, route);
    });

    it("should create correct url", () => {
        result.href.should.equal('https://example.com/api/v1/users/123');
    });

    it("should have correct origin", () => {
        result.origin.should.equal('https://example.com');
    });

    it("should have correct pathname", () => {
        result.pathname.should.equal('/api/v1/users/123');
    });
});