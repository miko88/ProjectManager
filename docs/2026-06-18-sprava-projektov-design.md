# Design dokument — „Správa projektov firmy"

- **Dátum:** 2026-06-18
- **Autor:** Michal
- **Kontext:** Testovací príklad k pohovoru (.NET). Pozícia je **backend-focused**, nie fullstack.
- **Cieľ dokumentu:** Zachytiť architektonické rozhodnutia a ich odôvodnenie. Tento dokument zároveň slúži ako podklad pre sprievodný „justification" dokument, ktorý zadanie vyžaduje (zdôvodnenie výberu prostredia, nástrojov a knižníc).

---

## 1. Zhrnutie zadania

Jednoduchá aplikácia **Správa projektov firmy**:

- Prístup chránený **prihlasovacím procesom**.
- Po prihlásení: **zoznam projektov** + menu so základnými operáciami (zoznam, nový, editácia, zmazanie projektu) — CRUD.
- Dáta **iba v XML súboroch** (`projects.xml` dodaný). **Žiadna DB ani jej časť.**
- **Trojvrstvová architektúra** — oddelená dátová / aplikačná / prezentačná vrstva.
- Prezentačná technológia podľa výberu **s odôvodnením**; C#; .NET Core / .NET 8+ LTS.
- Navrhnúť tak, aby sa **úložisko dalo ľahko vymeniť** (DB, REST API, web. služby, Cloud) a aby bol možný **prístup k aplikačnej logike cez webové služby**.
- **Heslo a konfigurácia** (cesta k XML, …) v **konfiguračnom XML súbore**.
- **Logovanie** je povinné.
- Dôraz na: OOP zásady, čitateľnosť/štruktúru, **minimalizáciu vzniku chýb**, programátorskú dokumentáciu, **použitie existujúcich knižníc**, **ošetrenie vstupných dát**.
- Mínus za: neprehľadnosť, **duplicitu kódu**, **neošetrené/nevhodne ošetrené chyby**, nesplnenie požiadaviek.
- Odhad času: **~8 h** (pri prekročení zdôvodniť).

**Štruktúra projektu v XML:** `id`, `name`, `abbreviation`, `customer`.

---

## 2. Vodiaci princíp: seniorita = úsudok, nie maximalizmus

Zadanie explicitne žiada **jednoduchosť** a penalizuje prekombinovanie. Kalibrácia tohto riešenia je **„something in between"** — nech vidno úroveň, ale bez zbytočnej exotiky.

Najsilnejší signál seniority v takomto zadaní nie je počet vzorov, ale **vedomé rozhodnutia a ich zdôvodnenie**: čo som spravil, čo som **zámerne nespravil**, a ako by to škálovalo ďalej (viď §13–14). Tento princíp riadi všetky rozhodnutia nižšie — každý vzor musí „zaplatiť nájom".

---

## 3. Architektúra

### 3.1 Štýl: Clean Architecture (proporcionálne) + Vertical Slices

Clean Architecture a Vertical Slices nie sú konkurenti — sú to **kolmé osi**:

- **Clean Architecture** určuje **smer závislostí** (dovnútra; doména v strede, infraštruktúra je vonkajší detail).
- **Vertical Slices** určujú **organizáciu kódu vnútri vrstiev** (podľa feature, nie podľa technického typu).

Toto riešenie kombinuje obe: **Clean Architecture ako skelet závislostí + vertical-slice organizácia vnútri Application/Api**, aplikované striedmo (žiadny MediatR, žiadne mapper-rozhrania pre každý typ).

### 3.2 Prečo Clean a nie doslovná „trojvrstvovka"

Klasická three-tier má závislosti **nadol** (biznis závisí na dátovej vrstve) → výmena XML za DB znamená zásah do biznis vrstvy. Clean Architecture tento smer **obracia**: dátová vrstva je adaptér závislý dovnútra na porte `IProjectRepository`. **Výmena úložiska = nová implementácia portu + jeden riadok v DI.**

Inými slovami: **dependency inversion je presne mechanizmus, ktorý robí požiadavku zadania („pripravené na efektívnu zmenu úložiska") triviálnou.** Clean nie je odbočka od zadania, je to *lepší spôsob ako splniť jeho vlastnú požiadavku.*

### 3.3 Mapovanie na slovník zadania

| Zadanie (3 vrstvy) | Clean Architecture |
|---|---|
| dátová | Infrastructure (`XmlProjectRepository`) — vonkajší adaptér |
| aplikačná | Domain + Application (use-cases za portami) |
| prezentačná | Api + Client (Blazor) |

### 3.4 Štruktúra riešenia

```
ProjectManager.sln
├─ src/
│  ├─ ProjectManager.Domain          // entita Project, value objekty, doménové pravidlá; žiadne závislosti
│  ├─ ProjectManager.Application     // use-cases (slice-y), DTO, validácia, porty (rozhrania)
│  │                                 //   IProjectRepository, IUserAuthenticator, Result<T>
│  ├─ ProjectManager.Infrastructure  // XmlProjectRepository, XML config, hashovanie hesla, file-locking
│  ├─ ProjectManager.Api             // ASP.NET Core Web API: endpoints, auth, DI, middleware, mapping
│  └─ ProjectManager.Client          // Blazor WebAssembly (prezentačná vrstva)
└─ tests/
   ├─ ProjectManager.Domain.Tests
   ├─ ProjectManager.Application.Tests
   ├─ ProjectManager.Infrastructure.Tests
   └─ ProjectManager.Api.Tests        // integračné cez WebApplicationFactory
```

**Vertical-slice organizácia vnútri Application/Api:**

```
ProjectManager.Application
└─ Features/
   ├─ Projects/
   │  ├─ CreateProject/   (Command, Validator, Handler, Result)
   │  ├─ UpdateProject/
   │  ├─ DeleteProject/
   │  └─ ListProjects/
   └─ Auth/
      └─ Login/           (Command, Handler → vydá JWT)

ProjectManager.Api
└─ Endpoints/
   ├─ ProjectsEndpoints.cs   (minimal API → volá handlery)
   └─ AuthEndpoints.cs
```

**Pravidlá:** Doména nič nevie o XML ani o ASP.NET. Application definuje porty, Infrastructure ich implementuje. Client komunikuje len cez HTTP API (DTO), nepozná Infrastructure.

---

## 4. Hosting model: Blazor WebAssembly + samostatné ASP.NET Core Web API

**Voľba:** Blazor WASM (Client) + samostatné Web API (Api).

**Odôvodnenie:**

- Frontend reálne volá HTTP API → hint zadania o „prístupe k aplikačnej logike cez webové služby" je **demonštrovaný naživo**, nie len teoreticky.
- Jasná hranica služby = čistá hranica pre auth (token cez hranicu) a pre budúce rozšírenia.
- Backend-focused pozícia: hĺbka ide do API/Application/Domain/Infrastructure; FE je funkčné a jednoduché.

**Alternatívy zvážené:** Blazor Server (jeden proces) — jednoduchšie, ale „web service" hranica len teoretická. Hosted WASM (API servíruje statiku) — jeden kontajner bez CORS, ale zväzuje deployment FE a API.

---

## 5. Autentifikácia

**Model:** **Self-issued JWT** — token podpisujeme my, žiadny externý IdentityServer/OAuth (zámerne, viď §14).

**Flow:**

1. Klient → `POST /auth/login` (meno + heslo).
2. API overí credentials cez **`IUserAuthenticator`** (port).
3. Pri úspechu API **vygeneruje podpísaný JWT** (expiry, claims).
4. Klient posiela `Authorization: Bearer <token>` na každý request.
5. API token **reálne validuje** cez JWT bearer middleware + `[Authorize]` na endpointoch.

**„Mock" je len zdroj používateľa, nie token.** Implementácia `IUserAuthenticator` číta **jedného používateľa z konfiguračného XML**; heslo uložené ako **hash + salt** (ASP.NET Core `PasswordHasher` / PBKDF2), nikdy nie plaintext. Token a jeho validácia sú plnohodnotné.

> Detail na zdôraznenie: zadanie naivne hovorí „heslo v XML"; riešenie ukazuje, *prečo* sa ukladá hash, nie plaintext.

**Konzistencia vzoru:** user-store je mock za rozhraním — **rovnaký vzor ako úložisko** → výmena za reálny IdP/DB neskôr je triviálna.

**Blazor strana:** `AuthenticationStateProvider` (stav prihlásenia) + `DelegatingHandler` (pripína token); UI podľa stavu (login / skryté menu). Token v pamäti (+ podľa potreby `sessionStorage`).

**Secrets:** JWT signing key a hash hesla mimo repa — lokálne user-secrets, v Dockeri env var / volume. `config.xml` drží len cestu a nesenzitívne nastavenia (resp. hash, nie plaintext heslo).

**Mimo rozsahu (zámerne):** refresh tokeny, multi-user, roly — viď §14.

---

## 6. Dátová vrstva (úložisko)

**Port (Application):** `IProjectRepository` — `GetAll`, `GetById`, `Add`, `Update`, `Delete`. Čisto doménové typy, žiadny náznak XML.

**Adapter (Infrastructure): `XmlProjectRepository`:**

- **Read stratégia: read-through per operácia** — číta/zapisuje súbor pri každej operácii. Súbor je malý → réžia zanedbateľná, **žiadna trieda chýb so stale-cache**, vždy konzistentné s diskom. (Cache je vedome vynechaná predčasná optimalizácia — viď §14.)
- **Súbežnosť + integrita:** zápisy serializované cez `SemaphoreSlim(1,1)`; **atomický zápis** (zápis do temp súboru → `File.Replace`/`Move`). Súbor sa nikdy nenechá v polovičnom stave ani pri súbehu.
- **Odolnosť na vstup:** chýbajúci/nečitateľný súbor, **poškodený XML**, kódovanie `windows-1250` (zo zadania) → zachytené, zalogované, jasná chyba namiesto pádu.
- **Generovanie ID:** keďže sú zápisy serializované, bezpečná schéma `max+1` (`prj6`…), drží konvenciu zo zadania.

---

## 7. Konfigurácia

Zadanie doslova chce „heslo a konfiguráciu (cesta k XML…) vo **vhodnom konfiguračnom XML súbore**".

- **Vstavaný XML config provider** (`Microsoft.Extensions.Configuration.Xml`) → `config.xml` načítaný cez `AddXmlFile(...)`.
- Typovo sprístupnené cez **Options pattern** (`IOptions<StorageOptions>`, `IOptions<AuthOptions>`).
- Splní literu zadania vstavanými prostriedkami + typová bezpečnosť.
- Citlivé hodnoty (signing key) idú cez user-secrets/env, nie do repa; `config.xml` drží cestu k XML, expiry, hash hesla.

---

## 8. Spracovanie chýb a validácia

Rozdelenie: **očakávané** chyby (biznis výsledky) vs **neočakávané** (IO zlyhanie, bug).

1. **Doménové invarianty (Domain)** — entita `Project` sa **nedá skonštruovať v nevalidnom stave** (guard clauses / value objekty: `name` povinné a neprázdne). Posledná poistka.
2. **Validácia vstupu (Application)** — **FluentValidation** validator per command, beží *pred* handlerom (formát, dĺžky, povinné polia, unikátnosť skratky).
3. **Application vracia `Result<T>`** (vlastný typ) — `Success / Invalid / NotFound / Conflict`. **Žiadne riadenie toku cez výnimky.**
4. **Api mapuje `Result` → HTTP + `ProblemDetails`** (RFC 7807) — `Success→200/201`, `Invalid→400`, `NotFound→404`, `Conflict→409`, `auth→401`. Konzistentný error kontrakt.
5. **Globálny exception handler** (`IExceptionHandler`, .NET 8+) — chytí neočakávané → **zaloguje** → `500 ProblemDetails` **bez úniku internals** (žiadny stack trace klientovi).
6. **Client (Blazor)** — `EditForm` s validáciou (UX); **parsuje `ProblemDetails`** a zobrazí ľudsky; nikdy surovú chybu. API je zdroj pravdy.
7. **XML-špecifické chyby** — chýbajúci/poškodený súbor, súbeh → ošetrené v repozitári (viď §6).

**Result pattern:** vlastný `Result<T>` (malý immutable typ so stavom + chybami) — zámerne bez knižnice pri triviálnej veci; ukazuje pochopenie podstaty.

---

## 9. Logovanie

**Princíp:** v kóde abstrakcia **`ILogger<T>`** (`Microsoft.Extensions.Logging`); provider vymeniteľný — rovnaká dependency-inversion logika ako inde.

**Provider:** **Serilog** — štruktúrované logovanie, enrichers, `UseSerilogRequestLogging`, console + rolling file out-of-the-box. Premostiteľné na OpenTelemetry/Aspire neskôr (OTLP sink).

**Štruktúrované, nie konkatenácia:** `LogInformation("Project {ProjectId} created by {User}", id, user)`.

**Úrovne:**
- **Information** — biznis udalosti (projekt vytvorený/upravený/zmazaný, úspešné prihlásenie).
- **Warning** — očakávané zlyhania (validácia neprešla, nenájdené, **neúspešné prihlásenie**).
- **Error** — neočakávané výnimky (z globálneho handlera).

**Kde:** aplikačné handlery + globálny exception handler + request-logging middleware. **Doména ostáva čistá.** Repozitár loguje IO warnings.

**Bezpečnosť v logoch:** nikdy nelogovať **heslo, token ani hash** (hlavne login endpoint) — vedomá redakcia.

**Sinky:** Console (stdout → Docker/Aspire) + rolling file (konkrétny dôkaz logovania).

---

## 10. Testovanie

Testovacia pyramída mapovaná na vrstvy; cieľ je **biznisovo kritické cesty**, nie 100 % pokrytie.

| Úroveň | Čo | Mock? | Priorita |
|---|---|---|---|
| **Domain** | invarianty entity, value objekty | nie | rýchle, lacné |
| **Application** | handlery/use-cases, `Result` výsledky, validátory | `IProjectRepository` (NSubstitute) | **#1** |
| **Infrastructure** | `XmlProjectRepository` proti **reálnym temp súborom**: round-trip, poškodený XML, atomický a súbežný zápis | **nie** (proti realite) | **#2** |
| **Api** | endpoint end-to-end vrátane auth (401/200), validácia→400, 404, 409 | `WebApplicationFactory` | **#3** |

**Nástroje:** xUnit, FluentAssertions, **NSubstitute**, `Microsoft.AspNetCore.Mvc.Testing`.

**Čo zámerne netestujem:** framework/Serilog/FluentValidation samotné; triviálne DTO/gettery; nemockujem to, čo testujem proti realite. **Testcontainers nepotrebujeme** (žiadna DB).

**UI / bUnit testy — zámerne vynechané:** pozícia je backend-focused (nie fullstack), UI testy sú časovo drahé a krehké. FE je overené manuálne; depth investovaná do backendu. (Viď §14.)

---

## 11. Spustenie a deployment

- **Lokálne cez IDE** — baseline, nulové závislosti (dva startup projekty: Api + Client). Musí fungovať aj bez Dockera.
- **`docker-compose`** — `web` (nginx servíruje statický WASM build) + `api` (Kestrel). Spoločná sieť, rieši CORS/reverse-proxy. **`projects.xml` + `config.xml` ako mounted volume** → dáta prežijú reštart, demonštruje konfigurovateľnú cestu k úložisku.
- **API URL pre klienta je konfigurovateľná** (nie hardcoded) → funguje lokálne aj v compose.
- **README** popíše obe cesty + konfiguráciu.

**.NET Aspire — zvážené, vyradené:** appka dáva zmysel aj bez neho. Aspire bolo vyskúšané ako vrstva navrchu pre orchestráciu + dashboard/observability (naviaže sa na Serilog → OTLP), no orchestrácia Blazor WASM klienta je zatiaľ len v preview a vyžadovala by invazívne zmeny klienta — pri tomto rozsahu bez úmernej hodnoty, preto vyradené. Aplikácia naň ostáva pripravená (OTLP sink), ale nie je naň viazaná.

---

## 12. Použité knižnice a odôvodnenie

| Knižnica | Účel | Prečo |
|---|---|---|
| ASP.NET Core (.NET 10) | Web API + hosting | LTS-trieda, moderný stack |
| Blazor WebAssembly | Prezentačná vrstva | C# end-to-end, reálna SPA za HTTP API |
| `Microsoft.Extensions.Configuration.Xml` (`AddXmlFile`) | XML konfigurácia | Splní požiadavku „config v XML" vstavanými prostriedkami; súčasť shared frameworku, bez samostatného NuGet balíka |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | JWT validácia | Štandardná, dobre auditovaná |
| `Microsoft.AspNetCore.Identity.PasswordHasher` | Hashovanie hesla | Robustné PBKDF2, netreba vymýšľať vlastné |
| **FluentValidation** | Validácia vstupu | De-facto štandard, čitateľné pravidlá |
| **Serilog** | Štruktúrované logovanie | Najbohatší ekosystém, console+file, OTel-ready |
| **xUnit + FluentAssertions + NSubstitute** | Testy | Štandardná, čitateľná testovacia trojica |
| `Microsoft.AspNetCore.Mvc.Testing` | Integračné testy | In-memory host pre API |

Vlastný `Result<T>` — zámerne bez knižnice (triviálna vec, ukazuje pochopenie).

---

## 13. Možné rozšírenia (ako to škáluje ďalej)

- **Výmena úložiska** XML → DB / REST / Cloud: nová implementácia `IProjectRepository` + riadok v DI. Doména a Application sa nedotknú.
- **Reálny IdP / OAuth:** nová implementácia `IUserAuthenticator`; flow a validácia tokenu ostávajú.
- **Multi-user + roly:** rozšírenie user-store a claims; `[Authorize(Roles=…)]`.
- **.NET Aspire:** orchestrácia + observability dashboard (Serilog → OTLP) — zvážené a zatiaľ vyradené (Blazor WASM orchestrácia len v preview, viď §11/§14); ostáva otvorené ako budúce rozšírenie.
- **Caching** úložiska pri väčšom objeme dát.
- **Refresh tokeny** pre dlhšie session bez re-loginu.

---

## 14. Vedomé rozhodnutia a obmedzenie rozsahu (YAGNI)

Pre hodnotiteľa — čo som **zámerne nespravil** a prečo:

| Vynechané | Dôvod |
|---|---|
| **MediatR / CQRS** | Jeden agregát (Project) + CRUD → réžia bez návratnosti, cargo-cult. Stačia obyčajné handlery. |
| **In-memory cache úložiska** | Predčasná optimalizácia pri malom súbore; pridáva triedu stale-cache chýb, ktoré zadanie penalizuje. |
| **Refresh tokeny** | Mimo 8h; jednoduchý expiry postačuje na demonštráciu. |
| **Multi-user / roly** | Zadanie pýta jedno heslo v configu; abstrakcia umožní rozšírenie neskôr. |
| **Externý IdP / OAuth** | Zámerne — token vydávame a validujeme sami; mock user-store za rozhraním. |
| **bUnit / UI testy** | Backend-focused pozícia; UI testy drahé a krehké; FE overené manuálne. |
| **Testcontainers** | Žiadna DB. |
| **CI/CD** | Mimo rozsahu zadania. |
| **Aspire (akákoľvek vrstva)** | Vyskúšané a vyradené: orchestrácia Blazor WASM klienta je len v preview a vyžaduje invazívne zmeny klienta; appka musí fungovať aj bez neho. |
| **Plná Clean ceremónia** (mapper-rozhrania pre každý typ, DTO na každej hranici) | Pri jednom agregáte over-engineering; ponechané len hranice, ktoré si zaslúžia existenciu. |

---

## 15. Časový rozpočet (~8 h) a priority

Poradie hodnoty, ak by čas tlačil:

1. Domain + Application (use-cases, `Result`, validácia) — jadro.
2. Infrastructure (`XmlProjectRepository` + súbežnosť/atomický zápis).
3. Api (endpoints, auth, error mapping, logovanie).
4. Application + Infrastructure + Api testy.
5. Client (Blazor) — funkčné UI, manuálne overené.
6. Docker-compose + README.

Aspire a prípadné rozšírenia (§13) sú nad rámec 8 h a v dokumente sú vedome označené.
