# GitHub-Builds und manuelle Downloads

Auftrag vom 24. November 2026, Produktversion 1.0.4. Diese Werkzeugarbeit erweitert P01 (reproduzierbarer Windows-Build); M5 und weitere Plattformabnahmen bleiben unverändert offen.

## PR-Prüfung

`PR and main build` startet bei jedem PR gegen `main`, bei neuen PR-Commits und nach einem Push/Merge nach `main`. Keine Pfadfilter. Der Pflichtcheck **PR validation** ist nur erfolgreich, wenn alle drei Stufen erfolgreich sind:

1. Windows: Produktversion prüfen, zwölf Versionswerkzeugtests und 58 Core-Verhaltenstests ausführen.
2. Linux/GameCI: mit der Editorversion aus `ProjectSettings/ProjectVersion.txt` einen Windows-x64-Mono-Player bauen. Der projektspezifische Build prüft VERSION; GameCI vergibt keine eigene Produktversion. Der Player wird ohne Development-Option gebaut.
3. Windows: Inno Setup 6.4.3 erstellt den Installer; zusätzlich entstehen portable ZIP, `build-info.json` mit Commit/Version/Editor und SHA-256-Prüfsummen. Ein isolierter Installationstest vergleicht sämtliche installierten Dateien mit der ZIP und prüft die Deinstallation, ohne eine vorhandene Benutzerinstallation zu registrieren oder zu verändern.

Ein fehlgeschlagener, abgebrochener oder übersprungener Teiljob lässt den Pflichtcheck fehlschlagen. Nur Leserechte für den Workflow; Actions sind an Commit-SHAs gebunden. Kein `pull_request_target` und kein Zugriff fremder PRs auf einen lokalen Rechner. Fork-PRs ohne Unity-Secrets können den Build nicht abschließen; geprüfte Beiträge müssen auf einen Branch des eigenen Repositorys übernommen werden.

GitHub-Branchschutz für `main`: PR erforderlich, **PR validation** erforderlich, Branch muss aktuell sein, Administratoren eingeschlossen, Force-Push und Löschen verboten. Die tatsächliche Aktivierung wird im Nachweis unten festgehalten.

## Einmalige Unity-Voraussetzung

Unter **Settings → Secrets and variables → Actions** benötigt das Repository `UNITY_EMAIL`, `UNITY_PASSWORD` und entweder `UNITY_LICENSE` (Personal-Lizenzdatei) oder `UNITY_SERIAL` (Professional). Keine Werte in Dateien, Logs oder Chat schreiben. Die Aktivierung muss zum verwendeten Unity-Konto passen. Eine lokale Hub-Aktivierung allein richtet keine GitHub-Aktivierung ein. Lizenz/Anmeldedaten werden nicht automatisch vom Entwicklerrechner kopiert.

Anleitung: [GameCI-Aktivierung](https://game.ci/docs/github/activation/). Die Nutzbarkeit des konkret bereitgestellten Lizenztyps muss der erste Remote-Build nachweisen; die Pipeline umgeht keine Lizenzprüfung. Fehlende Secrets erzeugen eine ausdrückliche Fehlermeldung.

## Download veröffentlichen

1. PR mergen und den erfolgreichen **PR and main build** für den neuen `main`-Commit abwarten.
2. **Actions → Release Windows download → Run workflow**, Branch **main** wählen. Für den Prototyp ist **Als Vorabversion kennzeichnen** voreingestellt.
3. Der Workflow verwendet ausschließlich das Distributionsartefakt eines erfolgreichen Push-Builds dieses exakten `main`-Commits. Er prüft Manifest und SHA-256-Werte. Kein erneuter Build mit anderem Inhalt.
4. GitHub Release `v<VERSION>` enthält Setup-EXE, portable ZIP, Manifest und Prüfsummen. Erst nach vollständigem Upload wird der zunächst private Release-Entwurf veröffentlicht. Ein inzwischen geänderter `main`, abgelaufene Artefakte oder schon vorhandene Versionen führen zum Abbruch.

Der Workflow startet ausschließlich manuell; ein Merge erzeugt keinen Release. Ein fehlgeschlagener Upload kann einen Entwurf hinterlassen; diesen im Releases-Bereich prüfen und vor erneutem Start gezielt entfernen. Bestehende veröffentlichte Versionen werden nicht überschrieben. Artefakte werden 30 Tage aufbewahrt; bei Ablauf den CI-Lauf für den aktuellen Commit erneut ausführen. Bereits veröffentlichte Release-Dateien bleiben davon unabhängig verfügbar.

## Installer und portable Ausgabe

Unity benötigt hier keinen Installer. Die portable ZIP funktioniert nach vollständigem Entpacken mit `PlanetOne.exe`. Der Installer erleichtert Startmenüeintrag, Installation und Deinstallation; er installiert ohne Administratorrechte unter `%LOCALAPPDATA%\Programs\PlanetOne`. Spielstände im Unity-Benutzerverzeichnis bleiben bei Deinstallation erhalten. Die stabile AppId ermöglicht Updates derselben Installation. Installer und Player sind derzeit nicht signiert.

Lokale Paketierung nach erfolgreichem Windows-Build und installierter Inno-Setup-Version:

```powershell
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Package-Windows.ps1
```

Optional `-CompilerPath <ISCC.exe>`. Ausgabe unter `Builds/Distribution`; vorhandene Pakete werden nicht überschrieben. Vor erneutem Paketieren die bisherigen Dateien einschließlich des Staging-Unterordners gezielt verschieben. Debug-/Backupordner und PDB-Dateien werden nicht ausgeliefert.

## Nachweis und Grenzen

Lokal am 24. November 2026 bestanden: 58 Core-Tests; zwölf Versionswerkzeugtests mit 19 Fixtures; Versionsprüfung 1.0.4; Unity-Windows-Release-Build; grafischer Integrationstest mit 91 Einzelprüfungen und elf erzeugten Renderbildern unter `TestResults/Smoke/20261124-072946`; Paketierung mit Inno Setup 6.4.3; Dateivergleich Installer/ZIP und erfolgreiche Installation/Deinstallation. `actionlint` 1.7.12 akzeptiert beide Workflows. Die neuen Renderbilder wurden nicht zusätzlich manuell abgenommen.

Der Branchschutz wurde über die GitHub-API eingerichtet und aus der Antwort verifiziert: PR erforderlich, aktueller Branch und `PR validation` der GitHub-Actions-App erforderlich, Administratoren eingeschlossen, kein Force-Push/Löschen. Bei Einrichtung waren keine Unity-Secrets vorhanden. Der erfolgreiche Remote-Unity-Build und damit der Merge bleiben bis zur Einrichtung der Secrets offen. Die manuelle Veröffentlichung wurde nicht ausgelöst; ein tatsächlicher Release-Upload ist noch ungeprüft.

Erster Remote-Nachweis: [CI-Lauf 45341142534](https://github.com/bratkartoffeln/PlanetOne/actions/runs/45341142534) zu [PR #4](https://github.com/bratkartoffeln/PlanetOne/pull/4). Core-, Versions- und Release-Prüfungen sind auf GitHub erfolgreich. Der Unity-Job scheitert ausdrücklich an fehlenden Aktivierungs-Secrets; Paketierung wird deshalb übersprungen und der Merge bleibt blockiert. Die Release-Prüfungen führen den tatsächlichen Workflow-Code mit simulierten API-/Dateiantworten aus: Erfolgsfall sowie sieben Fehlerfälle, ohne Veröffentlichung.

Der grafische Integrationstest bleibt lokal (`tools/Test-Prototype.ps1 -Visible`); die CI behauptet keine visuelle Bedienungsabnahme. Andere Exportplattformen, Codesignatur und Store-Veröffentlichungen gehören nicht zu diesem Auftrag.

Referenzen: [GameCI Builder](https://game.ci/docs/github/builder/), [GitHub Branchschutz](https://docs.github.com/en/repositories/configuring-branches-and-merges-in-your-repository/managing-protected-branches/about-protected-branches), [Inno Setup Benutzerinstallation](https://jrsoftware.org/ishelp/topic_setup_privilegesrequired.htm).
