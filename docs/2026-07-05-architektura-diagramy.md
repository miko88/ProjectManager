# Architektúra – diagramy

Vizuálny prehľad riešenia **Správa projektov**: vrstvy a závislosti medzi projektmi,
princíp dependency inversion a runtime tok pri behu (vrátane nasadenia cez Docker).
Diagramy sú v [Mermaid](https://mermaid.js.org/) — renderujú sa priamo na GitHube.

Podrobné odôvodnenie architektúry je v [design dokumente](2026-06-18-sprava-projektov-design.md).

## 1. Vrstvy a project referencie

Šípka `A --> B` znamená, že projekt `A` má `<ProjectReference>` na projekt `B` (t. j. `A`
závisí od `B`). Prerušované šípky testov (`-.->`) označujú, ktorý projekt daná testovacia
sada pokrýva.

```mermaid
flowchart TD
    subgraph Presentation["Prezentačná vrstva"]
        Client["ProjectManager.Client<br/>(Blazor WASM)"]
        Api["ProjectManager.Api<br/>(ASP.NET Core Web API)"]
    end

    subgraph AppLayer["Aplikačná vrstva"]
        Application["ProjectManager.Application<br/>(use-cases, rozhrania)"]
    end

    subgraph DomainLayer["Doménová vrstva"]
        Domain["ProjectManager.Domain<br/>(entity, pravidlá)"]
    end

    subgraph InfraLayer["Infraštruktúra"]
        Infrastructure["ProjectManager.Infrastructure<br/>(XML úložisko, JWT)"]
    end

    subgraph SharedLayer["Zdieľané"]
        Contracts["ProjectManager.Contracts<br/>(DTO)"]
    end

    Api --> Application
    Api --> Infrastructure
    Api --> Contracts
    Client --> Contracts
    Infrastructure --> Application
    Application --> Domain

    subgraph Tests["Testy (xUnit)"]
        ApiTests["Api.Tests"]
        AppTests["Application.Tests"]
        DomainTests["Domain.Tests"]
        InfraTests["Infrastructure.Tests"]
    end

    ApiTests -.-> Api
    ApiTests -.-> Contracts
    AppTests -.-> Application
    DomainTests -.-> Domain
    InfraTests -.-> Infrastructure

    classDef presentation fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d;
    classDef domain fill:#fef9c3,stroke:#ca8a04,color:#713f12;
    classDef infra fill:#fee2e2,stroke:#dc2626,color:#7f1d1d;
    classDef shared fill:#f3e8ff,stroke:#9333ea,color:#581c87;
    classDef test fill:#f1f5f9,stroke:#64748b,color:#334155;

    class Client,Api presentation;
    class Application app;
    class Domain domain;
    class Infrastructure infra;
    class Contracts shared;
    class ApiTests,AppTests,DomainTests,InfraTests test;
```

**Poznámky:**

- **`Domain`** nemá žiadne odchádzajúce referencie — jadro je zámerne bezzávislostné
  (pravidlo Clean Architecture: závislosti smerujú dovnútra, k doméne).
- **`Contracts`** (zdieľané DTO) tiež nemá odchádzajúce referencie; používa ho `Api`
  (serializácia odpovedí) aj `Client` (deserializácia) na oboch stranách drôtu.
- **`Infrastructure`** závisí od `Application` (nie naopak) — implementuje jej rozhrania.
  Tento obrátený smer je detailne v diagrame č. 2.

## 2. Dependency inversion (rozhranie vs. implementácia)

`Application` definuje rozhrania, ktoré potrebuje, ale **nepozná** ich konkrétnu
implementáciu. Tú dodáva `Infrastructure`. Referenčná šípka teda ide
`Infrastructure --> Application`, no za behu tečú volania opačne (`Application` volá
rozhranie, DI za ním dosadí triedu z `Infrastructure`). Vďaka tomu je úložisko vymeniteľné
(XML → DB/REST/Cloud) bez zásahu do jadra.

```mermaid
flowchart LR
    subgraph AppLayer["ProjectManager.Application"]
        IRepo["IProjectRepository"]
        IToken["ITokenService"]
        IAuth["IUserAuthenticator"]
    end

    subgraph InfraLayer["ProjectManager.Infrastructure"]
        XmlRepo["XmlProjectRepository"]
        JwtSvc["JwtTokenService"]
        ConfigAuth["ConfigUserAuthenticator"]
    end

    XmlRepo -. implementuje .-> IRepo
    JwtSvc -. implementuje .-> IToken
    ConfigAuth -. implementuje .-> IAuth

    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d;
    classDef infra fill:#fee2e2,stroke:#dc2626,color:#7f1d1d;

    class IRepo,IToken,IAuth app;
    class XmlRepo,JwtSvc,ConfigAuth infra;
```

Zapojenie prebieha v `Infrastructure/DependencyInjection.cs` (registrácia implementácií do DI).

## 3. Runtime a nasadenie

End-to-end tok od prehliadača po úložisko. Prerušovaná hranica označuje dva kontajnery
z `deploy/docker-compose.yml`. Klient (Blazor WASM) beží v prehliadači a volá API cez HTTP
s JWT `Bearer` tokenom.

```mermaid
flowchart LR
    Browser["Prehliadač<br/>(používateľ)"]

    subgraph ClientContainer["Kontajner: klient (nginx :8080)"]
        Nginx["nginx"]
        WASM["Blazor WASM<br/>ProjectManager.Client"]
    end

    subgraph ApiContainer["Kontajner: API (:5186)"]
        Endpoints["AuthEndpoints /<br/>ProjectsEndpoints"]
        Handlers["Application<br/>handlery (use-cases)"]
        Infra["Infrastructure<br/>(úložisko + JWT)"]
    end

    ProjectsXml[("data/projects.xml<br/>(Docker volume)")]
    ConfigXml[("config.xml<br/>hash hesla, JWT nastavenia")]

    Browser -->|"HTTP :8080"| Nginx
    Nginx -->|"statické súbory"| WASM
    WASM -->|"HTTP + JWT Bearer :5186"| Endpoints
    Endpoints --> Handlers
    Handlers --> Infra
    Infra -->|"čítanie/zápis XML"| ProjectsXml
    Infra -->|"overenie hesla, podpis tokenu"| ConfigXml

    classDef presentation fill:#dbeafe,stroke:#2563eb,color:#1e3a8a;
    classDef app fill:#dcfce7,stroke:#16a34a,color:#14532d;
    classDef infra fill:#fee2e2,stroke:#dc2626,color:#7f1d1d;
    classDef store fill:#fef9c3,stroke:#ca8a04,color:#713f12;

    class Nginx,WASM,Browser presentation;
    class Endpoints,Handlers app;
    class Infra infra;
    class ProjectsXml,ConfigXml store;
```

**Poznámky:**

- Pri lokálnom behu bez Dockera platí ten istý tok — klient na `:5150`, API na `:5186`
  (viď [README](../README.md), sekcia Spustenie).
- `data/projects.xml` je v compose pripojený ako **volume**, takže dáta prežijú reštart
  kontajnera.
- Heslá a tokeny sa nikdy nelogujú; v `config.xml` je len PBKDF2 hash hesla, JWT podpisový
  kľúč je secret (user-secrets / env premenná).
