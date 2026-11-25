# M7: Diplomatie und gemeinsamer Sieg

Stand: 24. November 2026. Produktversion **1.0.8**, Unity **6000.3.29f1**, Speicherformat **8**. P16–P18 sind lokal implementiert und automatisiert geprüft. Manuelle IMGUI-Bedienungsabnahme und GitHub-Integration sind getrennte Nachweise; ein lokaler Erfolg ersetzt keinen erforderlichen CI-Check.

## Einstieg und Umfang

**M7 Diplomatische Partie** startet drei unabhängige Fraktionen auf Aster, Nereid und Helios sowie einen neutralen Mond. Zwei regelbasierte KI bauen aus denselben Anfangsmitteln wie der Spieler auf: je 16 Bewohner, 60 Nahrung, 100 Metall, 40 Bauteile, keine Anfangsflotte. Alle Paare beginnen friedlich. Dies ist die M7-Partie, noch nicht das vollständige M8-Referenzszenario mit zwei Monden.

**Diplomatie und Sprungtor** öffnet Angebote, Vertragsdetails, Antworten, Kündigung, ausdrücklichen Vertragsbruch, Kriegserklärung, Lieferaufträge, Operationszusagen und Toraktivierung. Fraktionen behalten ihre eigenen Lager, Forschungen und Einheiten. Militärische Partner sind von Feinden farblich unterscheidbar; diplomatische Details werden als Text angezeigt.

**M7 Vertrags- und Torprüfkarte** ist ausdrücklich eine TESTFIXTURE: drei vorgebaute Kolonien mit Solarversorgung, Farm, Hafen und Tor; abgeschlossene Forschung; je ein Frachter, 24 Metall/12 Bauteile am Hafen und endliche Tor-Komponenten. Sie dient dem kurzen Verhandlungs-/Siegtest und ist kein regulärer Wirtschaftsstart. Die vorherige Partie lässt sich wiederherstellen. Reguläre M7-Partien verwenden `diplomacy-save.json`, die Prüfkarte getrennt `diplomacy-fixture-save.json`; der automatisierte Player-Test schreibt ausschließlich in sein separates Testverzeichnis.

## Verträge, Rechte und Handel — P16

- Paarweise Peace/War-Beziehungen; ausschließlich ausdrückliche Kriegserklärung. Waffenstillstand beendet gegenseitige Angriffsaufträge. Ablauf eines Friedensvertrags erzeugt keinen Krieg.
- Waffenstillstand, Nichtangriff, Handel, Zugang/Nutzung, Verteidigungspakt, Koalition, gemeinsame Operation und vereinbarte Kapitulation. Angebote speichern Beteiligte, Zustimmung, Zeitpunkte, Dauer, Kündigungsfrist und Gründe. Annahme prüft den aktuellen Zustand erneut. Ein Angebot allein erteilt keine Rechte.
- Standarddauer zehn Simulationsminuten, Kündigungsfrist 60 Sekunden; Daten in `TreatyTerms`. Vertragsbruch benötigt einen gesonderten bestätigten Befehl und erzeugt einmalig öffentlich erkennbaren Vertrauensverlust. Gewöhnliche Bewegung oder Waffenwahl bricht keinen Vertrag.
- Ziviler Transit, militärischer Transit/Torpassage, Landung, Stationierung, Werftreparatur und Feldversorgung werden unabhängig geprüft. Geschützte Orbitalzugänge haben Radius 3; Annäherung ohne Recht wird abgewiesen, freier Raum bleibt frei. Handel erlaubt keine Truppenlandung. Eine feindliche Landung braucht Krieg und die bisherigen tatsächlichen Landebedingungen.
- Handel nennt zwei eigene Lieferhäfen, Güter und Mengen. Jeder Partner stellt seinen eigenen Frachter. Die bestehenden Frachtreservierungen verhindern doppelte Verkäufe; nur tatsächliche Ankunft zählt als Lieferung. Kündigung beziehungsweise Verlust gibt Reservierungen frei. Geladene Ware bleibt bis Ankunft Eigentum des Lieferanten und kehrt bei Abbruch physisch zurück; zerstörte Ladung zählt als Verlust. Volle Häfen warten, fehlende Vorräte oder verlorene Häfen erklären den Status.
- Der Verteidigungspakt sagt den paarweisen Kriegseintritt gegen einen äußeren Angreifer zu; er aktiviert keine Beteiligung am selbst begonnenen Angriffskrieg. Ein bestehender Schutzvertrag gegenüber dem Angreifer wird nicht still gebrochen. Konkrete Schiffsbeiträge werden zusätzlich über gemeinsame Operationen vereinbart.
- KI entscheidet über eigene Bestände, öffentliche Beziehungen/Ereignisse und ihre Vertragskopien. Sie bietet Nichtangriff, Koalition, Handel und konkrete Unterstützung auch anderen KI an, antwortet, liefert mit eigenen Frachtern, stellt verfügbare Eskorten und kündigt nach bekanntem Vertrauensbruch. Unbezahlbare Gegenleistungen oder fehlende Kräfte rechtfertigen Ablehnung. Keine fremden Inventare gelangen in den Entscheider.

## Koalitionen und gemeinsame Operationen — P17

- Koalitionsbeitritt benötigt Zustimmung aller bisherigen Mitglieder und des Beitretenden; interne Kriege verhindern ihn. Koalitionen teilen aktuelle Sensoren, ohne Lager oder Steuerung fremder Armeen freizugeben. Nach Austritt bleiben vergangene Sichtungen erhalten, neue werden nicht übertragen.
- Operationsangebote benennen Verteidigungsort, Eskortziel oder Eroberungswelt und den künftigen Eigentümer. Jeder Teilnehmer sagt vorhandene Schiffe exklusiv zu. Persönliche KI-Operationen und andere gemeinsame Zusagen können dieselben Schiffe nicht nochmals reservieren.
- Bereitschaft frühestens zwei Sekunden nach Zusage. Gemeinsame Invasion: je Teilnehmer beladener Träger mit mindestens drei Truppen einschließlich Pionier, bewaffnete Eskorte, acht Nahrung und acht Bauteile an Bord. Start prüft erneut Krieg, Besitz und Verfügbarkeit. Flug, Landung, Entladung, Pionier-Landekopf und Besetzung nutzen reguläre Befehle. Gemeinsame Infanterie setzt den Fortschritt ihrer Partner nicht zurück; der vereinbarte Eigentümer erhält die gesicherte Welt.
- Verlust oder geänderte Rechte brechen die Zusage mit Grund ab; verfügbare Raumkräfte kehren zum eigenen Hafen zurück, Reservierungen werden freigegeben. Bereits gelandete Kräfte bleiben tatsächlich vor Ort und können mit ihren Transportern evakuiert werden; ein Abbruch teleportiert keine eingeschlossenen Truppen.
- Nach Ende der Stationierungsrechte gelten 120 Sekunden lokale Räumung. Nur die zuvor berechtigten Kontingente und ihre vorhandenen Landeträger sind geschützt. Abzug zum Evakuierungsträger, Einladen und Start sind erlaubt; Angriff, neue Besetzung und weiteres Eindringen sind gesperrt. Gastgeber dürfen diese Kontingente ebenfalls nicht angreifen. Neue Invasionskräfte erhalten keinen Schutz. Nach Ablauf gelten normale Kampfregeln, ohne Truppen zu löschen.

## Sprungtor und Ausscheiden — P18

- Regulär baubares Sprungtor: 40 Metall/16 Bauteile, 30 Sekunden Bauzeit, acht Energie/vier Arbeitskräfte. Betreiber benötigt Elektronik und fortgeschrittene Sensoren. Bau und Komponenten benutzen die vorhandenen lokalen Kuriere; Partner können zuvor per Handelsfrachter zum Hafen liefern.
- Projektkomponenten: 12 Nahrung, 40 Metall, 24 Bauteile, acht Kristalle, acht Elektronik. Keine abstrakten Siegpunktlieferungen. Erst bei vollständiger physischer Anlieferung beginnt die öffentlich angekündigte Sicherungsphase von 60 Sekunden.
- Sicherung braucht unveränderte Teilnehmer, aktive Fraktionen, Kontrolle über Welt und Gebäude, Betrieb, Energie/Personal, Nahrung und einen gesicherten Nahbereich. Änderungen werden vor der Siegauswertung geprüft; Unterbrechung setzt den Fortschritt zurück, erhält aber unverbrauchte Bestände. Die Komponenten werden einmalig beim erfolgreichen Abschluss verbraucht.
- Alle gespeicherten gültigen Koalitionsteilnehmer gewinnen gemeinsam. Versteckte Restschiffe verhindern das Ziel nicht. Unabhängige Abschlüsse im selben Tick ergeben einen ausgewiesenen geteilten Erfolg. Spätere Projekte ändern einen bereits feststehenden Siegerkreis nicht.
- Hauptstadtverlust allein eliminiert nicht. Eine weitere Kolonie oder ein vollständig ausgerüstetes Kolonieschiff mit legal erreichbarer freier Gründungswelt erhält die Fraktion; ein Aufklärer genügt nicht. Ein unbekannter, aber erreichbarer legaler Standort wird bei der Ausscheidensprüfung nicht ausgeschlossen.
- Bestätigte Kapitulation ohne Übergabe stellt mobile Kräfte außer Dienst und macht Restgebäude neutral/inaktiv unter Erhalt ihrer tatsächlichen Lager. Ein beiderseits angenommener Kapitulationsvertrag übergibt stattdessen bestehenden Restbesitz mit seinen IDs. Befehlsfähigkeit, Produktion, Verträge und exklusive Zusagen enden einmalig; verbrauchte Bau-/Produktionsmittel werden nicht kostenlos erstattet. Neutrale Anlagen können physisch gesichert und bei Bedarf repariert werden.

## Ausgeführte Prüfungen

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Prototype.ps1
```

**126 Core-Tests**, darunter 30 M7-Verhaltensszenarien: Rechte und bewusster Krieg/Bruch, Waffenstillstand/Ablauf, einstimmiger Beitritt, defensive Pflichten, reale beidseitige Lieferung, konkurrierende Mengen, Abbruch/Verlust, Sichtende, Räumung, exklusive Beiträge und tatsächliche Ankunft, vollständige gemeinsame Invasion mit zwei verladenen Armeen, bezahlter Torbau samt lokaler Komponentenlieferung, friedlicher Sieg, Änderungen im letzten Tick, gleichzeitiger Erfolg, Wiederaufbau/Eliminierung, einmalige Kapitulation und bestehende Besitzübergabe, Save/Load-Fortsetzung, fehlerhafte Zusagen, KI-KI-Verhandlungen, KI-Lieferung/Eskorte/Kündigung sowie getrennte Transit-, Reparatur- und Versorgungsrechte.

Der gemeinsame Invasionstest verwendet deklarierte endliche Streitkräfte, verlädt sie über echte Befehle und läuft nach Operationsbeginn 240 Simulationssekunden. Er behauptet keinen vollständigen Sieg aus dem regulären Wirtschaftsstart innerhalb dieses Zeitfensters. Der Torbautest finanziert den Bau aus seinen deklarierten Beständen und wartet 200 Sekunden auf Bau und lokale Lieferungen. KI-Verhandlung läuft 120 Sekunden, die vorbereitete KI-Handels-/Eskortprüfung 180 Sekunden.

**Windows-Build und 131 Player-Einzelprüfungen** erfolgreich, **18 Renderbilder** erzeugt. Der Player prüft unter anderem einen normalen M7-Start, Verhandlungen zweier KI, Controller-Befehle, einstimmige Koalition, getrennte Landerechte, öffentliche Toraktivierung, exaktes Speichern/Laden und gemeinsamen Abschluss. Die neuen Weltbilder `diplomacy-coalition.png` und `diplomacy-gate.png` wurden visuell kontrolliert. Abschließender geprüfter Lauf: `TestResults/Smoke/20261124-131634`; Torbild dieses Laufs visuell kontrolliert. Koalitions- und Toransicht des vorherigen Laufs `20261124-131430` ebenfalls visuell kontrolliert.

Tests laufen ohne sichtbare Fenster, Screenreader, Computer-Use oder Eingriffe in den Arbeitsdesktop. Weltbilder entstehen über `Camera.Render`/RenderTexture im echten Batch-Player. **IMGUI-Layout und manuelle Bedienbarkeit sind damit nicht visuell abgenommen.** Weitere Plattformen, umfassendes Balancing, das vollständige M8-Szenario und Lastmessungen bleiben offen. GitHub-Build/Installer benötigen weiterhin dort konfigurierte Unity-Aktivierungs-Secrets.

Speicherformat 8 ergänzt Verträge, Beiträge, Räumungsfristen, Fraktionsstatus, Torfortschritt und Sieger. V1–V7 werden über die bestehende Kette migriert; bestehende Welten/Bestände bleiben erhalten, Diplomatie wird nicht rückwirkend aktiviert. Der unveränderte V5-Invasionsspielstand und identische Fortsetzung bleiben Bestandteil der Regressionstests.
