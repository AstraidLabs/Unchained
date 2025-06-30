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

# spuštění aplikace
 dotnet run --project Unchained.Api/Unchained.csproj
```
Aplikace standardně poslouchá na portu `5000` (HTTP) a `5001` (HTTPS).

## Konfigurace
Nastavení se provádí pomocí souborů `appsettings.json` (případně variant `Development`, `Production` atd.). Klíčové sekce:
- `Unchained` – URL API, identifikace zařízení a další parametry
- `Session` – délka platnosti session, maximální počet přihlášení
- `Cache` – expirace jednotlivých položek v paměťové cache
- `RateLimit` – omezení počtu požadavků

## Správa zařízení
- `GET /magenta/devices` – vrátí seznam zařízení spojených s účtem.
- `DELETE /magenta/devices/{id}` – odebere uvedené zařízení.

V produkčním prostředí je nutné nastavit proměnnou `SESSION_ENCRYPTION_KEY` pro šifrování session tokenů. Pokud není nastavena, aplikace při startu vygeneruje klíč automaticky.

## Konzolový klient
Projekt obsahuje i ukázkovou aplikaci `Unchained.ConsoleClient`, která
umožňuje ovládání API z příkazové řádky. Program nabízí jednoduché menu
pro přihlášení, výpis kanálů, zobrazení EPG i spuštění nahrávání. Spustíte
ho příkazem:

```bash
dotnet run --project Unchained.ConsoleClient/Unchained.ConsoleClient.csproj
```

## Poznámky
Projekt momentálně cílí na .NET 9.0, který může vyžadovat preview verzi SDK. Pokud SDK není dostupné, kompilace se nemusí podařit.

