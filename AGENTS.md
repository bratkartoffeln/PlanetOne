# Arbeitsregeln für PlanetOne

## Auftrag und Quellen

Dieses Repository enthält die Umsetzungsvorlage und das entstehende Unity-Projekt für PlanetOne. Vor Implementierungsarbeit README.md, docs/DECISIONS.md und das betroffene Paket in docs/IMPLEMENTATION_PLAN.md lesen. Die dort verlinkten Fachregeln und Abnahmeszenarien gehören zum Paket.

Aktuelle Nutzeranweisungen haben Vorrang vor diesen Projektregeln. Bestehende Benutzeränderungen erhalten. Der Nutzer hat Unity mit C# gewählt, die Einrichtung von Unity ausdrücklich beauftragt und den Beginn der Umsetzung autorisiert. Zusätzlich ist der versionsbasierte Commit-/PR-Ablauf für abgeschlossene Entwicklungsarbeit beauftragt: Versionsbranch, Commit, Remote-Push, PR-Merge und Rückkehr auf `main`. Store-Veröffentlichungen, Deployments und Repository-Neuinitialisierungen sind davon nicht umfasst.

## Entscheidungen und Umfang

- Unity und C# sind verbindlich gewählt. Die konkrete Editorversion und ihre Buildwerkzeuge werden in M0 eingerichtet und festgehalten. Projektgerüst, Kompilierproben und unabhängige Core-Tests dürfen während dieser Einrichtung entstehen; M0 ist erst nach seinen tatsächlichen Laufzeitnachweisen abgeschlossen.
- Routinemäßige Implementierungsentscheidungen innerhalb des beauftragten Prototyps selbstständig treffen und dokumentieren. Keine erneute pauschale Freigabe für Enginewahl oder einzelne Pakete einholen. Neue kostenpflichtige Dienste sind nicht Bestandteil der Festlegung.
- Nutzervorgaben, Arbeitsannahmen und offene Entscheidungen in docs/DECISIONS.md unterscheiden. Bei einer wirklich blockierenden Produktentscheidung gezielt nachfragen und unabhängige Arbeit fortsetzen.
- Ein Paket vollständig einschließlich der relevanten Prüfungen abschließen. Verwandte Zukunftsfunktionen nicht beiläufig vorziehen.
- Anforderungen durch ihre stabilen IDs referenzieren. Eine bewusste Regeländerung im zuständigen Dokument und in betroffenen Abnahmeszenarien nachführen.

## Technische Leitlinien

- Eine gemeinsame Simulation betreibt alle Welten. Ansichtswechsel, versteckte Szenen und Kameradistanz verändern die Spielregeln nicht.
- Spieler und KI verwenden dieselbe validierte Befehlsschnittstelle. KI erhält ausschließlich ihre erlaubte Sicht auf die Welt.
- Ressourcen und Truppen existieren an genau einem Ort. Reservierung, Verladung, Entladung, Zerstörung und Besitzwechsel dürfen nichts duplizieren.
- Spielregeln von Darstellung, Eingabegeräten und Plattformdiensten trennen. Keine universelle eigene Engine oder umfangreiche Abstraktionsbibliothek vorab bauen.
- Definitionen und veränderlichen Spielzustand trennen. Kosten, Zeiten und Reichweiten als überprüfbare Daten pflegen.
- Einzelspieler ist der erste Umfang. Kein Netzwerk-Lockstep, Onlinekonto, Cloudzwang oder ML-Dienst als Voraussetzung einführen.
- Dokumentation und sichtbare Spieltexte zunächst auf Deutsch; technische Identifikatoren einheitlich auf Englisch. Sichtbare Texte für spätere Übersetzungen von der Logik trennen.
- Der visuelle Stil ist minimalistisch: einfache Geometrie, dunkler Weltraum, zurückhaltende Palette und klare Zustandsanzeige. Lesbarkeit und Funktion haben Vorrang vor Detailassets oder aufwendigen Effekten.
- Der erste Prototyp verbindet Sonnensystem und bebaubare Oberfläche über einen Unity-unabhängigen C#-Kern. Der ausgewählte Planet und die Kamera gehören zur Darstellung, nicht zu einer zweiten Spielsimulation.

## Arbeitsablauf und Nachweis

1. Repository-Stand und Paketabhängigkeiten prüfen.
2. Betroffene Regeln, Module und relevante Prüfszenarien nennen.
3. Kleinste vollständige Umsetzung erstellen; vorhandene geeignete Enginefunktionen nutzen.
4. Aussagekräftige Regeln automatisiert prüfen und sichtbare Abläufe im Spiel kontrollieren, sobald ein ausführbares Projekt existiert.
5. Reale Start-, Build- und Testbefehle dokumentieren. Noch nicht vorhandene Befehle nicht als erfolgreich getestet ausgeben.
6. Paketstatus, Ergebnisse und verbleibende Einschränkungen nachführen.

Tests sollen Verhalten nachweisen, insbesondere Ressourcenintegrität, Sichtgrenzen, Verträge, Invasionstransfers und Save/Load. Keine Tests allein für reversible Text- oder Darstellungsänderungen erfinden. Keine funktionierende Prüfung wiederholt ausweiten, solange kein neuer Befund dies begründet.

Eine Debug-Prüfkarte darf Ausgangsbestände setzen; sie ist klar als Testfixture zu kennzeichnen. Im normalen Spiel dürfen daraus keine kostenlosen KI-Flotten oder unsichtbaren Ressourcenquellen entstehen.

## Definition der Fertigstellung

Für den beauftragten Abschluss eines Entwicklungs-Goals den projektspezifischen [Commit-/PR-Skill](.agents/skills/commit-pr/SKILL.md) verwenden. `main` bleibt nach ausdrücklicher Nutzerauswahl der Haupt- und PR-Zielbranch; kein `master` anlegen. Produktversion aus `VERSION` beziehen, standardmäßig die Patchnummer erhöhen und den Arbeitsbranch genau nach dieser Zielversion benennen. Engine-, Paket- und Savegame-Schemaversionen werden unabhängig verwaltet. Ein Goal mit beauftragter Git-Integration ist erst nach verifiziertem PR-Merge und lokal aktualisiertem `main` vollständig abgeschlossen.

Ein Paket gilt erst als fertig, wenn sein beobachtbares Verhalten funktioniert, relevante Prüfungen bestanden sind, der Start reproduzierbar dokumentiert ist und keine Kernfunktion durch einen Platzhalter nur vorgetäuscht wird. Nicht ausgeführte Plattformtests oder fehlende Werkzeuge ausdrücklich benennen. Konzept, implementierter Stand und geprüfter Stand bleiben unterscheidbar.
