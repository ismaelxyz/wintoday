This file explains how Visual Studio created the project.

The following tools were used to generate this project:

- create-vite

The following steps were used to generate this project:

- Create vue project with create-vite: `npm init --yes vue@latest wintoday.client -- --eslint  --typescript `.
- Update `vite.config.ts` to set up proxying and certs.
- Add `@type/node` for `vite.config.js` typing.
- Add `shims-vue.d.ts` for basic types.
- Create project file (`wintoday.client.esproj`).
- Create `launch.json` to enable debugging.
- Add project to solution.
- Update proxy endpoint to be the backend server endpoint.
- Add project to the startup projects list.
- Write this file.
