# M6: Eigenständige Computerfraktionen

Stand: 24. November 2026, Produktversion 1.0.7, Speicherformat 7. P14 und P15 sind lokal umgesetzt und automatisiert geprüft. Der Branch baut auf dem noch offenen Hexraster-PR #6 auf. Git-Integration und erneute manuelle Bedienungsabnahme sind getrennte Abschlussbedingungen.

## Spielen

In der Systemübersicht **M6 Gegen Computer spielen** wählen. Eine neue Partie startet mit Aster für den Spieler und Nereid für die Freien Stationen. Jede Seite erhält genau die bestehenden Anfangsmittel: eine Verwaltung, 16 Bewohner, 60 Nahrung, 100 Metall und 40 Bauteile sowie die gleichen Grundstraßen. Es gibt keine Anfangsflotten, Testtruppen, Forschungsfreischaltungen oder laufenden Rohstoffboni. Der Krieg zwischen beiden Fraktionen ist eine ausdrücklich angezeigte Szenariovorgabe; freie Diplomatie folgt M7.

Die ursprüngliche Aufbausitzung bleibt beim Einstieg im Speicher. Über **Zur vorherigen Kolonie zurück** kann man zurückwechseln. F5/F9 beziehungsweise Speichern/Laden benutzen für diese Partie `ai-scenario-save.json`; die grafischen Tests benutzen weiterhin ihren isolierten Testspeicher. Die früheren M3–M5-Prüfkarten bleiben als solche gekennzeichnet. M6 ist ein regulär wirtschaftendes Szenario, keine bereits aufgebaute Testarmee.

Die KI plant alle zwei Simulationssekunden. Pause hält auch ihre Planung an. Aufbau und Forschung geschehen im Hintergrund unabhängig von der Kamera. Eine Offensive kann lange auf reale Ressourcen warten: Im 40-Minuten-Kriegstest befindet sich die selbst aufgebaute Flotte noch in Vorbereitung. Der Test verspricht keinen Angriff zu einem festen frühen Zeitpunkt.

## Was umgesetzt ist

- **AI-001/002, ARCH-003:** `ObserveForAi` erzeugt einen abgetrennten fraktionsbezogenen Datensatz. Der `AiPlanner` erhält keinen Verweis auf `GameState` oder `GameSimulation`. Eigene Objekte werden kopiert; fremde Gebäude, Bodentruppen und Schiffe gelangen erst nach Sichtprüfung hinein. Fremde Inventare, Forschung, Produktionsaufträge und Truppen im Frachtraum werden nicht übergeben. Die statische Geländegeometrie einer kartierten Welt gilt als öffentliche Karte; abgebaute Vorkommensmengen und Infrastruktur nicht. Verdeckte Welten liefern ihren zuletzt bekannten Besitzer und Ort.
- **AI-004/005:** Prioritäten richten sich nach Nahrung, Erz, Metall, Bauteilen, Energie, Wohnraum und Arbeitskräften. Die KI legt fehlende Straßenkanten an und verbindet unterbrochene Anlagen wieder. Werkstätten werden abhängig vom frei verfügbaren Bauteilpuffer angehalten und wieder eingeschaltet. Bei belegten Kurieren und großem Lieferstau wird optionale Elektronikverarbeitung gedrosselt. Es entstehen keine zusätzlichen kostenlosen Kuriere. Bestehende Reservierungen werden bei verfügbaren Gütern abgezogen.
- **AI-006:** Die KI erforscht verfügbare Technik mit tatsächlichen Proben, sammelt die teure Kolonieladung am Hafen, besiedelt bekannte neutrale Standorte, richtet Hafenversorgung ein und setzt echte Frachter ein. Erkundung fliegt öffentliche Suchsektoren ab, keine geheim ausgelesenen Gegnerpositionen. Handel und Schutzverträge sind noch nicht verfügbare Alternativen aus M7.
- **AI-007/008:** Eine Operation besitzt Ziel, Heimat-/Rückzugswelt, Landeort, Phase, Grund, exklusive Schiffs-/Truppenzuweisungen und ein Startversorgungsbudget. Vorbereitung beschafft Werft, Eskorte, Träger, Infanterie, Pionier und gegebenenfalls Artillerie über reguläre Produktion. Beginn verlangt mindestens drei geladene Truppen inklusive Pionier, Eskorte und reale Nahrung/Bauteile. Es gibt genau eine exklusive Offensive je KI im M6-Umfang.
- **AI-008/009:** Bekannte Turmreichweiten und Schilde beeinflussen die Landefläche. Fehlt eine vertretbare Fläche, wartet die Operation. Abgewiesene Landestellen werden gespeichert. Nach einer Landung benutzt vorhandene Artillerie aufgeklärte Türme als Ziele; ungeeignete Verbände brechen ab. Ein bezahlter Pionier errichtet einen versorgten Landekopf, Infanterie sichert die tatsächliche Verwaltung. Türme, Depots und mobile Reserven verteidigen die eigene Kolonie. Fehlende Versorgung oder starke Schäden führen zu Nachschub, Reparatur oder physischem Rückzug.
- **AI-007/012:** Rückzüge benutzen Bewegung, Wiederverladung und Aufstieg; sie löschen keine eingeschlossenen Truppen. Bei fehlender eigener Rückzugsbasis bleibt der Abbruch nachvollziehbar blockiert. Nach der Rückkehr werden exklusive Zuweisungen freigegeben. Ein Wiederanlauf hat eine Sperrfrist von 60 Sekunden. Aufgeklärte Stellungen werden erneut bewertet; eine Wiederholung setzt eine geeignete Landefläche voraus. Der Verlust des Kriegsrechts bricht eine vorhandene Operation ab.

Die Wiederaufbaureserve beträgt grundsätzlich 16 Metall/4 Bauteile. Grundversorgung darf diese Reserve mit protokollierter Begründung verwenden. Das Sicherheitsprofil hält acht zusätzliche Metall zurück, das Technologieprofil zieht das Labor vor. Profile und Planungsparameter sind persistente beziehungsweise zentrale Daten; eine vollständige diplomatische Persönlichkeit und ein Schwierigkeitsmenü sind hier nicht behauptet.

## Nachvollziehbarkeit und Speicherformat

Pro KI werden die letzten 80 Entscheidungen mit Tick, Ziel, beobachteter Ursache, Alternative, Aktion und Befehls-ID gespeichert. Dies sind Entwicklungsdaten im Spielstand, kein allwissender Gegner-Inspektor in der normalen Spieloberfläche. Fremde Bauaufträge und Fertigstellungen erscheinen nicht als private Spielermeldungen. Sichtbare Invasionswarnungen bleiben erhalten.

Speicherformat 7 erhält Planungszeitpunkt, Erkundungsfortschritt, Profil, letzte Gebäudesichtungen, abgewiesene Landeorte, Operationen, Zuweisungen, Versorgungsbudget und Entscheidungsprotokoll. Das Budget bezeichnet die einem Träger zugeordnete Startladung und ist kein zweiter Warenbestand. Die tatsächlichen Waren bleiben in den regulären Inventaren und Frachträumen.

Versionen 1–6 werden über die bestehende Kette übernommen und erhalten eine leere KI-Liste. Alte Koloniespielstände bekommen dadurch keinen unerwartet aktivierten Gegner. Doppelte oder fremde Operationszuweisungen sowie ungültige Sichtungen und Zustände werden beim Laden abgelehnt.

## Reproduzierbare Nachweise

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
```

96 Core-Tests, davon 14 neue M6-Prüfungen:

| Szenario | Grenze und beobachtetes Ergebnis |
|---|---|
| Wirtschaft, Seed 7 | Nach 20 Minuten: 39 Bewohner, 443 erzeugtes Metall, 124 erzeugte Bauteile, drei erforschte Technologien; keine injizierten Güter |
| Weitere Wirtschaftskarten | Seeds 41/113, jeweils 20 Minuten: 39 Bewohner; 443/444 Metall und 124/122 Bauteile |
| Selbst aufgebaute Kriegsflotte | Nach 40 Minuten vier tatsächlich produzierte Schiffe; Operation wartet in Vorbereitung auf weitere reale Voraussetzungen |
| Friedliche Besiedlung, Seed 23 | Innerhalb 40 Minuten Mond durch bezahltes Kolonieschiff besiedelt; vier Technologien und vier vorhandene Schiffseinträge inklusive verbrauchtem Kolonieschiff |
| Vorbereitete Operationsfixture | Die ausdrücklich endlichen M5-Testbestände werden von derselben KI innerhalb 120 Sekunden durch Anflug, Landung, Landekopf und tatsächliche Eroberung geführt; dies ist ein separater taktischer Nachweis, keine Behauptung eines vollständig aus Normalstart finanzierten Sieges |
| Informationsschutz | Ein ungesehener Turm verändert weder Beobachtung noch nächste Entscheidung. Kopien verändern keine Simulationsbestände. Alte Kontakte aktualisieren sich nicht bei verborgenen Veränderungen |
| Anpassung und Rückzug | Bekannte Stellung ändert den Landeplan. Fehlende Transporter verhindern Ausführung. Beschädigte Flotte fährt tatsächlich zurück und gibt Zuweisungen frei; weggefallenes Kriegsrecht bricht den Auftrag ab |
| Persistenz und Takt | Identische Fortsetzung nach Save/Load; Pause und Schrittgruppierung erhalten Entscheidungen; doppelte/fremde Zuweisungen werden verworfen |

Windows-Mono-Build mit Unity 6000.3.29f1 erfolgreich. Der echte Player besteht 113 Einzelprüfungen und erzeugt 16 Renderbilder unter `TestResults/Smoke/20261124-115453`, darunter M6-Systemansicht und Spielerkolonie während der Hintergrundplanung. Die Prüfung verwendet `-batchmode` mit GPU und RenderTexture, ohne sichtbare Fenster, Screenreader, Computer-Use oder Eingriffe in den Arbeitsdesktop. Eine VM war nicht nötig. Die M6-Bilder des gleichartigen ersten Laufs `20261124-114543` wurden zusätzlich als Dateien betrachtet; im finalen Lauf sind die Bilder erzeugt und ihre Existenz geprüft.

Grenzen: Die Weltbilder enthalten keine IMGUI-Oberfläche. Erneute manuelle Bedienungs-/Beschriftungsabnahme, andere Plattformen, M7-Verträge/Koalitionen und M8-Gesamtszenario bleiben offen. Die KI ist eine deterministische regelbasierte Planung mit einzelnen Befehlen, keine optimale Strategie und kein maschinelles Lernen. Der GitHub-Unity-Build benötigt weiterhin separat eingerichtete Aktivierungs-Secrets; lokale Nachweise ersetzen den PR-Pflichtcheck nicht.
