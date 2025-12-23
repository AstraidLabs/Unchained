# Unchained API

**Unchained API** je otevřená brána k televiznímu obsahu Magenta. Díky této ASP.NET Core aplikaci můžete pohodlně obsloužit oficiální API, bezpečně spravovat relace a vytvářet vlastní playlisty či integrační služby. Projekt míří na vývojáře a nadšence, kteří chtějí využívat Unchained ve svých aplikacích bez složité konfigurace.

Rychlé starty, přehledné rozhraní a možnost libovolného rozšíření – to je Unchained API v kostce. Ať už stavíte domácí IPTV řešení, nebo chcete pouze automatizovat přístup k živému vysílání, tato aplikace vám poskytne všechny potřebné nástroje.

## Požadavky
- .NET SDK 9.0 (preview)
- Volitelně `curl` pro testování endpointů

## Struktura projektu
- `Unchained.Api/` – zdrojové kódy aplikace
- `Unchained.ConsoleClient/` – interaktivní konzolový klient
- `Unchained.sln` – solution soubor
- `appsettings.Production.json` – ukázková konfigurace

## Co získáte
- **Okamžitou správu relací a přihlášení** – API obstará autentizaci a správu uživatelských relací za vás.
- **Přístup ke kanálům, EPG a streamům** – vše přehledně na jednom místě díky koncovým bodům `MagentaController`.
- **Generování M3U a XMLTV** – připravte svým přehrávačům dokonalý playlist jedním požadavkem.
- **SignalR notifikace** – sledujte v reálném čase přihlášení uživatelů, stav FFmpeg úloh i chod background služeb přes `/hubs/notifications`.
- **NotificationClient** – knihovna pro snadné připojení k SignalR hubu a příjem událostí jako přihlášení, odhlášení, dokončení nahrávání či start a konec background úloh.
- **Realtime přehled spojení** – nový ConnectionRegistry hlídá připojení klientů a zasílá události o jejich připojení či odpojení.
- **Health checks a barevná konzole** – kontrolujte stav služeb a užijte si přehledné výpisy díky [Spectre.Console](https://spectreconsole.net).
- **Jednotný formát chyb** – vlastní middleware vrací `ApiResponse` s identifikátorem chyby, takže se v logu neztratíte.

## Spuštění v režimu vývoje
```bash
# obnova závislostí
dotnet restore Unchained.sln

# spuštění gateway serveru
dotnet run --project Unchained.Api/Unchained.csproj

# spuštění TUI klienta (nové okno/terminál)
dotnet run --project src/Unchained.Tui/Unchained.Tui.csproj
```
Gateway poslouchá na `http://localhost:5000`. TUI se automaticky připojí na základní URL z `src/Unchained.Tui/appsettings.json`.

## Konfigurace
Nastavení se provádí pomocí souborů `appsettings.json` (případně variant `Development`, `Production` atd.). Klíčové sekce:
- `Urls` – základní adresa Kestrel serveru (výchozí `http://localhost:5000`)
- `Gateway` – nastavení veřejných endpointů (`Auth.Mode` = `None`/`ApiKey`, `Auth.ApiKeyHeader`, `Auth.ApiKey`, `PlaylistCacheSeconds`, `XmlTvCacheSeconds`)
- `Unchained` – URL upstream API, identifikace zařízení a další parametry
- `Session` – délka platnosti session, maximální počet přihlášení
- `Cache` – expirace jednotlivých položek v paměťové cache
- `RateLimit` – omezení počtu požadavků

Klient `src/Unchained.Tui/appsettings.json` obsahuje zrcadlové hodnoty:
- `BaseUrl` – základní URL gateway (bez koncového `/`, přidá se automaticky)
- `Profile` – profil playlistu (`generic`, `kodi`, `tvheadend`, `jellyfin`)
- `Auth` – stejné nastavení ApiKey jako na serveru
- `SignalR` – volitelné připojení k hubu (pokud je povoleno)

## TUI klávesové zkratky
- `F5` – načíst /channels
- `F2` – uložit `playlist.m3u` dle profilu v konfiguraci
- `F3` – uložit `epg.xml`
- `Actions` menu – volání health checků, statusu a admin endpointů (404 na admin cestách se vypíše jen do logu)

## Správa zařízení a upstream
Původní endpoints `/magenta/*` jsou zachovány pro práci s upstreamem (např. `/magenta/devices`, `/magenta/stream/{id}`), ale TUI používá nové kořenové cesty (`/channels`, `/m3u`, `/xmltv`, `/status`, `/health/live`, `/health/ready`, `/admin/*`).

V produkčním prostředí je nutné nastavit proměnnou `SESSION_ENCRYPTION_KEY` pro šifrování session tokenů. Pokud není nastavena, aplikace při startu vygeneruje klíč automaticky.

## Poznámky
Projekt cílí na .NET 9.0, který může vyžadovat preview verzi SDK. Pokud SDK není dostupné, kompilace se nemusí podařit.
