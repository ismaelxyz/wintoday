# WinToday – Juego de Ruleta (Full‑Stack .NET 8 + Vue 3)

> Prueba Técnica – Ismael Belisario

WinToday es una pequeña aplicación de ejemplo que demuestra cómo construir un flujo de juego tipo ruleta con registro (login liviano por nombre), giros, apuestas, cálculo de resultados, persistencia de rondas / transacciones y un cliente SPA moderno en Vue 3 + Vite. El objetivo principal del proyecto es ilustrar un backend limpio y predecible con Entity Framework Core sobre PostgreSQL y un frontend simple que consume la API vía fetch, manteniendo tipado y normalización de respuestas. No pretende ser una simulación exacta de ruleta tradicional: las reglas de pago y asignación de color están simplificadas para enfocarnos en el modelado de dominio, consistencia transaccional y ergonomía de la API.

El flujo básico: un jugador ingresa un nombre (si no existe se crea con fondos iniciales configurables), solicita un giro (se genera un número 0–36 y un color aleatorio Red/Black independiente del número), luego decide una apuesta sobre la ronda recién creada (color, color+paridad o número exacto+color). Al confirmar la apuesta se debitan fondos, se evalúa el resultado, se registra la transacción y se devuelve el nuevo balance junto al detalle de la apuesta. También se puede consultar histórico de apuestas y guardar en lote ("save-session") una serie de apuestas simuladas previamente en el cliente.

## Arquitectura y Componentes

Backend (`wintoday.Server`):
- ASP.NET Core 8 minimal hosting + Controllers.
- Entity Framework Core + Npgsql. Tablas: Players, Rounds, Bets, BalanceTransactions.
- Servicio de dominio `GameService` que concentra reglas: creación de jugador con fondos iniciales, spin, commit de apuesta, histórico y batch save.
- Opciones configurables vía sección `Game` (ej. `InitialFunds`).
- Swagger/OpenAPI habilitado en Development.

Frontend (`wintoday.client`):
- Vite + Vue 3 (TypeScript) con proxy HTTPS hacia el backend durante desarrollo.
- Normalización manual de payloads (`api.ts`) para robustez frente a cambios.
- Componente de ruleta (biblioteca `vue3-roulette`) + formulario de apuestas.

Persistencia:
- PostgreSQL (puede usarse contenedor local o servicio administrado). Una cadena por defecto de fallback en código: `Host=localhost;Port=5432;Database=wintoday;Username=postgres;Password=postgres` cuando no se especifica `ConnectionStrings:DefaultConnection`.

## Reglas de Apuesta (Simplificadas)
Tipo `color`: paga 0.5× del wager si coincide el color.
Tipo `colorParity`: requiere color + paridad (even/odd). Paga 1× del wager si acierta ambos.
Tipo `exact`: número exacto + color. Paga 3× el wager.
El wager siempre se descuenta primero. Si la apuesta gana, se añade de vuelta el wager + la ganancia (profit calculado). Esto permite registrar transacciones claras: débito (wager) y crédito (payout) separado.


Las respuestas devuelven identificadores (GUIDs) permitiendo correlación y potencial auditoría futura.

## Configuración y Variables de Entorno

Ahora se proporcionan dos archivos separados:

- `wintoday.Server/.env.example`: variables específicas del backend (.NET). Úsalo como guía y exporta realmente las variables o gestiona secretos con `dotnet user-secrets` / tu plataforma. Claves clave: `ConnectionStrings__DefaultConnection`, `Game__InitialFunds`, y opcionalmente `ASPNETCORE_URLS` / `ASPNETCORE_HTTPS_PORT`.
- `wintoday.client/.env.example`: variables del cliente Vite. Copia a `.env` o `.env.local`. Principalmente `VITE_API_BASE` (dejar vacío para proxy) y `DEV_SERVER_PORT`.

Certificados de desarrollo: el `vite.config.ts` crea/usa certificados exportados con `dotnet dev-certs https`; si los eliminas se regeneran al volver a ejecutar `npm run dev`.

## Preparar Entorno Local
1. Requisitos:
	- .NET SDK 8.x
	- Node.js 20 LTS (o >=22 según `engines`)
	- PostgreSQL local (o contenedor / servicio remoto) con una base disponible.
2. Clonar el repositorio y ubicarse en la carpeta raíz.
3. (Opcional) Editar el archivo `wintoday.Server/appsettings.Development.json` propio o usar variables de entorno para sobreescribir la cadena de conexión y opciones del juego.
4. Restaurar dependencias backend: `dotnet restore`.
5. Aplicar migraciones iniciales a la base de datos.
6. Instalar dependencias frontend: `npm install` dentro de `wintoday.client`.
7. Ejecutar ambos (ver secciones siguientes).

## Base de Datos / Migraciones
Instalar la herramienta EF (una sola vez global):
```
dotnet tool install --global dotnet-ef
```
Aplicar migraciones existentes:
```
cd wintoday.Server
dotnet ef database update
```
Crear una nueva migración (si cambias el modelo):
```
dotnet ef migrations add NombreMigracion
dotnet ef database update
```

## Ejecutar el Backend
Desde la raíz o carpeta del proyecto:
```
cd wintoday.Server
dotnet run
```
Esto levantará la API (HTTPS por puerto asignado en launchSettings o dinámico). En Development verás Swagger en `/swagger`.

Si configuras un puerto específico (ejemplo 7290):
```
set ASPNETCORE_URLS=https://localhost:7290;http://localhost:5290
dotnet run
```

## Ejecutar el Frontend (Dev)
En otra terminal:
```
cd wintoday.client
npm install
npm run dev
```
El servidor Vite arrancará en HTTPS usando el certificado generado. Las llamadas a `/api/...` se proxificarán al backend (puerto detectado por variables ASP.NET o fallback `https://localhost:7290`). Si necesitas apuntar a una API remota diferente, establece `VITE_API_BASE` antes de ejecutar:
```
set VITE_API_BASE=https://mi-api-remota.example.com
npm run dev
```

## Build Producción
Backend (opcional publish):
```
dotnet publish -c Release -o publish
```
Frontend:
```
cd wintoday.client
npm run build
```
El resultado estará en `wintoday.client/dist`. Puedes servirlo: copiar a una carpeta estática del backend o configurar un hosting separado (ajusta entonces `VITE_API_BASE` en tiempo de build o con variables runtime si usas inyección de env). Actualmente el backend ya incluye `UseDefaultFiles` + `UseStaticFiles` + fallback a `/index.html`, por lo que puedes integrar el build copiando `dist` a la carpeta `wwwroot` (si la creas y configuras) o publicando un proyecto combinado (requiere adaptar csproj para incluir archivos estáticos del cliente).

## Seguridad y Notas
- No dejes credenciales reales en `appsettings.Development.json` al hacer público el repo. Usa variables de entorno o un secret manager.
- Las reglas de pago son arbitrarias y no representan probabilidades justas de un casino real.
- No hay autenticación fuerte; el "login" es solo un identificador de jugador. Para entornos serios agregaría identidad/JWT.
- No hay control de concurrencia sofisticado más allá de transacciones; escenarios de multi-sesión simultánea del mismo jugador podrían requerir row-level locking.
