#!/usr/bin/env node
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// This script builds arc-bootstrap.js by bundling the real Arc libraries
// for use in ClearScript V8 testing environment

const fs = require('fs');
const path = require('path');

// Paths to the built Arc libraries (CJS format for V8)
const arcPath = path.join(__dirname, '../../../JavaScript/Arc/dist/cjs');
const arcReactPath = path.join(__dirname, '../../../JavaScript/Arc.React/dist/cjs');
const fundamentalsPath = path.join(__dirname, '../../../JavaScript/Arc/node_modules/@cratis/fundamentals/dist/cjs');

const outputPath = path.join(__dirname, 'Scenarios/Infrastructure/arc-bootstrap.js');

console.log('Building arc-bootstrap.js...');
console.log('Arc path:', arcPath);
console.log('Arc.React path:', arcReactPath);
console.log('Fundamentals path:', fundamentalsPath);

// Read all CJS modules
function readModulesFromDir(dir, prefix = '') {
    const modules = {};
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    
    for (const entry of entries) {
        const fullPath = path.join(dir, entry.name);
        
        if (entry.isDirectory()) {
            // Recursively read subdirectories
            Object.assign(modules, readModulesFromDir(fullPath, prefix ? `${prefix}/${entry.name}` : entry.name));
        } else if (entry.name.endsWith('.js')) {
            // Read JavaScript file
            const moduleName = prefix ? `${prefix}/${entry.name.replace('.js', '')}` : entry.name.replace('.js', '');
            modules[moduleName] = fs.readFileSync(fullPath, 'utf-8');
        }
    }
    
    return modules;
}

console.log('Reading Arc modules...');
const arcModules = readModulesFromDir(arcPath);
console.log(`Found ${Object.keys(arcModules).length} Arc modules`);

console.log('Reading Arc.React modules...');
const arcReactModules = readModulesFromDir(arcReactPath);
console.log(`Found ${Object.keys(arcReactModules).length} Arc.React modules`);

console.log('Reading Fundamentals modules...');
const fundamentalsModules = fs.existsSync(fundamentalsPath) 
    ? readModulesFromDir(fundamentalsPath)
    : {};
console.log(`Found ${Object.keys(fundamentalsModules).length} Fundamentals modules`);

// Build the bootstrap file
const bootstrap = `// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Arc Runtime Bootstrap for ClearScript V8
// This file is AUTO-GENERATED - do not edit manually
// Run 'node build-arc-bootstrap.js' to regenerate

// Node.js process shim (required by TypeScript compiler)
var process = {
    env: {},
    platform: 'darwin',
    version: 'v20.0.0',
    versions: { node: '20.0.0' },
    cwd: function() { return '/'; },
    nextTick: function(fn) { setTimeout(fn, 0); },
    stderr: { write: function() {} },
    stdout: { write: function() {} }
};

// Module registry
var __moduleRegistry = ${JSON.stringify({
    '@cratis/arc': arcModules,
    '@cratis/arc.react': arcReactModules,
    '@cratis/fundamentals': fundamentalsModules
}, null, 2)};

var __moduleCache = {};
var __executingModules = new Set();

// Execute a module and return its exports
function __executeModule(packageName, modulePath, code) {
    var fullKey = packageName + '/' + modulePath;
    
    // Check for circular dependencies
    if (__executingModules.has(fullKey)) {
        console.warn('Circular dependency detected:', fullKey);
        return __moduleCache[fullKey] ? __moduleCache[fullKey].exports : {};
    }
    
    __executingModules.add(fullKey);
    
    var module = { exports: {} };
    var exports = module.exports;
    __moduleCache[fullKey] = module;
    
    try {
        // Create a module-scoped function
        var moduleFunction = new Function('require', 'module', 'exports', '__filename', '__dirname', code);
        
        // Create a scoped require for this module
        var scopedRequire = function(modulePath) {
            return require(modulePath, packageName);
        };
        
        moduleFunction(scopedRequire, module, exports, '/' + fullKey + '.js', '/' + packageName);
    } catch (e) {
        console.error('Error executing module ' + fullKey + ':', e.message);
        throw e;
    } finally {
        __executingModules.delete(fullKey);
    }
    
    return module.exports;
}

// CommonJS require implementation
function require(modulePath, currentPackage) {
    // Handle relative imports within same package
    if ((modulePath.startsWith('./') || modulePath.startsWith('../')) && currentPackage) {
        var cacheKey = currentPackage + '/' + modulePath;
        if (__moduleCache[cacheKey]) {
            return __moduleCache[cacheKey].exports;
        }
        
        // Create empty module for DTO/data classes
        var module = { exports: {} };
        var typeName = modulePath.replace(/^\\.\\.?\\//, '').replace(/\\.js$/, '');
        var TypeConstructor = function() {};
        module.exports[typeName] = TypeConstructor;
        __moduleCache[cacheKey] = module;
        return module.exports;
    }
    
    // Handle @cratis/arc imports
    if (modulePath === '@cratis/arc' || modulePath.startsWith('@cratis/arc/')) {
        var subPath = modulePath === '@cratis/arc' ? 'index' : modulePath.replace('@cratis/arc/', '');
        var cacheKey = '@cratis/arc/' + subPath;
        
        if (__moduleCache[cacheKey]) {
            return __moduleCache[cacheKey].exports;
        }
        
        var modules = __moduleRegistry['@cratis/arc'];
        if (!modules[subPath]) {
            throw new Error('Arc module not found: ' + subPath);
        }
        
        return __executeModule('@cratis/arc', subPath, modules[subPath]);
    }
    
    // Handle @cratis/arc.react imports
    if (modulePath === '@cratis/arc.react' || modulePath.startsWith('@cratis/arc.react/')) {
        var subPath = modulePath === '@cratis/arc.react' ? 'index' : modulePath.replace('@cratis/arc.react/', '');
        var cacheKey = '@cratis/arc.react/' + subPath;
        
        if (__moduleCache[cacheKey]) {
            return __moduleCache[cacheKey].exports;
        }
        
        var modules = __moduleRegistry['@cratis/arc.react'];
        if (!modules[subPath]) {
            throw new Error('Arc.React module not found: ' + subPath);
        }
        
        return __executeModule('@cratis/arc.react', subPath, modules[subPath]);
    }
    
    // Handle @cratis/fundamentals imports
    if (modulePath === '@cratis/fundamentals' || modulePath.startsWith('@cratis/fundamentals/')) {
        var subPath = modulePath === '@cratis/fundamentals' ? 'index' : modulePath.replace('@cratis/fundamentals/', '');
        var cacheKey = '@cratis/fundamentals/' + subPath;
        
        if (__moduleCache[cacheKey]) {
            return __moduleCache[cacheKey].exports;
        }
        
        var modules = __moduleRegistry['@cratis/fundamentals'];
        if (!modules[subPath]) {
            console.warn('Fundamentals module not found:', subPath, '- creating stub');
            // Create a stub module
            var module = { exports: {} };
            __moduleCache[cacheKey] = module;
            return module.exports;
        }
        
        return __executeModule('@cratis/fundamentals', subPath, modules[subPath]);
    }
    
    // Handle other relative imports
    if (modulePath.startsWith('./') || modulePath.startsWith('../')) {
        var typeName = modulePath.replace(/^\\.\\.?\\//, '').replace(/\\.js$/, '');
        var module = { exports: {} };
        module.exports[typeName] = function() {};
        return module.exports;
    }
    
    throw new Error('Module not found: ' + modulePath);
}

// Global module shims
var module = { exports: {} };
var exports = module.exports;

console.log('Arc bootstrap loaded successfully');
`;

console.log('Writing arc-bootstrap.js...');
fs.writeFileSync(outputPath, bootstrap, 'utf-8');
console.log('Done! arc-bootstrap.js written to:', outputPath);
console.log(`File size: ${(fs.statSync(outputPath).size / 1024).toFixed(2)} KB`);
