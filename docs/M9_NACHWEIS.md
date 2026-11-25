# M9 / P22 – Spieleinstieg und lokale Partien, 1.0.17

Stand: 25. November 2026. Grundlage: D-037, T-057/058, GUI-013/014, ARCH-014/015 und TEST-033. M8 war das letzte bisher geplante Paket; der Fortsetzungsauftrag wird mit diesem abgegrenzten Bedienungsabschnitt weitergeführt.

## Bedienung

Das Spiel öffnet ein Hauptmenü mit **Neue Partie**, **Gespeicherte Partie laden**, **Starthilfe**, Einstellungen und getrennten Prüfkarten. Im Hintergrund läuft keine aktive Partie weiter. **Drei Kolonien** ist der empfohlene F05-Start. **Diplomatische Partie** und **Gegen einen Computer** verwenden die bestehenden Szenarien. Ein optionaler ganzzahliger Startwert macht den Ausgangszustand reproduzierbar; leer bedeutet zufällig.

Eine neue Partie öffnet vier kurze Hilfeseiten: Orientierung, Grundversorgung, Raumfahrt und Diplomatie/Sieg. Die Verknüpfungen öffnen die eigene Kolonie, Bauauswahl, Forschung oder Diplomatie. Die Hilfe ist jederzeit erneut im Menü erreichbar. Sie erteilt keine Bauaufträge und verändert keine Bestände.

**Speichern … / Laden …** zeigt acht feste Plätze mit Datum, Spielzeit und Startwert. F5 benutzt für eine ungespeicherte Sitzung einen freien Platz und aktualisiert danach den zugeordneten Platz. „Hier speichern“ weist einen anderen Platz zu; ein belegtes Ziel benötigt eine Bestätigung. Bei acht belegten Plätzen öffnet F5 die Auswahl. F9 fragt vor dem Laden nach. Menüs und Einstellungen halten die Spielzeit an, ohne den gespeicherten Pausenzustand umzuschalten. Escape bricht zuerst eine Bestätigung ab, führt danach zum Hauptmenü und anschließend zurück zur laufenden Partie.

Jeder Platz behält die vorherige gültige Datei als ausdrücklich ladbare **Sicherung**. Beschädigte Einträge werden mit einem Fehler angezeigt; eine intakte Sicherung bleibt separat auswählbar. Alte Einzeldateien aus früheren Versionen erscheinen zusätzlich im Katalog. Sie sind schreibgeschützt: Nach dem Laden speichert F5 in einen freien neuen Platz. Spielstände liegen weiterhin unter Unitys `Application.persistentDataPath`, die neuen Plätze im Unterordner `Saves`. Der Windows-Installer verändert diese Nutzerdaten nicht.

## Umsetzung und Grenzen

- `Platform/SaveSlots.cs` verwaltet feste Kennungen und den vorhandenen atomaren `SnapshotStore`. Metadaten stammen aus validierten Snapshots. Es gibt keine zweite Manifestdatei und keine neue Speicherformatversion; Schema 11 bleibt erhalten.
- `Runtime/PrototypeSessions.cs` trennt Titelbild, Szenarioauswahl, Speicherverwaltung, Hilfe und Bestätigungen. Das Speicherziel gehört zur konkreten Simulationsinstanz; eine Prüfkarte übernimmt es nicht. Die Rückkehr zur vorherigen Partie stellt auch deren Speicherziel wieder her.
- Die gemeinsame Simulation wird während der Menüs lediglich nicht fortgeschaltet. Ihre Pause, Befehle, Reservierungen, KI und Ressourcen bleiben Bestandteil desselben Zustands. Ein fehlerhaftes Laden ersetzt die laufende Partie nicht.
- Acht nummerierte Plätze, keine Umbenennung, automatische Speicherung, Cloud oder Löschfunktion. Die Starthilfe ist eine Lese- und Navigationshilfe, kein automatisch bewertetes Tutorial. Physische Maus-/Tastaturbedienung, subjektive Verständlichkeit und weitere Plattformen bleiben ungeprüft.

## Tatsächliche Prüfungen

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -SessionUiOnly -Width 1280 -Height 720
```

- **165 Core-/Plattformtests bestanden**, darunter vier neue Dateiszenarien: Platz-/Backup-Isolation, beschädigte Hauptdatei und explizite Wiederherstellung, schreibgeschützte Altdaten/ungültige Kennungen sowie voller Katalog einschließlich allein vorhandener Sicherung. Log: `TestResults/m9-core.log`.
- **Unity-Windows-Build und Versionsprüfung bestanden.** Log: `TestResults/m9-build.log`; Editorprotokoll `Logs/Unity-Build.log`.
- **1440×900:** vollständiger Spiel-/GUI-Lauf mit **538 Einzelbedingungen + Gesamt-PASS**, **121 PNGs**. Enthält die bisherigen 476 Bedingungen und 62 neue Sitzungs-/Speicherbedingungen.
- **1280×720:** gezielter M9-Lauf mit **64 Einzelbedingungen einschließlich zweier Setup-Prüfungen + Gesamt-PASS**, **14 PNGs**. Kein zweiter vollständiger Regressionstest.
- Geprüft: unveränderte Szenariozustände für alle drei Starts, ungültiger Startwert, Bestätigen/Abbrechen, tatsächlicher Simulationsfortschritt vor dem Überschreiben, Dateischaden, explizite Sicherung, neue Speicherung nach Altdatenimport, voller Katalog, F9, Escape, Speicherziel bei Prüfkartenwechsel sowie Menüzeit und erhaltene Spielerpause. Alle drei Layouts sind enthalten.
- Die Player laufen über `CreateDesktop`/`CreateProcess` auf einem separaten, niemals eingeblendeten Windows-Desktop. Kein Screenreader, Computer-Use, Umschalten des Arbeitsdesktops oder globale Eingabeinjektion. Die erzeugten Bilder wurden visuell kontrolliert. Ein früher Regressionstest fand die falsche Rückkehr aus Einstellungen; der finale Test enthält die Korrektur.

Finale Laufverzeichnisse: `TestResults/Presentation/20261125-191816` (vollständig 1440×900) und `TestResults/Presentation/20261125-191921` (M9 bei 1280×720). Die Abschlusslogs sind `TestResults/m9-full-1440.log` und `TestResults/m9-sessions-720.log`. Keine Exceptions, GUI- oder Shaderfehler; im vollständigen Lauf ist die Warnung des absichtlich beschädigten M8-Spielstands erwarteter Testinhalt.

[Startmenü](M9_1_0_17/startmenue.png) · [Partieauswahl bei 1280×720](M9_1_0_17/neue-partie-1280.png) · [Beschädigter Stand mit ausdrücklich ladbarer Sicherung](M9_1_0_17/sicherung-1280.png)

## Git und Windows-Paket

Lokale Prüfungen sind bestanden. Versionsbranch `1.0.17` basiert auf dem integrierten `main`-Commit `4a8dd38`. Implementierungs- und Paketmanifest-Commit: `66a665e8d69ffbbbb382bc0dcb708c07651e0638`; der Folgecommit ergänzt ausschließlich diesen Paketnachweis. Git-Integration und Remote-Pflichtchecks sind unabhängig von den lokalen Ergebnissen zu bewerten; ihr aktueller Stand steht im zugehörigen PR. Es wurde kein GitHub-Release veröffentlicht.

```powershell
powershell -ExecutionPolicy Bypass -File tools/Package-Windows.ps1 -CompilerPath TestResults/Toolchains/InnoSetup-6.4.3/ISCC.exe
powershell -ExecutionPolicy Bypass -File tools/Test-Installer.ps1 -RunPlayer
```

Beide Befehle bestanden. Paketvergleich, Installation, tatsächlicher Start des installierten Players mit vollständigen 538 Einzelbedingungen und Deinstallation erfolgreich. Installationsfixture: `TestResults/Installer/470a8b8b658b41139ddf977f7fb15aab`; isolierter Playerlauf: `TestResults/Presentation/20261125-192122`. Keine Shortcuts, Änderung einer vorhandenen Installation oder Anzeige auf dem Arbeitsdesktop. Logs: `TestResults/m9-package.log` und `TestResults/m9-installer.log`.

| Datei unter `Builds/Distribution` | Bytes | SHA-256 |
|---|---:|---|
| `PlanetOne-1.0.17-Windows-x64-Setup.exe` | 28033984 | `ed4c829217f3e64bb4c8d16bb4bb040ef401fc331c8d88d244586a3c2a0d2e34` |
| `PlanetOne-1.0.17-Windows-x64.zip` | 37584024 | `ce98cf5a74b08e70748e557e119316a44d01e74fa50dbd2833255704878365f7` |
