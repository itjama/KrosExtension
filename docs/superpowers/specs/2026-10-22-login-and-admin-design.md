# Prihlásenie, používatelia a admin menu — Design Spec

**Date:** 2026-10-22
**Scope:** Api (autentifikácia, autorizácia, správa používateľov), Web host (HTTPS), React klient (login, menu podľa roly, správa používateľov)
**Mimo scope (neskoršie samostatné projekty):** správa účtov v Olymp/Omega, zálohovanie a obnova databáz

Značka **[builder]** znamená rozhodnutie, ktoré navrhol Claude a používateľ ho výslovne neschválil. Builder ho môže zmeniť, ak na to má dobrý dôvod.

---

## Prečo

LedgerPulse dnes beží na serveri a kolegovia ho otvárajú z prehliadača v lokálnej sieti. Aplikácia nemá žiadne prihlásenie, takže ktokoľvek v sieti vie meniť nastavenia, napríklad SQL servery s heslami alebo sledované firmy. Api navyše počúva na `0.0.0.0:5184` aj so Swaggerom, takže sa dá obísť aj webová stránka.

Cieľ:
- Na stránku sa dostane len prihlásený používateľ (meno + heslo).
- Nastavenia programu a správu používateľov vidí a mení len admin.
- Všetko beží lokálne, bez externých služieb (žiadne AD/SSO, žiadny cloud).

## Ciele

1. Prihlásenie menom a heslom. Bez prihlásenia nie je prístupná stránka ani API.
2. Dve roly: **Admin** a **Používateľ**.
3. Admin zakladá používateľov, resetuje im heslá a blokuje ich. Registrácia neexistuje.
4. Prvý admin je pripravený vopred cez `appsettings` (bootstrap).
5. Bezpečnosť vynucuje **backend (Api)**. Frontend iba skrýva, čo používateľ nesmie vidieť.
6. HTTPS na internej doméne, dostupné len z lokálnej siete.

## Nie sú cieľom

- Napojenie na Active Directory / Windows prihlásenie.
- Obmedzenie, ktoré firmy kto vidí. Zatiaľ vidia všetci všetky firmy, ale dátový model to neskôr nesmie zablokovať.
- Samoobslužný reset hesla (e-mail a pod.). Zabudnuté heslo resetuje admin.
- Správa účtov Olymp/Omega a zálohy. V menu nie sú ani ako placeholdery, pridajú sa vo vlastných projektoch.

## Anti-ciele (zlyhanie, aj keby to „fungovalo")

- Api je stále dostupné zo siete bez prihlásenia, napríklad cez port 5184 alebo Swagger.
- Heslá používateľov sú v DB alebo v logoch v čitateľnej podobe.
- Admin heslo musí natrvalo ostať v `appsettings` v čistom texte, aby aplikácia fungovala.
- Zablokovaný používateľ alebo používateľ s resetnutým heslom môže ďalej pracovať so starou session.
- Admin sa dá omylom vymknúť tak, že nezostane žiadny aktívny admin.
- Bežný používateľ zavolá admin endpoint priamo a uspeje.

---

## Menu a obrazovky (schválené používateľom)

Ľavý panel ostáva dnešný tmavý sidebar.

**Bežný používateľ:**
```
LedgerPulse
  Olymp            (dashboard)
  Omega            (dashboard)
  ...
  <meno> · Zmeniť heslo · Odhlásiť
```

**Admin:**
```
LedgerPulse
  Olymp
  Omega
  Settings ▾        (len admin – nastavenia programu)
    Servery
    Olymp           (sledované firmy + refresh time)
    Omega           (sledované firmy + refresh time)
  Admin ▾           (len admin)
    Používatelia
  ...
  <meno> · Zmeniť heslo · Odhlásiť
```

- Používateľ chce mať Servery, Olymp a Omega v menu **Settings**, lebo sú to nastavenia programu. Celé Settings vidí len admin.
- Menu **Admin** zatiaľ obsahuje len Používateľov. Neskôr pribudnú účty Olymp/Omega a zálohy.
- Bežný používateľ menu Settings ani Admin nevidí vôbec.
- Ručné prepočítanie dát na dashboardoch (`POST /api/{product}/companies/update-data`) ostáva **každému prihlásenému** (schválené).
- Refresh time a výber sledovaných firiem sú **len pre admina** (schválené). Sú globálne pre všetkých a čas ovplyvňuje záťaž SQL serverov.

**Prihlasovacia obrazovka:** názov aplikácie, Meno, Heslo, tlačidlo Prihlásiť, text „Zabudnuté heslo? Kontaktuj admina." Pri chybe sa zobrazí všeobecná hláška „Nesprávne meno alebo heslo", ktorá neprezradí, či meno existuje. Hlášku „Účet je dočasne zamknutý" (423) API vráti **iba vtedy, keď je zadané správne heslo** k zamknutému účtu. So zlým heslom dostane zamknutý účet rovnakú odpoveď 401 ako neexistujúce meno. **[builder]**

**Vynútená zmena hesla:** ak má používateľ príznak `MustChangePassword`, po prihlásení vidí iba formulár na zmenu hesla (staré, nové, nové znova). Kým heslo nezmení, API mu okrem zmeny hesla a odhlásenia všetko odmietne.

**Admin → Používatelia:** tabuľka s menom, rolou, stavom (aktívny / zablokovaný / musí zmeniť heslo) a posledným prihlásením. Akcie sú:
- **+ Nový**: meno, rola (Admin/Používateľ) a dočasné heslo. **[builder]** Dočasné heslo vygeneruje server a ukáže ho adminovi raz, aby ho odovzdal kolegovi.
- **Reset hesla**: nové dočasné heslo sa zobrazí raz, nastaví sa `MustChangePassword` a zrušia sa existujúce session používateľa.
- **Zablokovať / Odblokovať**: namiesto mazania, aby ostala história. Zablokovanie okamžite ukončí session.
- **Zmena roly**: Admin ↔ Používateľ.

Ochrany: admin nemôže zablokovať sám seba ani sebe odobrať rolu Admin. Nikdy nesmie zostať nula aktívnych adminov.

Mockup z brainstormingu: `.superpowers/brainstorm/1058-1792685284/content/login-admin.html` (Admin tam ešte obsahoval Servery a firmy; podľa rozhodnutia vyššie patria do Settings).

---

## Architektúra

### Topológia (dnes → po zmene)

Dnes: `Prehliadač → Web (:5284, http, 0.0.0.0) → proxy /api → Api (:5184, http, 0.0.0.0)`

Po zmene:
- **Web** počúva na HTTPS na internej doméne, **na vlastnom porte, nie na 443** (rozhodnutie používateľa), napr. `https://ledgerpulse.firma.local:5284`. **[builder]** Ponechať port 5284 a zrušiť HTTP listener. Samostatný HTTP port len kvôli presmerovaniu sa neoplatí.
- Nasadenie je **iba ako Windows služby na Windows Serveri** (rozhodnutie používateľa). Docker sa nerieši a existujúci `src/KrosExtension.Web/Dockerfile` ostáva bez zmeny.
- **Api** počúva iba na `localhost`. Zo siete je dostupný len cez proxy vo Webe.
- Autentifikáciu a autorizáciu robí **Api**. Web proxy ostáva „hlúpa". Dnes už preposiela všetky hlavičky oboma smermi, teda aj `Cookie` a `Set-Cookie`. Builder overí, že to platí aj pre viacnásobné `Set-Cookie`.
- **Swagger** sa zapína len v Development. **[builder]** Konfiguračný kľúč `Swagger:Enabled` v produkcii buď odstrániť, alebo ho podmieniť prihláseným adminom.

Aj keď Api počúva len na localhoste, každý endpoint musí aj tak vyžadovať prihlásenie. Ochrana nesmie stáť iba na sieťovom nastavení.

### Autentifikácia

- ASP.NET Core **cookie authentication** v Api. Plné ASP.NET Identity sa nepoužije, je na tento rozsah zbytočne veľké. **[builder]**
- Heslá sa hashujú cez `PasswordHasher<T>` z `Microsoft.Extensions.Identity.Core` (PBKDF2, s verziovaním hashu). **[builder]**
- Cookie: `HttpOnly`, `SameSite=Strict`, `Secure`. V produkcii `CookieSecurePolicy.Always`, lebo Api za proxy vidí http. V Development `SameAsRequest`.
- Session je sliding a vyprší po **8 hodinách** nečinnosti. **[builder]**
- Pri neprihlásenom volaní API vráti **401** a pri chýbajúcej role **403**. Cookie handler nesmie robiť redirect na login stránku.
- **Okamžité zrušenie session:** používateľ má `SecurityStamp`, ktorý sa uloží do cookie ako claim. Pri každom requeste (`OnValidatePrincipal`) sa porovná s DB. Ak nesedí alebo je používateľ zablokovaný, session sa zamietne. Stamp sa mení pri resete hesla, zmene hesla, zablokovaní a zmene roly. **[builder]** Kontrolu je možné krátko cachovať (napr. 30 s), ak by bola záťaž DB problém.
- **Zamknutie po zlých pokusoch:** 5 neúspešných prihlásení zamkne účet na 15 minút. **[builder]** Login endpoint má navyše rate limit na IP (`Microsoft.AspNetCore.RateLimiting`). **[builder]**
  - Ktokoľvek v sieti tak vie adminovi spôsobiť 15-minútový výpadok. Pri internej sieti to akceptujeme. Bootstrap obnova (nižšie) zamknutie zruší.
  - Api za proxy vidí všetky requesty z localhostu. Aby rate limit na IP fungoval, Web proxy doplní `X-Forwarded-For` a Api použije `UseForwardedHeaders`, ktoré dôveruje iba loopbacku. Je to jediná zmena „hlúpej" proxy. **[builder]**
- **CSRF:** `SameSite=Strict` a API, ktoré prijíma iba `application/json`. **[builder]** Antiforgery tokeny sa nepoužijú.

### Autorizácia

- **Fallback policy** v Api vyžaduje prihláseného používateľa, takže každý nový endpoint je chránený automaticky.
- Policy **`Admin`** vyžaduje rolu Admin.
- Policy pre `MustChangePassword`: používateľ s týmto príznakom smie iba `/api/auth/me`, `/api/auth/change-password` a `/api/auth/logout`.
- Anonymne dostupné sú iba `POST /api/auth/login` a `/health`. **[builder]** `/health` vracia len stav, nie detaily.

Rozdelenie dnešných endpointov:

| Endpoint | Prístup |
|---|---|
| `/api/data-read-servers` (GET, POST, DELETE) | Admin |
| `/api/{product}/refresh-settings` (GET, PUT) | Admin |
| `/api/{product}/companies/discover` | Admin |
| `/api/{product}/companies/tracked` (GET, PUT) | Admin |
| `/api/databases/*` (catalog, configured) | Admin |
| `/api/{product}/dashboard/...` (vrátane detailu a orphan-payments) | Prihlásený |
| `/api/{product}/companies/update-data` (POST) | Prihlásený |
| `/api/{product}/companies/statistics` | Prihlásený |
| `/api/companies/data-sources` | **[builder]** zistiť, kto ho volá. Ak len Settings, tak Admin. |
| `/api/system/info` | Prihlásený |

### Nové API

```
POST /api/auth/login             { userName, password }        → 200 + cookie | 401 | 423 (zamknutý)
POST /api/auth/logout                                           → 204
GET  /api/auth/me                                               → { userName, role, mustChangePassword } | 401
POST /api/auth/change-password   { currentPassword, newPassword } → 204 | 400

GET  /api/admin/users                                           → zoznam (bez hashov)
POST /api/admin/users            { userName, role }             → { user, temporaryPassword }
POST /api/admin/users/{id}/reset-password                       → { temporaryPassword }
POST /api/admin/users/{id}/block
POST /api/admin/users/{id}/unblock
PUT  /api/admin/users/{id}/role  { role }
```

Endpointy sa pridajú v štýle existujúcich `*Endpoints.cs` (Minimal API, `MapGroup`, `WithTags`, `WithName`).

**Pravidlá hesla [builder]:** minimálne 10 znakov, bez ďalších požiadaviek na zložitosť. Nové heslo sa nesmie zhodovať so starým.

**Meno:** unikátne bez ohľadu na veľkosť písmen, pred uložením sa orežú medzery.

### Dátový model (DB `KrosExtension`, EF Core migrácia)

Nová entita `AppUser : Entity` (Domain, priečinok napr. `Domain/Users`). **[builder]** Názov tabuľky aj priečinka nechávam na builderovi.

| Vlastnosť | Poznámka |
|---|---|
| `Id` (Guid) | z `Entity` |
| `UserName` | unikátny index, case-insensitive |
| `PasswordHash` | výstup `PasswordHasher` |
| `Role` | enum `UserRole { User, Admin }` |
| `IsBlocked` | |
| `MustChangePassword` | |
| `SecurityStamp` | Guid/string, mení sa pri citlivých zmenách |
| `FailedLoginCount`, `LockoutEndsAt` | zamknutie |
| `CreatedAt`, `LastLoginAt`, `PasswordChangedAt` | `DateTimeOffset` ako v ostatných entitách |

Konfigurácia ide do `Persistence/Configurations/` a migrácia vedľa existujúcich. Migrácie sa aplikujú pri štarte Api ako doteraz.

Aby sa neskôr dalo obmedziť, ktoré firmy kto vidí, netreba nič pridávať teraz. Stačí, že používateľ má vlastné `Id`.

### Bootstrap prvého admina

Konfigurácia v `appsettings.json` pre Api:

```json
"Bootstrap": {
  "AdminUserName": "admin",
  "AdminPassword": ""
}
```

Pri štarte Api, po migrácii:
1. Ak v DB existuje aspoň jeden **aktívny admin**, bootstrap nerobí nič a pri vyplnenom `AdminPassword` zaloguje warning, že ho treba z appsettings zmazať.
2. Ak žiadny aktívny admin neexistuje a `AdminPassword` je vyplnené, vytvorí admina s `MustChangePassword = true`. Ak účet s tým menom už existuje, odblokuje ho, zruší zamknutie, nastaví mu rolu Admin a nové heslo. Tým bootstrap slúži aj ako **núdzová obnova** po zabudnutom hesle: admina treba zablokovať priamo v DB alebo ho nemať a reštartovať. **[builder]** Ak sa nájde jednoduchší a bezpečný spôsob obnovy, napríklad samostatný príkaz, je to v poriadku.
3. Ak žiadny admin neexistuje a heslo nie je vyplnené, zaloguje error a aplikácia beží ďalej. Nikto sa neprihlási, kým sa heslo nedoplní.

Heslo z appsettings sa nikdy neloguje. Po prvom prihlásení si ho admin musí zmeniť, takže hodnota v súbore prestane platiť.

### Frontend (React, `main.tsx`)

- Pri načítaní aplikácie sa zavolá `GET /api/auth/me`. Pri 401 sa zobrazí login, pri `mustChangePassword` formulár na zmenu hesla, inak aplikácia.
- Spoločný wrapper nad `fetch`: pri **401** z ktoréhokoľvek volania sa vráti na login (vypršaná alebo zrušená session). Pri **403** zobrazí chybu.
- Menu sa skladá podľa `role`. Settings a Admin sa zobrazia len adminovi.
- Dole v sidebare je meno používateľa, „Zmeniť heslo" a „Odhlásiť".
- Nová stránka Admin → Používatelia podľa popisu vyššie. Dočasné heslo sa ukáže v okne s tlačidlom na skopírovanie a upozornením, že sa už znova nezobrazí.
- Štýl drží existujúce triedy a vzhľad (`page`, `server-form`, `companies-table`, `status`...). **[builder]** Ak `main.tsx` (~2000 riadkov) rozumne rozdelíš na súbory pre auth a admin, je to vítané, ale nie povinné.

### HTTPS

- Web host načíta certifikát z konfigurácie Kestrelu (`Kestrel:Endpoints:Https:Certificate`), podporuje **PFX súbor s heslom** aj **Windows certificate store**. Nič natvrdo v kóde.
- `Install-Service.ps1` dnes pri každej inštalácii prepíše `dist\web` vrátane `appsettings.json`. Nastavenie certifikátu (a celé produkčné nastavenie HTTPS) preto pôjde do súboru, ktorý update neprepíše, napríklad `appsettings.Production.json`, ktorý skript iba vytvorí, ak chýba. **[builder]** To isté platí pre sekciu `Bootstrap` v Api.
- Zdroj certifikátu rieši nasadenie, nie kód. Môže to byť firemná certifikačná autorita, ak ju majú, alebo self-signed certifikát nainštalovaný kolegom ako dôveryhodný. Postup sa zapíše do `readme_deploy.md`.
- Interný DNS záznam (napr. `ledgerpulse.firma.local`) a firewall, ktorý port nepustí zvonku, nastaví admin siete. Do `readme_deploy.md` patrí krátka poznámka.
- `UseHsts` sa zapne vo Webe mimo Development. Ak bude len HTTPS listener, `UseHttpsRedirection` nie je potrebné.
- **[builder]** Upraviť `scripts/Install-Service.ps1` a predvolené appsettings tak, aby Api štandardne bežalo na `http://localhost:5184`.

### Šifrovanie SQL hesiel k serverom **[builder]**

`DataReadServer.OlympPassword` a `OmegaPassword` sú dnes v DB ako čistý text. Navrhujem ich šifrovať cez **ASP.NET Core Data Protection**. Kľúče sa uložia do priečinka na serveri a na Windows sa chránia cez DPAPI (`ProtectKeysWithDpapi`). Existujúce hodnoty sa pri štarte jednorazovo zašifrujú. Keďže zašifrovaná hodnota je dlhšia, treba skontrolovať dĺžku stĺpca v `DataReadServerConfiguration`.

Táto časť je **nezávislá** od prihlásenia. Môže byť samostatný posledný krok plánu a pri komplikáciách sa dá odložiť. Kľúče Data Protection treba zahrnúť do zálohy, inak sa heslá po reinštalácii nedajú rozšifrovať. Treba to spomenúť v `readme_deploy.md`.

---

## Testovanie

Projekt dnes nemá testovací projekt. **[builder]** Odporúčam pridať `KrosExtension.Api.Tests` (xUnit + `WebApplicationFactory`, DB napr. LocalDB alebo SQLite) aspoň na:
- neprihlásený request na chránený endpoint → 401
- bežný používateľ na admin endpoint → 403
- login, zlé heslo, zamknutie po 5 pokusoch
- reset hesla a zablokovanie → stará cookie prestane platiť
- `MustChangePassword` → všetko okrem auth endpointov vracia 403
- bootstrap: vytvorí admina iba vtedy, keď žiadny aktívny neexistuje
- nedá sa zablokovať ani degradovať posledný aktívny admin

## Čo nechávam na builderovi

- Rozdelenie do vrstiev (Domain / Application / Infrastructure / Api) podľa existujúcich vzorov.
- Presné názvy tried, tabuliek a DTO.
- Poradie krokov v pláne. Odporúčam auth v Api → bootstrap → ochranu endpointov → frontend → HTTPS/deploy → šifrovanie SQL hesiel.
- Hodnoty označené **[builder]** vyššie.
