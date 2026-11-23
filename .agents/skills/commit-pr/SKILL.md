---
name: commit-pr
description: Versioniert abgeschlossene PlanetOne-Arbeit, erstellt einen Versionsbranch, committet die geprüften Änderungen, pusht den Branch und merged einen GitHub-PR nach main. Verwenden beim beauftragten Goal-Abschluss oder bei einem ausdrücklichen Commit-/PR-Auftrag.
---

# PlanetOne Commit und PR

Dieser projektspezifische Skill führt beauftragte Arbeit bis zum integrierten Stand auf `main`. Der Nutzer hat den Ablauf einschließlich Push und PR-Merge für den Goal-Abschluss angefordert. Das Repository verwendet ausdrücklich **main**, nicht master. Eine reine Planungs-, Prüf- oder Skillbearbeitung löst allein keinen Release aus. Keine GitHub-Releases, Deployments oder Änderungen an Branchschutz/Repositorysichtbarkeit daraus ableiten.

## Projekt und Version

- Im Repository-Stamm arbeiten; [AGENTS.md](../../../AGENTS.md) und den aktuellen [Umsetzungsplan](../../../docs/IMPLEMENTATION_PLAN.md) beachten.
- `origin` bezeichnet `https://github.com/bratkartoffeln/PlanetOne.git`. Bei abweichender Repositoryidentität erst den tatsächlichen Auftrag zuordnen; nicht in ein anderes Repository veröffentlichen.
- [VERSION](../../../VERSION) ist die einzige Quelle der Produktversion im Format `MAJOR.MINOR.PATCH`. Der Branch heißt genau wie die Zielversion, z. B. `1.0.1`, ohne Präfix. Standardmäßig Patch um eins erhöhen; Minor oder Major nur bei ausdrücklich gewähltem Versionssprung.
- [tools/Version.ps1](../../../tools/Version.ps1) synchronisiert die Unity-Produktversionen und führt vorhandene Plattform-Buildzähler getrennt fort. Unity-Editor-, Paket-, Dateiformat- und Savegame-Schemaversionen sind **keine** Produktversionen. Keine globale Text-Ersetzung durchführen. Historische Versionsangaben in Nachweisen bleiben historisch.

## Branch zu Beginn des beauftragten Arbeitsabschnitts

1. `git status --short --branch`, aktuellen Branch, Index und `origin` prüfen, dann `git fetch origin`. Benutzeränderungen, insbesondere lokale IDE-Einstellungen, erhalten; keine pauschalen Resets, Stashes oder Clean-Aufrufe.
2. Auf `main` zunächst dessen Verhältnis zu `origin/main` feststellen. Einen sauberen, nur zurückliegenden Branch per `git pull --ff-only origin main` aktualisieren. Bei lokalen Änderungen oder auseinanderlaufenden Historien die Änderungen zuerst auf einem Arbeitsbranch sichern und regulär integrieren; nie unbesehen pullen oder überschreiben.
3. Die nächste Version ermitteln:

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Next
   ```

4. Vor der Erstellung prüfen, ob der Zielname lokal, remote oder als Release-Tag bereits verwendet wird. Einen fremden oder abgeschlossenen Branch nicht wiederverwenden. Bei einer Kollision den vorhandenen Stand untersuchen und die nächste freie, höhere Version wählen. Anschließend `git switch -c <Zielversion>` und die Version setzen:

   ```powershell
   powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Set -Version <Zielversion>
   ```

5. Auf einem bereits bestehenden Versionsbranch denselben Arbeitsabschnitt fortsetzen. Falls VERSION noch auf der Basisversion steht, genau einmal auf den Branchnamen setzen. Bei Widersprüchen zwischen Branch, VERSION und Remote-Historie die Ursache klären; nicht bei jedem Wiederanlauf erneut hochzählen. Ein vorhandener anderer Arbeitsbranch wird erst nach Prüfung seines Inhalts und seiner PR-Zuordnung passend eingeordnet.

## Prüfen und committen

1. Den beauftragten Funktionsumfang und dessen Nachweise abschließen. Bei Spielcode gehören die relevanten Core-Tests, ein erfolgreicher Unity-Windows-Build und der sichtbare Integrationstest dazu. Ein versionsgeänderter Player muss neu gebaut werden. Bei reinen Werkzeugänderungen deren Verhalten gezielt prüfen. Nicht ausgeführte Prüfungen ausdrücklich benennen.
2. `tools/Version.ps1 -Action Check` ausführen. Branchnamen und VERSION müssen übereinstimmen. Angezeigte Spielversion aus `Application.version` beziehen; PR-Titel und Release-Commit verwenden dieselbe Zielversion.
3. Diff und tatsächliche Dateiliste prüfen. Nur die zur Lieferung gehörenden Dateien explizit stagen. `Library`, `Builds`, Logs, Installer, Testergebnisse, lokale IDE-Einstellungen und Zugangsdaten bleiben außerhalb des Commits. Bereits gestagte fremde Änderungen nicht stillschweigend übernehmen oder zurücksetzen.
4. `git diff --cached --check` und den gestagten Diff prüfen. Eine fehlende Git-Identität aus einer bestätigten bestehenden Projektidentität übernehmen oder gezielt klären; keine Autoridentität erfinden und keine globale Konfiguration ändern.
5. Einen sachlichen Commit erstellen, z. B. `feat: add sustainable colony economy (1.0.1)`. Die Nachricht beschreibt die tatsächliche Änderung. Bei mehreren unabhängig gelieferten Korrekturen sind mehrere sinnvolle Commits zulässig. Nach dem Commit den exakten HEAD-SHA festhalten.

## Push, PR und Merge

1. Den Versionsbranch mit `git push -u origin <Zielversion>` veröffentlichen. Kein Force-Push. Bei Ablehnung zuerst den neuen Remote-Stand untersuchen. Vor einer Wiederholung unklar ausgegangener Aktionen nachsehen, ob sie bereits erfolgreich waren.
2. Den GitHub-Connector bevorzugen. Nach einem bestehenden offenen PR mit demselben Head und Base `main` suchen; diesen aktualisieren oder genau einen PR erstellen. Falls der Connector die benötigte Aktion nicht unterstützt, die vorhandene GitHub CLI verwenden. Bei CLI-Aufrufen mehrzeilige Beschreibungen über eine Datei und `--body-file` übergeben.
3. Titel und Beschreibung benennen Problem, resultierendes Verhalten, Zielversion, konkrete Prüfergebnisse und verbleibende Grenzen. Keine neuen Anforderungen oder ungetestete Erfolge behaupten. Den endgültigen Diff gegen `main` prüfen.
4. PR-Head-SHA mit dem geprüften und gepushten Commit vergleichen. Statuschecks, Workflowläufe und erforderliche Reviews beachten. Bei fehlgeschlagenen Checks erst deren Ursache beheben und betroffene Prüfungen wiederholen. Kein Merge mit Admin-Bypass oder übersprungenem Branchschutz. Eine Wartebedingung offen ausweisen; nicht schon als gemerged melden.
5. Nach bestandenen erforderlichen Prüfungen über den PR mergen, vorzugsweise als Merge-Commit. Die GitHub-Mergeaktion mit `expected_head_sha` auf den geprüften Commit begrenzen. Bei unerwartet verändertem Head dessen Diff prüfen, regulär integrieren und die betroffenen Tests sowie den versionsgebundenen Build erneut für genau diesen neuen Stand ausführen. Bei Konflikten `origin/main` regulär in den Arbeitsbranch integrieren und erneut prüfen. Ist `main` inzwischen weitergelaufen, muss die Zielproduktversion weiterhin höher sein und jeder veröffentlichte Plattform-Buildzähler den entsprechenden Wert aus `origin/main` übersteigen. Kollisionen gezielt auflösen (nächste freie Produktversion bzw. kollidierenden Buildzähler auf Basiswert + 1 setzen), VERSION/Branch synchron halten und den resultierenden Player neu bauen; keine früher vergebenen Buildcodes wiederverwenden.
6. Den erfolgreichen Merge anhand des GitHub-Ergebnisses und PR-Status verifizieren. Erst dann lokal zurückwechseln und aktualisieren:

   ```powershell
   git fetch origin
   git switch main
   git pull --ff-only origin main
   powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
   ```

7. Abschließend prüfen: aktueller Branch `main`, lokaler HEAD entspricht `origin/main`, Zielversion und integrierte Änderungen sind vorhanden. Unbeteiligte Benutzerdateien bleiben erhalten. Versionsbranch nicht ungefragt löschen.

## Ergebnis

Kurz Zielversion, Commit-SHA, verlinkten PR, Merge-Ergebnis, aktuellen Branch und die aussagekräftigen Prüfungen nennen. Bei einer echten Sperre die letzte erfolgreiche Stufe und den konkreten Grund angeben; keine automatische Publikation nach einem fehlgeschlagenen Prüf- oder Merge-Schritt behaupten. Bereits erteilte Autorisierung innerhalb dieses Ablaufs nicht wiederholt abfragen.
