# Vite Configuration

When you use Vite's built-in dev server to proxy API requests to a backend running on a different port, WebSocket connections require explicit opt-in.

## WebSocket proxy for the observable query hub

Vite's proxy is built on [http-proxy](https://github.com/http-party/node-http-proxy). By default it only proxies regular HTTP requests. To also forward the WebSocket upgrade handshake that the [observable query hub](./observable-query-multiplexing.md) requires, you must set `ws: true` on every proxy rule whose target serves WebSocket connections.

The hub endpoint is `/.cratis/queries/ws`. Without `ws: true` on the `/.cratis` proxy rule, the browser issues a WebSocket upgrade to the Vite dev server, but Vite returns a plain HTTP response and the connection fails. Observable queries silently receive no data and repeatedly attempt to reconnect.

### Correct configuration

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

Compare with a common incorrect configuration that handles only HTTP:

```ts
// ❌ Missing ws: true — WebSocket connections fail silently
'/.cratis': {
    target: 'http://localhost:5001',
},
```

### Why it is easy to miss

Most proxy rules are added to forward `fetch` and `XHR` traffic. Those work without `ws: true` because they are ordinary HTTP requests. Observable queries are the only part of Arc that upgrade to a WebSocket, so the missing flag goes unnoticed until you observe that live data is never delivered to the frontend.

Vite docs: [Server options — proxy](https://vite.dev/config/server-options.html#server-proxy).

### Checking a full proxy block

A typical Vite setup that exposes both the REST API and the observable query hub looks like this:

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

Neither rule needs `changeOrigin: true` when the backend is on the same machine.
