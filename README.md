# Správa projektov firmy

Testovací príklad (.NET) — jednoduchá aplikácia na správu projektov firmy s CRUD operáciami, prihlásením a XML úložiskom.

> **Stav:** Návrhová fáza. Implementácia zatiaľ nezačala.

## Dokumentácia

- [Design dokument](docs/2026-06-18-sprava-projektov-design.md) — architektúra, rozhodnutia a ich odôvodnenie.

## Plánovaný stack (zhrnutie)

- **Backend:** ASP.NET Core (.NET 10) Web API
- **Frontend:** Blazor WebAssembly
- **Architektúra:** Clean Architecture (proporcionálne) + vertical slices, trojvrstvové oddelenie
- **Úložisko:** XML súbory za rozhraním `IProjectRepository` (pripravené na výmenu za DB/REST/Cloud)
- **Autentifikácia:** self-issued JWT, heslo ako hash+salt v konfiguračnom XML
- **Logovanie:** Serilog (štruktúrované)
- **Testy:** xUnit, FluentAssertions, NSubstitute

Podrobnosti a odôvodnenie výberov v [design dokumente](docs/2026-06-18-sprava-projektov-design.md).

## Spustenie

Pokyny na konfiguráciu a spustenie (lokálne cez IDE aj `docker-compose`) budú doplnené s implementáciou.
