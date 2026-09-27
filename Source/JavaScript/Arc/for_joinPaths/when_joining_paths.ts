// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { joinPaths } from '../joinPaths.js';

describe('when joining paths', () => {
    it('should join ordinary segments', () => joinPaths('api', 'orders').should.equal('api/orders'));
    it('should preserve a leading slash', () => joinPaths('/workbench', 'api').should.equal('/workbench/api'));
    it('should collapse repeated slashes at each boundary', () => joinPaths('/workbench//', '///api//', '//orders').should.equal('/workbench/api/orders'));
    it('should leave slashes within a segment alone', () => joinPaths('/a//b', 'c//d').should.equal('/a//b/c//d'));
    it('should preserve an https scheme', () => joinPaths('https://example.com/', '/api').should.equal('https://example.com/api'));
    it('should preserve an http scheme', () => joinPaths('http://example.com', '/api').should.equal('http://example.com/api'));
    it('should preserve a scheme supplied as its own segment', () => joinPaths('https://', 'example.com', '/api').should.equal('https://example.com/api'));
    it('should handle an empty first segment', () => joinPaths('', '/api').should.equal('/api'));
    it('should handle empty middle segments', () => joinPaths('/api', '', 'orders').should.equal('/api/orders'));
    it('should preserve the separator between two empty segments', () => joinPaths('', '').should.equal('/'));
    it('should handle no segments', () => joinPaths().should.equal(''));
    it('should preserve a trailing slash', () => joinPaths('/api/', 'orders/').should.equal('/api/orders/'));
    it('should preserve a trailing slash for an empty final segment', () => joinPaths('/api', '').should.equal('/api/'));
});
