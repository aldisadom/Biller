# Biller

[English](../README.md)

Biller yra sąskaitų išrašymo sistemos serverio dalis: REST API, kuri saugo naudotojus, pardavėjus,
pirkėjus, prekes, sąskaitų serijas ir sąskaitas, ir kuria sąskaitų faktūrų bei atliktų darbų aktų
PDF dokumentus lietuvių ir anglų kalbomis. Sistema turi vieną savininką ir veikia per Docker Compose
mažame Linux kompiuteryje.

Šis failas yra [README.md](../README.md) vertimas. Komandos, keliai ir pavadinimai palikti tokie,
kokie yra angliškame faile.

## Keturios repozitorijos

| Repozitorija | Kas tai |
|---|---|
| Biller (ši) | API, duomenų bazės migracijos ir PDF dokumentai |
| [BillerContracts](https://github.com/aldisadom/BillerContracts) | Užklausų ir atsakymų įrašai, išvardijimai (enum) ir tipizuoti HTTP klientai, skelbiami kaip NuGet paketai `BillerContracts` ir `BillerContracts.Clients` |
| [BillerFrontEnd](https://github.com/aldisadom/BillerFrontEnd) | Blazor naudotojo sąsaja. Šią API ji kviečia per `BillerContracts.Clients` |
| [BillioIntegrationTest](https://github.com/aldisadom/BillioIntegrationTest) | Testai, kurie kviečia veikiančią API per HTTP |

Užklausos ar atsakymo pakeitimas pirmiausia daromas BillerContracts. Kitas repozitorijas jis pasiekia
tada, kai paskelbiama nauja paketo versija ir kiekvienoje repozitorijoje atnaujinama paketo nuoroda.

## Ko reikia

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/get-started) su Docker Compose
- Padengimo ataskaitai: `dotnet tool install -g dotnet-reportgenerator-globaltool`

## Paleidimas

```sh
docker compose up -d --build    # or: make run
```

| Konteineris | Kas | Kur |
|---|---|---|
| `billio-webapi` | API | http://localhost:8090, Swagger: http://localhost:8090/swagger |
| `billio-db` | PostgreSQL 16, duomenų bazė `Data` | localhost:5437 |
| `billio-liquibase` | Pritaiko migracijas ir sustoja | |
| `billio-pgadmin4` | pgAdmin | http://localhost:8888 |

`make update` skirta kompiuteriui, kuriame sistema įdiegta ir kuriame aktyvi `master` šaka: ji
padaro atsarginę kopiją, tada atstato aktyvią šaką į `origin/master` (sekamų failų pakeitimai ir
vietiniai įrašai (commit) prarandami, kaip ir failas, kurio git neseka, jei jo kelią seka
`origin/master` arba vietinė `master`), persijungia į `master` ir iš naujo paleidžia konteinerius.
Kiti failai, kurių git neseka, lieka. Toks failas projekte aplanke `src/` įtraukiamas į API (pvz.,
`.cs` failas sukompiliuojamas), nebent `.dockerignore` jį praleidžia (`bin/`, `obj/` ir kt.).
Pakeitimų rinkinio (changeset) failas, kurio git neseka, aplanke `migrations/sql/` (`.sql`,
`.yaml`, `.yml`, `.xml` ar `.json`) paleidžiamas duomenų bazėje, todėl jo ten nepalikite.
Žinoma problema: ji atstato prieš atsisiųsdama (fetch), todėl įdiegia tą `master` šaką, kuri buvo
atsisiųsta praėjusį kartą, ir tik antras paleidimas gauna naujausią. Kitoje šakoje pirmas
paleidimas atstato tą šaką ir įdiegia vietinę `master` tokią, kokia ji yra.

## Testavimas

```sh
bash .claude/skills/_test-coverage/scripts/run-coverage.sh    # unit tests and the coverage report
bash .claude/skills/_test-coverage/scripts/build.sh           # build only
```

Abi komandos kompiliuoja Release konfigūraciją, kaip ir CI. Padengimo ataskaita įrašoma į
`coveragereport/`.

Integraciniai testai yra BillioIntegrationTest repozitorijoje ir per API ištrina visus duomenis,
todėl jie leidžiami su laikinu rinkiniu, niekada su aukščiau išvardytais konteineriais.
`run-integration.sh` testus ima iš BillioIntegrationTest darbinės kopijos šalia šios
(`../BillioIntegrationTest`) arba iš aplanko, nurodyto su `--repo`:

```sh
bash .claude/skills/_test-stack/scripts/test-stack.sh up
bash .claude/skills/_test-stack/scripts/run-integration.sh
bash .claude/skills/_test-stack/scripts/test-stack.sh down
```

CI (`.github/workflows/CI.yaml`) sukompiliuoja projektą, tada lygiagrečiai paleidžia du darbus:
modulinius testus, kurie nutrūksta, jei padengimas nukrenta žemiau nustatytų minimumų, ir
integracinius testus iš BillioIntegrationTest šakos tokiu pačiu pavadinimu kaip ši šaka, o jei
tokios nėra, iš jos master.

## Konfigūracija

Docker Compose paleidžia API su `ASPNETCORE_ENVIRONMENT=LocalDocker`, todėl nustatymai imami iš
`src/WebAPI/appsettings.json` ir `src/WebAPI/appsettings.LocalDocker.json`:

- `ConnectionStrings:PostgreConnection`: duomenų bazė.
- `FontSettings`: PDF dokumentų šriftų failai, kiekvienas tik su `Path`. Compose prijungia `./Fonts`
  kaip `/Fonts`. API nepasileidžia, jei failo nėra arba jis nėra nuskaitomas šriftas, jei nė viename
  nėra dokumentų šrifto šeimos (Calibri) arba jei ne visuose yra A-Z, a-z ir 0-9. Failai taip pat
  lemia, kokį tekstą galima išsaugoti: simbolis, kurio nėra kiekviename nurodytame faile (jaustukas,
  kinų rašmuo), gauna 400 „Validation failure“, o pranešime nurodomas tas simbolis. Tai taikoma
  visiems tekstiniams laukams, su kuriais išsaugomas naudotojas, pirkėjas, pardavėjas, prekė ar
  serija (išskyrus slaptažodį), ir sąskaitų komentarams.
- `PasswordEncryption:Pepper`: įmaišoma į slaptažodžių maišos reikšmes. Ją pakeitus visi išsaugoti
  slaptažodžiai nebetinka.
- `Serilog`: žurnalai.

Prievadai, duomenų bazės pavadinimas ir prisijungimo duomenys yra `docker-compose.yml`.

Sąskaitos, kurių apmokėjimo terminas praėjo, tampa pradelstomis paleidžiant API ir kasdien 03:00 UTC.

## Kur kas yra

- `src/WebAPI`: kontroleriai, Swagger pavyzdžiai, tarpinė programinė įranga (middleware), foninės užduotys.
- `src/Validators`: užklausų tikrintuvai.
- `src/Application`: servisai, modeliai, atvaizdavimo metodai, PDF dokumentai, sumos žodžiais.
- `src/Domain`: esybės, repozitorijų sąsajos, išimtys.
- `src/Infrastructure`: Dapper repozitorijos, duomenų bazės ir PDF generatoriaus registravimas.
- `tests/xUnitTests`: moduliniai testai tuose pačiuose aplankuose kaip `src`.
- `migrations/sql`: Liquibase pakeitimų rinkiniai (changeset).
- `docs/plans`: po vieną planą kiekvienai Jira užduočiai.

Susitarimai, paketų apribojimai ir peržiūros taisyklės yra [CLAUDE.md](../CLAUDE.md) ir
[REVIEW.md](../REVIEW.md) failuose.

## Migracijos

Schemą valdo Liquibase. Kiekvienas `.sql` failas aplanke `migrations/sql/` yra pakeitimų rinkinys:
`migrations/dbChangeLog.yaml` įtraukia visą aplanką, o `billio-liquibase` konteineris kaskart
paleidžiant rinkinį pritaiko failus pagal failų pavadinimų tvarką (skaičių pavadinimo pradžioje).
Pakeitimų rinkinys, kuris jau yra master šakoje, niekada neredaguojamas; klaida taisoma nauju
rinkiniu. Naujas rinkinys turi `-- rollback` eilutę. Keturi seni rinkiniai (023, 024, 026 ir 027)
jos neturi, todėl atšaukti pakeitimų toliau jų negalima.

## Atsarginė kopija ir atkūrimas

`make update` pirmiausia padaro atsarginę kopiją ir sustoja, jei tai nepavyksta. Prieš bet kokį kitą
pakeitimą, kuris liečia duomenis, pavyzdžiui, prieš diegdami migraciją rankomis, patys paleiskite
`make backup`.

```sh
make backup     # saves backup/billio-YYYYMMDD-HHMMSS.dump (UTC), keeps the newest 14
make restore    # lists the backups in backup/, asks which one, then restores it into Data
```

Atkuriama pirmiausia į laikiną duomenų bazę, todėl sugadinta kopija palieka `Data` tokią, kokia
buvo. Tik kai tai pavyksta, sustabdomas `webapi`, dabartinė `Data` išsaugoma į
`backup/billio-YYYYMMDD-HHMMSS-before-restore.dump` (kad ne to failo pasirinkimą būtų galima
atšaukti), atkurta duomenų bazė įstatoma į jos vietą, paleidžiamas Liquibase ir vėl paleidžiamas
`webapi`. Jei Liquibase nepavyksta, `webapi` lieka sustabdytas. Atsarginėje kopijoje yra ir
Liquibase lentelė `databasechangelog`, todėl atkūrus senesnę kopiją pritaikomos tik naujesnės
migracijos.

**Patikrinti atsarginę kopiją** neliečiant veikiančios duomenų bazės: atkurkite ją į bandomąją
duomenų bazę, palyginkite eilučių skaičius su veikiančios bazės ir bandomąją ištrinkite:

```sh
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_test\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_test --no-owner --exit-on-error < backup/billio-....dump
docker exec billio-db psql -U postgres -d Data_restore_test -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d Data -c "SELECT count(*) FROM invoices;"
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_test\";"
```

**Be scenarijų:**

```sh
# backup
docker exec billio-db pg_dump -U postgres -d Data -Fc > backup/billio.dump

# restore: into a temp database first; go on only if pg_restore succeeds
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE IF EXISTS \"Data_restore_tmp\" WITH (FORCE);"
docker exec billio-db psql -U postgres -d postgres -c "CREATE DATABASE \"Data_restore_tmp\" TEMPLATE template0;"
docker exec -i billio-db pg_restore -U postgres -d Data_restore_tmp --no-owner --exit-on-error < backup/billio.dump
docker stop billio-webapi
# back up the current Data; go on only if both commands succeed
undo=backup/billio-$(date -u +%Y%m%d-%H%M%S)-before-restore.dump
docker exec billio-db pg_dump -U postgres -d Data -Fc > "$undo"
docker exec -i billio-db pg_restore --list < "$undo" | grep -m1 "TABLE DATA"
# swap, migrate, and drop the old database only once Liquibase succeeded
docker exec billio-db psql -U postgres -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = 'Data'; ALTER DATABASE \"Data\" RENAME TO \"Data_restore_old\"; ALTER DATABASE \"Data_restore_tmp\" RENAME TO \"Data\";"
docker start -a billio-liquibase
docker exec billio-db psql -U postgres -d postgres -c "DROP DATABASE \"Data_restore_old\" WITH (FORCE);"
docker start billio-webapi
```

Atsarginių kopijų kopiją laikykite ne tame kompiuteryje (USB laikmenoje ar debesies aplanke).
Kopija tame pačiame diske neišgyvena disko gedimo.

## Licencija

MIT, žr. [LICENSE.txt](../LICENSE.txt).
