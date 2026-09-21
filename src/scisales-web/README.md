# scisales-web

The frontend of the SCI Sales catalog. React 19 with TypeScript, built by Vite.

```bash
npm install
npm run dev     # http://localhost:5173, proxies /api to http://localhost:5163
npm run build   # static files in dist/, copied into the API's wwwroot by the Dockerfile
```

Point the proxy somewhere else with `VITE_API_URL`. The rest is in the README at
the root of the repository.
