// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Arc Module Loader - Loads built JavaScript files from dist/cjs
(function() {
    // Check if we've already been loaded
    if (globalThis.__arcBootstrapLoaded) {
        if (globalThis.__write_to_file) {
            globalThis.__write_to_file('[ArcBootstrap] ALREADY LOADED - skipping re-initialization');
        }
        return;
    }
    globalThis.__arcBootstrapLoaded = true;
    
    if (globalThis.__write_to_file) {
        globalThis.__write_to_file('[ArcBootstrap] LOADING for the first time');
    }

    // Node.js process shim (required by some modules)
    if (!globalThis.process) {
        globalThis.process = {
            env: {},
            platform: 'darwin',
            version: 'v20.0.0',
            versions: { node: '20.0.0' },
            cwd: function() { return '/'; },
            nextTick: function(fn) { setTimeout(fn, 0); },
            stderr: { write: function() {} },
            stdout: { write: function() {} }
        };
    }

    // URL API shim (required by some modules)
    if (!globalThis.URL) {
        globalThis.URL = class URL {
            constructor(url, base) {
                // Simple URL combination logic
                if (base && !url.startsWith('http')) {
                    // Remove trailing slash from base
                    const cleanBase = base.endsWith('/') ? base.slice(0, -1) : base;
                    // Add leading slash to url if missing
                    const cleanUrl = url.startsWith('/') ? url : '/' + url;
                    this.href = cleanBase + cleanUrl;
                } else {
                    this.href = url;
                }
                
                // Parse the URL
                const urlObj = this.href.match(/^(https?):\/\/([^/:]+)(:(\d+))?(\/.*)?$/);
                if (urlObj) {
                    this.protocol = urlObj[1] + ':';
                    this.hostname = urlObj[2];
                    this.port = urlObj[4] || '';
                    this.host = this.port ? `${this.hostname}:${this.port}` : this.hostname;
                    this.pathname = urlObj[5] || '/';
                    this.search = '';
                    this.hash = '';
                } else {
                    // Fallback for malformed URLs
                    this.protocol = '';
                    this.host = '';
                    this.hostname = '';
                    this.port = '';
                    this.pathname = this.href;
                    this.search = '';
                    this.hash = '';
                }
            }
            toString() {
                return this.href;
            }
        };
    }

    // AbortController API shim (required by fetch for cancellation)
    if (!globalThis.AbortController) {
        globalThis.AbortController = class AbortController {
            constructor() {
                this.signal = {
                    aborted: false,
                    addEventListener: function() {},
                    removeEventListener: function() {}
                };
            }
            abort() {
                this.signal.aborted = true;
            }
        };
    }

    // URLSearchParams API shim (required for query string manipulation)
    if (!globalThis.URLSearchParams) {
        globalThis.URLSearchParams = class URLSearchParams {
            constructor(init) {
                this._params = {};
                if (typeof init === 'string') {
                    // Parse query string
                    if (init.startsWith('?')) init = init.slice(1);
                    init.split('&').forEach(pair => {
                        const [key, value] = pair.split('=');
                        if (key) this._params[decodeURIComponent(key)] = decodeURIComponent(value || '');
                    });
                } else if (init) {
                    // Copy from object or another URLSearchParams
                    for (const key in init) {
                        this._params[key] = init[key];
                    }
                }
            }
            append(name, value) {
                this._params[name] = String(value);
            }
            set(name, value) {
                this._params[name] = String(value);
            }
            get(name) {
                return this._params[name] || null;
            }
            has(name) {
                return name in this._params;
            }
            delete(name) {
                delete this._params[name];
            }
            toString() {
                return Object.keys(this._params)
                    .map(key => `${encodeURIComponent(key)}=${encodeURIComponent(this._params[key])}`)
                    .join('&');
            }
        };
    }

    // Timer API shims (required by WebSocket and other modules)
    const timers = {};
    let nextTimerId = 1;
    
    if (!globalThis.setTimeout) {
        globalThis.setTimeout = function(callback, delay, ...args) {
            const timerId = nextTimerId++;
            timers[timerId] = {
                callback: callback,
                args: args,
                type: 'timeout'
            };
            // Since we can't actually schedule it, execute immediately
            // This is a limitation of the test environment, but it won't block
            try {
                callback(...args);
            } catch (e) {
                console.error('setTimeout callback error:', e);
            }
            return timerId;
        };
    }
    
    if (!globalThis.setInterval) {
        globalThis.setInterval = function(callback, delay, ...args) {
            const timerId = nextTimerId++;
            timers[timerId] = {
                callback: callback,
                args: args,
                type: 'interval'
            };
            // We can't truly schedule intervals in V8, so we just store the reference
            // The actual interval behavior won't work, but it won't crash
            return timerId;
        };
    }
    
    if (!globalThis.clearTimeout) {
        globalThis.clearTimeout = function(timerId) {
            delete timers[timerId];
        };
    }
    
    if (!globalThis.clearInterval) {
        globalThis.clearInterval = function(timerId) {
            delete timers[timerId];
        };
    }

    // Module cache and loading state tracking
    const moduleCache = {};
    const loadingModules = new Set(); // Track modules currently being loaded to prevent circular loops
    
    // Map of module specifiers to built JavaScript file paths (dist/cjs)
    const modulePathMap = {
        '@cratis/fundamentals': 'node_modules/@cratis/fundamentals/dist/cjs/index.js',
        'reflect-metadata': 'node_modules/reflect-metadata/Reflect.js',
        '@cratis/arc/queries': 'Arc/dist/cjs/queries/index.js',
        '@cratis/arc/commands': 'Arc/dist/cjs/commands/index.js',
        '@cratis/arc/reflection': 'Arc/dist/cjs/reflection/index.js',
        '@cratis/arc/validation': 'Arc/dist/cjs/validation/index.js',
        '@cratis/arc/identity': 'Arc/dist/cjs/identity/index.js',
        '@cratis/arc': 'Arc/dist/cjs/index.js',
        '@cratis/arc.react/queries': 'Arc.React/dist/cjs/queries/index.js',
        '@cratis/arc.react/commands': 'Arc.React/dist/cjs/commands/index.js',
        '@cratis/arc.react/identity': 'Arc.React/dist/cjs/identity/index.js',
        '@cratis/arc.react/dialogs': 'Arc.React/dist/cjs/dialogs/index.js',
        '@cratis/arc.react': 'Arc.React/dist/cjs/index.js'
    };

    // Resolve module specifier to file path
    function resolveModulePath(specifier, fromPath) {
        // Special handling for React and JSX runtime - these are polyfills, not real modules
        if (specifier === 'react' || specifier === 'react/jsx-runtime') {
            return '__POLYFILL__' + specifier;
        }
        
        // Direct mapping for package imports
        if (modulePathMap[specifier]) {
            return modulePathMap[specifier];
        }
        
        // Relative imports (e.g., './ObservableQueryConnection', './commands')
        if (specifier.startsWith('./') || specifier.startsWith('../')) {
            if (!fromPath) {
                throw new Error(`Cannot resolve relative import '${specifier}' without context`);
            }
            
            // Get directory of the current module
            const fromDir = fromPath.substring(0, fromPath.lastIndexOf('/'));
            
            // Resolve relative path
            let resolved = fromDir + '/' + specifier;
            
            // Normalize path (remove './' and '../')
            const parts = resolved.split('/');
            const normalized = [];
            for (let i = 0; i < parts.length; i++) {
                if (parts[i] === '.' || parts[i] === '') continue;
                if (parts[i] === '..') {
                    normalized.pop();
                } else {
                    normalized.push(parts[i]);
                }
            }
            resolved = normalized.join('/');
            
            // Add .js extension if not present
            if (!resolved.endsWith('.js')) {
                // Check if it's a directory import (like './commands')
                const indexPath = resolved + '/index.js';
                if (__fileExists(indexPath)) {
                    return indexPath;
                }
                // Otherwise it's a file import
                resolved += '.js';
            }
            
            return resolved;
        }
        
        throw new Error(`Unknown module: ${specifier}`);
    }

    // Load a JavaScript module
    function loadModule(modulePath, fromPath) {
        // Handle polyfill markers
        if (modulePath && modulePath.startsWith('__POLYFILL__')) {
            const specifier = modulePath.substring('__POLYFILL__'.length);
            if (specifier === 'react') {
                return getReactShim();
            }
            if (specifier === 'react/jsx-runtime') {
                return {
                    jsx: function(type, props) {
                        return { type: type, props: props || {} };
                    },
                    jsxs: function(type, props) {
                        return { type: type, props: props || {} };
                    },
                    Fragment: 'Fragment'
                };
            }
        }
        
        // Check cache first
        if (moduleCache[modulePath]) {
            if (globalThis.__write_to_file && modulePath.includes('fundamentals')) {
                globalThis.__write_to_file('[ModuleCache] HIT for: ' + modulePath);
            }
            return moduleCache[modulePath].exports;
        }

        if (globalThis.__write_to_file && modulePath.includes('fundamentals')) {
            globalThis.__write_to_file('[ModuleCache] MISS for: ' + modulePath);
        }

        // Special handling for reflect-metadata to avoid reloading it if it's already global
        // This prevents resetting the metadata store if the library is loaded multiple times
        if (modulePath.includes('reflect-metadata/Reflect.js') && globalThis.Reflect && globalThis.Reflect.metadata) {
            moduleCache[modulePath] = { exports: {} };
            return moduleCache[modulePath].exports;
        }

        // Check if we're already loading this module (circular dependency)
        if (loadingModules.has(modulePath)) {
            // Return a placeholder that will be filled in when the module finishes loading
            // Create an empty exports object that will be populated
            if (!moduleCache[modulePath]) {
                moduleCache[modulePath] = { exports: {} };
            }
            return moduleCache[modulePath].exports;
        }

        // Mark this module as being loaded
        loadingModules.add(modulePath);

        try {
            // Read JavaScript source from filesystem
            let jsCode;
            try {
                jsCode = __readJavaScriptFile(modulePath);
            } catch (error) {
                throw new Error(`Failed to read module '${modulePath}': ${error.message}`);
            }

            // Create module context
            const module = { exports: {} };
            const exports = module.exports;
            
            // Cache the module immediately (before execution) to handle circular dependencies
            moduleCache[modulePath] = module;
            
            // Create a require function for this module
            const moduleRequire = function(specifier) {
                // Handle built-in polyfills
                if (specifier === 'react') {
                    return getReactShim();
                }
                if (specifier === 'react/jsx-runtime') {
                    return {
                        jsx: function(type, props) {
                            return { type: type, props: props || {} };
                        },
                        jsxs: function(type, props) {
                            return { type: type, props: props || {} };
                        },
                        Fragment: 'Fragment'
                    };
                }
                
                const resolvedPath = resolveModulePath(specifier, modulePath);
                return loadModule(resolvedPath, modulePath);
            };
            
            // Wrap in function to provide CommonJS module scope
            const moduleFunction = new Function('require', 'module', 'exports', '__dirname', '__filename', jsCode);
            
            // Execute module
            const dirname = modulePath.substring(0, modulePath.lastIndexOf('/'));
            moduleFunction(moduleRequire, module, exports, dirname, modulePath);
            
            return module.exports;
        } finally {
            // Remove from loading set after execution completes
            loadingModules.delete(modulePath);
        }
    }

    // Minimal React shim for type compatibility
    function getReactShim() {
        return {
            createElement: function() { return {}; },
            Component: class Component {},
            useState: function() { return [null, function() {}]; },
            useEffect: function() {},
            useMemo: function(fn) { return fn(); },
            useCallback: function(fn) { return fn; },
            useContext: function() { return {}; },
            createContext: function() { return {}; }
        };
    }

    // Global require function
    globalThis.require = function(specifier) {
        try {
            // Handle built-in polyfills
            if (specifier === 'react') {
                return getReactShim();
            }
            
            // Handle react/jsx-runtime - used by JSX transpiled code
            if (specifier === 'react/jsx-runtime') {
                return {
                    jsx: function(type, props) {
                        return { type: type, props: props || {} };
                    },
                    jsxs: function(type, props) {
                        return { type: type, props: props || {} };
                    },
                    Fragment: 'Fragment'
                };
            }
            
            // Handle relative imports (DTO classes) - these don't have a fromPath context
            // Instead of creating empty stubs, return the class from globalThis where it was
            // exported by the module wrapper (with all its decorator metadata intact!)
            if (specifier.startsWith('./') || specifier.startsWith('../')) {
                // Extract the class name from the path
                const className = specifier.split('/').pop().replace(/\.js$/, '');
                // Return the class from globalThis if it exists, otherwise return empty stub
                if (globalThis[className]) {
                    return {
                        [className]: globalThis[className]
                    };
                }
                // Fallback to empty constructor if class not found in globalThis
                return {
                    [className]: class {}
                };
            }
            
            const modulePath = resolveModulePath(specifier);
            return loadModule(modulePath);
        } catch (error) {
            console.error(`Error loading module '${specifier}':`, error.message);
            throw error;
        }
    };

    // Mock fetch for testing
    globalThis.fetch = function(url, options) {
        return new Promise(function(resolve, reject) {
            if (globalThis.__fetchHandler) {
                globalThis.__fetchHandler(url, options, resolve, reject);
            } else {
                reject(new Error('No fetch handler configured'));
            }
        });
    };

    // Export for debugging
    globalThis.__moduleRegistry = {
        cache: moduleCache,
        loadingModules: loadingModules,
        require: globalThis.require
    };
})();
