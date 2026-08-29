# ScanTrack — Distribuerat paketnätverk

> Vecka 36 · Docker · ITHS Administrera molnlösningar

---

## Scenario

ScanTrack AB hanterar pakettransporter mellan svenska städer. Varje stad har ett eget system — en **nod** — som tar emot paket och vidarebefordrar dem mot destinationen längs kortaste väg.

Ni är teamet som bygger **er stads nod**. En annan grupp bygger en annan stad. I slutet av dagen ska paketen röra sig genom Sverige i realtid.

---

## Arkitektur

Varje nod är en containeriserad .NET Web API. Den:

1. **Registrerar sig** mot ScanTrack-registret vid uppstart
2. **Tar emot paket** via `POST /paket`
3. **Räknar ut nästa hopp** med Dijkstras algoritm (baserat på `data/cities.csv`)
4. **Vidarebefordrar** paketet till nästa stad
5. **Levererar** om paketet nått sin destination

Paketet bär med sig en historik — listan med alla städer det passerat. Det förhindrar loopar.

```
Göteborg ──→ POST /paket {destination: "Sundsvall", history: ["Göteborg"]}
    ↓
   Dijkstra: kortaste väg är Göteborg→Stockholm→Gävle→Sundsvall
    ↓
Vidarebefordrar till Stockholm...

Stockholm ──→ history: ["Göteborg", "Stockholm"]
    ↓
Vidarebefordrar till Gävle...

Gävle ──→ history: ["Göteborg", "Stockholm", "Gävle"]
    ↓
Vidarebefordrar till Sundsvall...

Sundsvall ──→ destination == jag → levererat! ✓
```

---

## Miljövariabler

Tre variabler måste sättas när noden startas:

| Variabel | Exempel | Beskrivning |
|----------|---------|-------------|
| `CITY_NAME` | `Göteborg` | Er stads namn (måste matcha cities.csv) |
| `NODE_URL` | `https://scantrack-goteborg.azurecontainer.io` | Er nods publika URL |
| `REGISTRY_URL` | *(delas ut av läraren)* | Centralt register för alla noder |

---

## Endpoints

| Endpoint | Metod | Beskrivning |
|----------|-------|-------------|
| `/paket` | POST | Ta emot och vidarebefordra ett paket |
| `/paket` | GET | Lista paket som levererats till denna nod |
| `/status` | GET | Nodens hälsostatus |
| `/route?from=X&to=Y` | GET | Visa beräknad rutt (för testning) |
| `/swagger` | GET | API-dokumentation |

---

## Din uppgift

### Steg 1 — Förstå koden
Läs igenom projektet. Testa att köra det lokalt:

```bash
cd ScanTrackNode
dotnet run
```

Öppna `http://localhost:5000/swagger` och testa `/route?from=Göteborg&to=Umeå`.

### Steg 2 — Implementera `ForwardAsync`

Öppna `Services/PackageForwarder.cs`. Implementera metoden `ForwardAsync` enligt instruktionerna i kommentarerna.

Testa lokalt innan du går vidare.

### Steg 3 — Skriv Dockerfile

Öppna `Dockerfile`. Fyll i de saknade raderna (multi-stage build — se föreläsningen).

Bygg och testa lokalt:
```bash
docker build -t scantrack-node .
docker run -p 8080:8080 \
  -e CITY_NAME=Göteborg \
  -e NODE_URL=http://localhost:8080 \
  -e REGISTRY_URL=<url från läraren> \
  scantrack-node
```

### Steg 4 — Sätt upp GitHub Actions-pipeline

Deployment sker via CI/CD — **inga manuella `docker push` eller `az container create`**.
Miljövariablerna får aldrig ligga i koden. De lever i GitHub Secrets och injiceras av pipeline.

**Secrets att sätta under Settings → Secrets and variables → Actions:**

| Secret | Vad det är |
|--------|-----------|
| `ACR_LOGIN_SERVER` | `<ditt-register>.azurecr.io` |
| `ACR_USERNAME` | Användarnamn till ACR |
| `ACR_PASSWORD` | Lösenord till ACR |
| `AZURE_CREDENTIALS` | JSON från `az ad sp create-for-rbac` |
| `CITY_NAME` | Er stads namn (t.ex. `Göteborg`) |
| `NODE_URL` | Er nods publika URL när den är uppe |
| `REGISTRY_URL` | URL till ScanTrack-registret (delas ut av läraren) |

Öppna `.github/workflows/deploy.yml`. Fyll i alla `# ???`-ställen.
När du pushar till `main` ska pipeline köra och noden hamna i ACI automatiskt.

Verifiera att det fungerade:
```bash
az container show --name scantrack-<din-stad> --resource-group <din-rg> --query instanceView.state
```

### Steg 5 — Skicka ett paket

När alla grupper är uppe — skicka ett paket och se det röra sig genom Sverige:

```bash
curl -X POST https://<din-url>/paket \
  -H "Content-Type: application/json" \
  -d '{
    "destination": "Umeå",
    "payload": "3 kartonger kaffe",
    "history": []
  }'
```

Titta på loggarna:
```bash
az container logs --name scantrack-<din-stad> --resource-group <din-rg> --follow
```

---

## Städer

| Stad | Grupp |
|------|-------|
| Göteborg | Grupp 1 |
| Malmö | Grupp 2 |
| Jönköping | Grupp 3 |
| Linköping | Grupp 4 |
| Norrköping | Grupp 5 |
| Stockholm | Grupp 6 |
| Örebro | Grupp 7 |
| Gävle | Grupp 8 |
| Sundsvall | Grupp 9 |
| Umeå | Grupp 10 |

---

## Inlämning

- Länk till er live-nod (`/status` ska svara)
- Ifylld `rapport_mall.md`
- Skärmbild: ett paket som passerat er nod (syns i loggarna)

Deadline: *(sätts av läraren)*
