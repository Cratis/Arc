---
title: Cratis Arc Vite development proxy configuration
description: Configure Vite to forward Cratis Arc API requests and observable-query WebSocket connections to your local backend, then check the hub route.
---

Configure Vite's development proxy when your React frontend and Cratis Arc backend run on different ports. The browser sends requests to Vite, which forwards them to the backend. For observable queries using WebSocket, enable `ws: true` on the matching proxy rule so live query results can reach the frontend.

## WebSocket proxy for the observable query hub

The [observable query hub](./queries/observable-query-multiplexing.md) shares a connection across query subscriptions. In WebSocket hub mode, the client connects to `/.cratis/queries/ws`. The initial HTTP request asks to switch protocols to WebSocket; Vite needs `ws: true` to forward that upgrade to the backend.

These examples use the frontend's current origin and no `apiBasePath`. If you set `<Arc origin>` directly to the backend, requests bypass Vite. A path prefix in `apiBasePath` also prefixes the hub route, so adjust the proxy rule to match it. See [Arc provider configuration](./arc.md#configuration-options) for those settings.

React's `<Arc>` provider defaults to Server-Sent Events (SSE), an HTTP stream. The WebSocket route applies when you select `QueryTransportMethod.WebSocket` and leave `queryDirectMode` as `false`; see [query transport configuration](./queries/configuration.md#observable-query-transport).

### Correct configuration

Merge this `server.proxy` entry into your existing `defineConfig` in `vite.config.ts`, keeping your plugins and other options. Replace `http://localhost:5001` with your backend's listening address.

```ts
// vite.config.ts
export default defineConfig({
    server: {
        proxy: {
            '/.cratis': {
                target: 'http://localhost:5001',
                ws: true,                          // required for WebSocket upgrade
            },
        },
    },
});
```

For example, with Vite at `http://localhost:5173`, the browser connects to `ws://localhost:5173/.cratis/queries/ws`. The `/.cratis` rule forwards that connection to the same path on the backend at `http://localhost:5001`; it does not rewrite the path.

Without the WebSocket flag, the rule below forwards HTTP traffic but does not forward the hub's WebSocket upgrade:

```ts
// ❌ Missing ws: true — WebSocket connections fail silently
'/.cratis': {
    target: 'http://localhost:5001',
},
```

### Why it is easy to miss

An ordinary API request can succeed while the WebSocket connection fails: `fetch` and `XHR` traffic do not need a protocol upgrade. Check the connection itself instead of using a successful API response as proof that the proxy is ready.

After applying the configuration, open a screen that subscribes through the WebSocket hub and inspect the browser's Network panel. Look for `/.cratis/queries/ws` with a `101 Switching Protocols` response and incoming query-result messages. If the connection fails, check that its URL goes to Vite and that the matching rule has `ws: true`. The client logs hub connection errors and attempts to reconnect according to its reconnect policy.

For proxy options, see Vite's [Server options — proxy](https://vite.dev/config/server-options.html#server-proxy) and the [http-proxy documentation](https://github.com/http-party/node-http-proxy).

### Checking a full proxy block

If your generated command and query routes start with `/api`, forward that prefix as well as `/.cratis`. In direct WebSocket mode, queries connect to their own routes instead of the hub, so those routes also need a WebSocket-enabled rule:

```ts
server: {
    proxy: {
        '/api': {
            target: 'http://localhost:5001',
            ws: true,        // required if any /api route also uses WebSocket
        },
        '/.cratis': {
            target: 'http://localhost:5001',
            ws: true,        // required for /.cratis/queries/ws
        },
    },
},
```

The example leaves `changeOrigin` unset. That option changes the forwarded `Host` header; it does not enable WebSocket forwarding or replace the browser's `Origin` header.

If the backend restricts WebSocket origins with `Cratis:Arc:Query:AllowedWebSocketOrigins`, the proxied connection still carries the dev-server origin, for example `http://localhost:5173`. Arc accepts it when it matches the request's scheme, host and port, or an entry in the configured list. With `changeOrigin: true`, or when the frontend and backend use different schemes, add the dev-server origin to that list. An origin rejected by this policy receives `403 Forbidden` during the upgrade.

## Slice locations from component folders

If your React components live in slice folders, `SliceLocationPlugin` from `@cratis/arc.vite/slices` makes their module, feature and slice available through `useSliceContext()`. This is separate from proxy configuration. Follow [resolve from slice folders at build time](./slice-context.md#resolve-from-slice-folders-at-build-time) to configure the folder mapping. The plugin has its own entry point, so importing other plugins from `@cratis/arc.vite` does not load its parser.
