# Správa projektov firmy

Jednoduchá aplikácia na správu projektov firmy: prihlásenie, zoznam projektov a CRUD operácie (nový / editácia / zmazanie), s XML úložiskom za vymeniteľným rozhraním. Testovací príklad k pohovoru (.NET).

- **Backend:** ASP.NET Core (.NET 10) Web API
- **Frontend:** Blazor WebAssembly
- **Architektúra:** Clean Architecture (proporcionálne) + vertical slices, trojvrstvové oddelenie
- **Úložisko:** XML súbory za `IProjectRepository` (pripravené na výmenu za DB/REST/Cloud)
- **Autentifikácia:** self-issued JWT; heslo ako hash+salt v konfiguračnom XML
- **Logovanie:** Serilog (štruktúrované, console + rolling file)
- **Testy:** xUnit + FluentAssertions + NSubstitute + `WebApplicationFactory` (39 testov)

## Dokumentácia

- [Design dokument](docs/2026-06-18-sprava-projektov-design.md) — architektúra, rozhodnutia a ich odôvodnenie.
- [Implementačný plán](docs/2026-06-18-sprava-projektov-implementation-plan.md) — task-by-task postup s kódom.

## Predpoklady

- **.NET 10 SDK** (10.0.2xx alebo novší)
- Voliteľne **Docker** (Docker Desktop / Engine 28+) pre beh cez kontajnery

## Prihlasovacie údaje (demo)

```
používateľ: admin
heslo:      Admin123!
```

Toto je zdokumentovaný demo účet potrebný na spustenie. V `config.xml` je uložený len **PBKDF2 hash** hesla (nie plaintext). Podpisový kľúč JWT (`Auth:SigningKey`) je **secret** a nikdy nie je v repozitári — lokálne sa berie z user-secrets, v kontajneri z premennej prostredia.

## Konfigurácia

| Nastavenie | Kde | Poznámka |
|---|---|---|
| Cesta k XML úložisku | `config.xml` → `Storage:ProjectsFilePath` | default `../../data/projects.xml` |
| Používateľ + hash hesla | `config.xml` → `Auth:Username` / `Auth:PasswordHash` | hash, nie plaintext |
| Issuer / Audience / expirácia tokenu | `config.xml` → `Auth:*` | |
| **JWT podpisový kľúč (secret)** | user-secrets / env `Auth__SigningKey` | min. 32 znakov (fail-fast guard) |
| API URL pre klienta | `src/ProjectManager.Client/wwwroot/appsettings.json` → `ApiBaseUrl` | klient ho číta za behu |
| CORS origin klienta | `appsettings.json` → `Cors:ClientOrigin` / env `Cors__ClientOrigin` | |

Pred prvým spustením API nastav podpisový kľúč (lokálne, raz):

```bash
cd src/ProjectManager.Api
dotnet user-secrets set "Auth:SigningKey" "dev-only-signing-key-please-change-min-32-chars-1234"
cd ../..
```

## Spustenie

### A) Lokálne cez IDE / dva terminály (bez Dockera)

Spusti API a klienta súčasne (dva startup projekty v IDE, alebo dva terminály):

```bash
dotnet run --project src/ProjectManager.Api      # http://localhost:5186
dotnet run --project src/ProjectManager.Client   # http://localhost:5150
```

Otvor URL klienta v prehliadači → presmeruje na `/login` → prihlás sa demo údajmi.

Klient volá API na `ApiBaseUrl` z `wwwroot/appsettings.json` (default `http://localhost:5186`, čo zodpovedá http profilu API).

### B) Docker Compose

```bash
docker compose -f deploy/docker-compose.yml up --build
```

- **Klient (nginx)**: http://localhost:8080
- **API**: http://localhost:5186

**Prečo práve tieto porty:** Blazor WASM beží v prehliadači a volá `ApiBaseUrl` z host počítača. Aby jedna hodnota `ApiBaseUrl` fungovala v lokálnom dev aj v compose, API je v compose publikované na host porte **5186** (rovnako ako lokálny http profil), a klient sa servíruje cez nginx na **8080** (čo je `Cors__ClientOrigin`). Ak chceš klienta prepojiť na iné API, zmeň `ApiBaseUrl` v `wwwroot/appsettings.json`.

`data/projects.xml` je pripojený ako **volume** (`../data:/data`), takže dáta prežijú reštart kontajnera a cesta k úložisku je konfigurovateľná cez `Storage__ProjectsFilePath`.

Zastavenie: `docker compose -f deploy/docker-compose.yml down`.

## Testy

```bash
dotnet test
```

Pokrýva doménu, aplikačné use-cases (List/Create/Update/Delete/Login), XML úložisko vrátane súbežného zápisu, a API integračne (auth 401/200, validácia 400, CRUD) cez `WebApplicationFactory`. UI testy sú zámerne mimo rozsahu (pozícia je backend-focused) — viď design dokument.

## Logy

Štruktúrované logy idú na **console** (stdout — zoberie ich Docker) a do **rolling file** v `src/ProjectManager.Api/logs/`. Heslá ani tokeny sa nelogujú.

## Rozsah a možné rozšírenia

Riešenie cieli na ~8 h a vedome nerobí veci, ktoré by boli pri tomto zadaní over-engineering (DB, MediatR, cache úložiska, refresh tokeny, multi-user, CI/CD). Sú navrhnuté ako jednoduché rozšírenia — detaily a odôvodnenie v [design dokumente](docs/2026-06-18-sprava-projektov-design.md) (§13–14). **.NET Aspire** je plánovaný ako voliteľná vrstva navrchu pre orchestráciu a observability; aplikácia funguje aj bez neho.
