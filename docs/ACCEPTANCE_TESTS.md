# Abnahmeszenarien für PlanetOne

Diese Szenarien übersetzen die Spielregeln in beobachtbare Nachweise für Codex. Sie sind Prüfvorlagen, keine bereits vorhandenen oder ausgeführten Laufzeittests. Verbindliche Regeln stehen in [GAME_DESIGN](GAME_DESIGN.md), [AI_DIPLOMACY](AI_DIPLOMACY.md) und [TECHNICAL_ARCHITECTURE](TECHNICAL_ARCHITECTURE.md). Die Umsetzung erfolgt nach [IMPLEMENTATION_PLAN](IMPLEMENTATION_PLAN.md).

## Durchführung und Ergebnisprotokoll

Jeder Nachweis nennt Paket, Regel-IDs, Spiel-/Definitionsversion, Szenario, Seed, Anfangszustand, Befehle, erwartetes Ergebnis und tatsächliches Ergebnis. Für KI-Tests werden Planungsintervalle und erlaubtes Zeitfenster vor dem Test festgelegt. Eine beliebig lange Wartezeit gilt nicht als Erfolg.

Automatisierte Prüfungen eignen sich für Mengen, Rechte und Zustandswechsel. Sichtbare Abläufe müssen zusätzlich manuell auf Lesbarkeit, verständliche Warnungen und Bedienbarkeit geprüft werden. Szenariostartbestände sind explizite Testdaten und keine kostenlosen Produktionsregeln für normale Fraktionen.

Alle hier genannten Zeit-, Kapazitäts- und Materialwerte sind lokale Testeingaben. Sie legen keine endgültige Spielbalance fest. Fehlende Werkzeuge, nicht verfügbare Plattformen und nicht ausgeführte Prüfungen bleiben als solche gekennzeichnet.

## Referenzkarten

| Karte | Aufbau | Zweck |
|---|---|---|
| F01 Wirtschaft | Eine Kolonie, Mine, Verarbeitung, zwei Lager, zwei mögliche Straßenwege | Vorräte, Baustellen, Produktionspausen und Erreichbarkeit |
| F02 Transport | Zwei Oberflächen und ein Mond mit bewegten Bezugspunkten, Frachter und Raumhäfen | Orbits, physische Lieferung, Kapazität und Ansichtswechsel |
| F03 Invasion | Zwei Fraktionen, Verteidigung mit zwei Zugängen, alternative Landefläche, reale Transportflotte | Tower Defense, Landung, Besetzung, Nachschub und Rückzug |
| F04 Diplomatie | Drei Fraktionen mit eigenen Beständen und Stationierungsflächen | Handel, getrennte Rechte, Zusagen und Koalitionsänderungen |
| F05 Vollständige Partie | Ein Stern, drei Startwelten, zwei Monde, Spieler und zwei KI, Sprungtorprojekt | Gesamter Spielkreislauf und gemeinsamer Sieg |

Die Karten sind zunächst Spezifikationen. Ihre ausführbaren Szenariodaten werden im jeweiligen Paket angelegt und versioniert. Erreichte Teilmeilensteine dürfen noch nicht vorhandene Nachbarsysteme durch explizite Fixtures vorbereiten, aber nicht als fertig ausgegeben werden.

## TEST-001 Gemeinsame Zeit und Pause

- **Pakete:** P02, später P08 und P19. **Regeln:** CORE-007, CORE-008, ARCH-001, ARCH-010.
- **Gegeben:** Zwei Orte mit identischem zeitabhängigem Vorgang; einer ist sichtbar.
- **Wenn:** Eine feste Zahl Simulationsschritte einmal mit und einmal ohne Ansichtswechsel durchlaufen wird.
- **Dann:** Beide Läufe besitzen dieselben Mengen und Fortschritte. Kamera und Renderbildrate verändern die Simulation nicht.
- **Zusatz:** In Pause verändern sich weder Produktion noch Bewegung, Forschung, Schaden oder Vertragsfristen. Eingereihte Befehle dürfen sich ändern; sie werden nach Fortsetzen geordnet und erneut validiert.

## TEST-023 Wabenraster und Spielstandübernahme

- **Pakete:** P03, P04, P06, P07, P12. **Regeln:** SURFACE-001/005, COMBAT, INV-003, ARCH-002/005.
- **Gegeben:** Gerade und ungerade Hexzeilen, Randfelder, ein laufender Kurier, ein Bodenweg und ein alter V5-Spielstand.
- **Wenn:** Jede der sechs Kanten ausgewählt und überquert wird, Straßen schräg anschließen, Deckung/Sicht geprüft und zwei Träger zu Landungen befohlen werden.
- **Dann:** Maus-/Kameraprojektion und sichtbare Wabe stimmen überein; sechs gleich lange Nachbarschritte, keine Verbindung über nicht benachbarte Quadratdiagonalen. Bau erhält reale Lieferungen. Sicht und Deckung folgen den Hexzellen. Sieben Landezellen werden geprüft und exklusiv reserviert.
- **Spielstand:** Migration verändert außer der Schemaversion keine Bestände, Kennungen oder gespeicherten Wege. Fortsetzung und erneutes Save/Load sind reproduzierbar; neue Regelentscheidungen verwenden Hexgeometrie.
- **Nachweis:** Acht Core-Szenarien plus Kamera-Picking im fensterlosen Player, [HEXRASTER_NACHWEIS](HEXRASTER_NACHWEIS.md).

## TEST-002 Lokale Waren und Produktionsunterbrechung

- **Pakete:** P04, P05. **Regeln:** SURFACE-005, ECO-002, ECO-005, ECO-006.
- **Gegeben:** Das Testrezept verbraucht 2 Erz und erzeugt 1 Metall; ein erreichbares Lager enthält 10 Erz. Ein anderes, unerreichbares Lager besitzt zusätzliche Ware.
- **Wenn:** Ein Zyklus gestartet, durch Energieausfall pausiert und anschließend fortgesetzt wird.
- **Dann:** Insgesamt werden genau 2 Erz verbraucht und genau 1 Metall erzeugt. Kein zweiter Verbrauch beim Fortsetzen; kein Zugriff auf unerreichbare Ware.
- **Zusatz:** Voller Ausgaberaum verhindert einen neuen Zyklus. Abbruch und Zerstörung beachten die ausgewiesene Rückgewinnungsregel und duplizieren keine Menge.

## TEST-003 Reservierungen und paralleler Bedarf

- **Pakete:** P09. **Regeln:** LOG-003, LOG-004, ARCH-011.
- **Gegeben:** Eine Quelle besitzt 100 Metall und schützt 20 als Eigenreserve. Zwei Ziele und passende Frachter fordern je 60 Metall an.
- **Wenn:** Beide Aufträge im selben Planungszyklus verarbeitet und anschließend erneut ausgewertet werden.
- **Dann:** Insgesamt sind höchstens 80 Metall reserviert; kein Lagerplatz und Frachtraum ist überbucht. Wiederholte Auswertung bestellt bereits zugesagte Mengen nicht doppelt.
- **Zusatz:** Aufträge bleiben unter den getesteten Grenzwerten, wenn ein Ziellager währenddessen gefüllt wird oder ein Träger wegfällt.

## TEST-004 Abbruch und Verlust echter Fracht

- **Pakete:** P09, P11. **Regeln:** LOG-012, LOG-013, ARCH-011.
- **Gegeben:** Ein Auftrag über 40 Metall und bekannte Startmengen aller Inventare.
- **Wenn:** Der Auftrag vor Beladung abgebrochen wird.
- **Dann:** Reservierungen sind frei; die Ware ist unverändert am Ursprung.
- **Wenn:** Derselbe Ablauf nach tatsächlicher Beladung abgebrochen oder der Träger zerstört wird.
- **Dann:** Bei Abbruch bleibt die Ware im Träger bis zur realen Entladung. Bei Zerstörung wird sie genau einmal als Verlust erfasst. Es gibt keine Rückerstattung am Ursprungsort.
- **Zusatz:** Nach feindlicher Hafenübernahme wird eine bereits laufende Lieferung neu bewertet, statt automatisch an den Gegner zu entladen.

## TEST-005 Bewegte Ziele und Hintergrundsimulation

- **Pakete:** P08, P09, P11. **Regeln:** SPACE-002, SPACE-003, CORE-007, ARCH-012.
- **Gegeben:** Ein Frachter fliegt zu einem Mond, der seinen Planeten umkreist. Ein zweiter Ort produziert oder kämpft.
- **Wenn:** Der Spieler mehrfach die Oberfläche und Raumansicht wechselt.
- **Dann:** Der Frachter erreicht den bewegten Zugang ohne manuelle Nachsteuerung. Der Hintergrundvorgang erzielt bei demselben Befehlsablauf dasselbe Ergebnis wie unter durchgehender Beobachtung.

## TEST-006 Erreichbare Forschung und Wiederaufbau

- **Pakete:** P05, P10, P21. **Regeln:** SPACE-007, ECO-004, ECO-007, RES-001, RES-003, RES-005, RES-006.
- **Gegeben:** Heimatwelt mit regulären Anfangsressourcen und ohne importierte Kristalle.
- **Wenn:** Grundversorgung, Ersatzfrachter, Kolonisierung und einfache Belagerungsfähigkeit aufgebaut werden.
- **Dann:** Kein Schritt setzt Kristalle oder eine erst durch sich selbst freigeschaltete Fähigkeit voraus. Ein späteres Projekt mit Probe beginnt erst nach deren tatsächlicher Anlieferung zum Forschungsstandort.
- **Zusatz:** Verlorene Frachter lassen sich lokal ersetzen. Der Technologiegraph hat keine Zyklen oder fehlenden Definitionsverweise. Ein freigeschaltetes Schiff braucht weiterhin Werft, Zeit und Material.
- **Gründungsprüfung ab P08:** Ein regulär gebautes Kolonisationsschiff gründet genau eine Kolonie aus seinen tatsächlichen Anfangsbeständen. Wiederholung oder ein inzwischen besetztes Ziel erzeugen keine zweite Kolonie. Siehe SPACE-008 und SPACE-009.

## TEST-007 Mauern, Gegenmittel und Versorgung

- **Pakete:** P06, P07. **Regeln:** DEF-001, DEF-002, DEF-005, DEF-007, LOG-009, LOG-010.
- **Gegeben:** Eine versorgte Stellung, geschlossener zerstörbarer Mauerabschnitt und zwei mögliche Angriffsrichtungen.
- **Wenn:** Ein geeigneter Angreifer aufklärt und entweder einen Durchbruch oder einen Artillerieangriff ausführt.
- **Dann:** Die Stellung ist taktisch überwindbar; keine Einheit wird durch Hindernisse teleportiert. Die Verteidigung kann mit Reserven reagieren.
- **Zusatz:** Unterbrochener Nachschub reduziert die tatsächlichen Vorräte. Nach Verbrauch kann die betroffene Waffe nicht weiterfeuern. Eine neue Baustelle ist während der Bauzeit angreifbar und nicht sofort kampfbereit.

## TEST-008 Identität geladener Bodentruppen

- **Pakete:** P12, P19. **Regeln:** COMBAT-004, LOG-011, LOG-012, ARCH-007.
- **Gegeben:** Vier Bodeneinheiten mit verschiedenen IDs und dokumentierten Schäden, Munitionswerten und Eigentümern.
- **Wenn:** Vier Einheiten verladen, zwei entladen und der Träger anschließend zerstört wird.
- **Dann:** Zwei Einheiten stehen mit denselben IDs am Ziel; die übrigen zwei gelten als vernichtet. Keine der vier ist zusätzlich am Ursprung vorhanden. Schäden und Munition werden beim Ortswechsel nicht zurückgesetzt.
- **Zusatz:** Wiederholte Verarbeitung desselben Lade-/Entladebefehls erzeugt weder Kopien noch einen zweiten Verlust.

## TEST-009 Landung, Flak und unterbrochener Nachschub

- **Pakete:** P12, P13. **Regeln:** INV-001, INV-006, INV-011, DEF-004, LOG-010.
- **Gegeben:** Eine aufklärbare Invasionsflotte, Bodenflugabwehr und eine alternative Landefläche.
- **Wenn:** Ein Transporter absteigt und seine zweite Welle im All abgefangen wird.
- **Dann:** Flak wirkt nur in ihren gültigen Übergangsphasen. Bereits entladene Truppen bestehen weiter, benötigen anschließend ihre örtlichen Vorräte und dürfen nur mit vorhandenen Transporten evakuiert werden.
- **Manuell:** Aufklärung, Warnung und Abstieg lassen den Verteidiger sinnvoll reagieren; die Ursache einer gescheiterten Landung ist sichtbar.

## TEST-010 Eroberung und fortbestehender Widerstand

- **Pakete:** P13. **Regeln:** INV-007 bis INV-010, COMBAT-009.
- **Gegeben:** Ein Planet mit Verwaltung, Versorgungspunkt, zivilem Bezirk und verbliebenen Verteidigern.
- **Wenn:** Nur der Orbit gewonnen oder die Verwaltung nur beschossen wird.
- **Dann:** Der Planet wechselt nicht automatisch den Besitzer.
- **Wenn:** Berechtigte Infanterie die Verwaltung tatsächlich sichert und einen gültigen erreichbaren Versorgungspunkt hält.
- **Dann:** Der strategische Besitz wechselt gemäß Operationszuordnung. Lagerbestände bleiben real; verbliebene Verteidiger bleiben bestehen. Versteckte Einheiten außerhalb des Sicherungsbereichs blockieren nicht pauschal die Übernahme.
- **Zusatz:** Zerstörte notwendige Kontrollanlagen lassen sich nach den Spielregeln wieder nutzbar machen; keine dauerhaft uneroberbare Ruine.

## TEST-011 Sichtgrenzen und identische Befehlsrechte

- **Pakete:** P03, P06, P14. **Regeln:** SPACE-005, COMBAT-005, AI-002, ARCH-003, ARCH-008.
- **Gegeben:** Zwei identische beobachtbare Welten; nur in einer existiert zusätzlich ein ungesehener gegnerischer Turm.
- **Wenn:** Dieselbe KI mit identischem Zufallszustand ihre nächste Entscheidung trifft.
- **Dann:** Die zusätzliche geheime Tatsache verändert weder ihr Wissensmodell noch eine darauf gestützte Entscheidung. Nach zulässiger Aufklärung darf der Plan abweichen.
- **Zusatz:** Spieler und KI erhalten für denselben unberechtigten Befehl denselben Ablehnungsgrund. Ein Angriff auf eine letzte bekannte Position verfolgt kein verborgenes bewegtes Ziel.

**M6-Nachweis zu TEST-011:** Gleiche Beobachtung und nächste Entscheidung trotz ungesehenem Turm; abgetrennte Daten verändern keine Originalinventare. Letzte Sichtungen aktualisieren sich erst nach erneuter Aufklärung. Siehe [M6_NACHWEIS](M6_NACHWEIS.md).

## TEST-012 KI erkennt den tatsächlichen Engpass

- **Pakete:** P14. **Regeln:** AI-001, AI-004, AI-005.
- **Gegeben:** Ausreichende Metallproduktion; Transportleistung liegt unter dem bekannten Verbrauch. Eine zweite Testvariante besitzt ausreichend Frachter, aber zu wenig Produktion.
- **Wenn:** Die KI innerhalb des festgelegten Planungsfensters reagiert.
- **Dann:** Die gewählte Maßnahme passt zur jeweiligen Ursache, etwa Transportkapazität erhöhen oder Verbrauch senken im ersten Fall, Produktion erschließen im zweiten. Aufträge kosten reale Ressourcen.
- **Nachweis:** Entscheidungsprotokoll nennt Wissen, Engpass, Alternative und tatsächlich ausgeführten Befehl.

**M6-Nachweis zu TEST-012:** Unterbrochene Straße wird innerhalb zehn Simulationssekunden wieder verbunden; bei ausgelasteten Kurieren und zwölf offenen Lieferungen stoppt optionale Elektronikverarbeitung. Grundaufbau/Reserven und Forschung werden mit drei Seeds in jeweils 20 Minuten geprüft; friedliche echte Besiedlung mit Seed 23 innerhalb 40 Minuten.

## TEST-013 KI passt eine Invasion an

- **Pakete:** P15. **Regeln:** AI-007, AI-008, AI-009.
- **Gegeben:** Ein aufgeklärter Turmriegel stoppt die erste Armee. Ein anderer Landeort oder ein erreichbares Belagerungsmittel ist verfügbar.
- **Wenn:** Die KI den nächsten Versuch plant.
- **Dann:** Sie ändert den begründeten Plan, beschafft passende Kräfte, wartet auf echte Verstärkung oder bricht ab. Kein identischer endloser Angriff und keine kostenlose zweite Armee.
- **Zusatz:** Fehlende Transportplätze verhindern einen verfrühten Start. Beim Rückzug werden Operationen und Reservierungen bereinigt; die Einheiten müssen tatsächlich zurückfahren oder abgeholt werden.

**M6-Nachweis zu TEST-013:** Sichtbare Turmstellung verändert Landeplanung; ohne Träger kein Start. Rückzug mit beschädigtem realem Träger und Freigabe der Zuweisungen geprüft. Eine separat vorbereitete endliche Operationsfixture erreicht innerhalb 120 Sekunden die tatsächliche Eroberung. Das ist kein garantierter Sieg aus normalen Anfangsmitteln; [M6_NACHWEIS](M6_NACHWEIS.md).

## TEST-014 Verträge trennen Handel und Militärrechte

- **Pakete:** P16. **Regeln:** DIP-001 bis DIP-005.
- **Gegeben:** Ein Handelsvertrag und ziviler Hafenzugang zwischen zwei friedlichen Fraktionen, aber kein militärisches Stationierungsrecht.
- **Wenn:** Ein Träger Handelsware liefert und zugleich Truppen ausladen möchte.
- **Dann:** Der zulässige Handel wird abgewickelt; die Truppenlandung bleibt mit verständlichem Grund gesperrt. Material wird erst beim tatsächlichen Übergang gutgeschrieben.
- **Zusatz:** Ein selbst begonnener Angriffskrieg zieht unbeteiligte Vertragspartner nicht automatisch in den Krieg.
- **Kriegszustand:** Nach DIP-011 beginnt eine vertragslose Paarung ohne gegenteilige Szenariovorgabe friedlich. Angriff benötigt eine bewusste Kriegserklärung; eine gültige feindliche Landung benötigt anschließend kein Stationierungsrecht des Verteidigers. Waffenstillstand beendet Angriffsaufträge und erzeugt nach Ablauf keinen automatischen Krieg.

## TEST-015 Zusagen und gemeinsame Eroberung

- **Pakete:** P17. **Regeln:** DIP-006, AI-007, INV-008.
- **Gegeben:** Zwei Koalitionsmitglieder planen eine Invasion und vereinbaren Fraktion A als künftigen Eigentümer. Die Infanterie von B erreicht die Verwaltung zuerst; B stellt auch den Landekopf.
- **Wenn:** Beide die realen Sicherungs- und Versorgungsbedingungen gemeinsam erfüllen.
- **Dann:** A erhält die vereinbarte strategische Kontrolle. B behält seine Einheiten. Vereinbarte Versorgungsrechte erlauben die Operation; dieselbe Eskorte wird nicht gleichzeitig einer unvereinbaren zweiten Operation zugesagt.
- **Zusatz:** Ein notwendiger Planabbruch wird mit Grund gemeldet und gibt Zusagen frei.

## TEST-016 Koalitionsende und Räumung

- **Pakete:** P17. **Regeln:** AI-003, DIP-008, DIP-009.
- **Gegeben:** Berechtigt stationierte Truppen auf fremder Oberfläche und geteilte Koalitionssicht.
- **Wenn:** Mitgliedschaft und Stationierungsrechte wirksam enden.
- **Dann:** Neue Sichtdaten werden nicht weiter geteilt. Die betroffenen Kontingente besitzen eine sichtbare Räumungsphase und können nicht sofort aus der Basis heraus angreifen. Gastgeber greifen sie während ihres örtlichen geschützten Abzugs ebenfalls nicht an.
- **Zusatz:** Die Schutzphase erfasst keine neuen Invasionstruppen und keine externen Angriffsflotten. Nach Ablauf gelten normale Beziehungen; Einheiten werden nicht gelöscht.

## TEST-017 Gemeinsamer und friedlicher Sieg

- **Pakete:** P18, P21. **Regeln:** WIN-001, WIN-002, WIN-003, WIN-005, WIN-006.
- **Gegeben:** Koalition aus Spieler und KI; Toreigentümer besitzt die nötige Forschung, alle Beiträge stammen aus realen Beständen.
- **Wenn:** Komponenten geliefert und Betriebsbedingungen während der ganzen Aktivierung erfüllt werden.
- **Dann:** Alle gültigen Teilnehmer gewinnen gemeinsam. Ein verborgener gegnerischer Aufklärer verhindert das Ergebnis nicht.
- **Zusatz:** Dieselbe Prüfung funktioniert, wenn sich alle Parteien friedlich zusammenschließen; der Zusammenschluss selbst ersetzt den Projektbau nicht.

## TEST-018 Änderung im letzten Siegschritt

- **Pakete:** P18. **Regeln:** WIN-003, WIN-004, WIN-005, ARCH-010.
- **Gegeben:** Eine Toraktivierung steht unmittelbar vor Abschluss.
- **Wenn:** Im letzten Tick die Mitgliedschaft wechselt, die erforderliche Kontrolle verloren geht oder die Betriebsversorgung ausfällt.
- **Dann:** Die laufende Aktivierung bricht vor der Siegauswertung ab und ihr Fortschritt wird zurückgesetzt. Tatsächlich erhaltene Anlagen und unverbrauchte Vorräte werden nicht erneut erzeugt oder gelöscht.
- **Zusatz:** Ein zulässiger Neustart prüft alle Voraussetzungen erneut. Zwei unabhängige gültige Abschlüsse im selben Tick verwenden die dokumentierte Regel für gemeinsamen Szenarioerfolg.

## TEST-019 Niederlage und Wiederaufbaumöglichkeit

- **Pakete:** P18. **Regeln:** WIN-007.
- **Gegeben:** Varianten mit einer zweiten Kolonie, einem vollständig ausgerüsteten Kolonieschiff samt legalem Ziel oder nur einem Aufklärer.
- **Wenn:** Die bisherige Hauptstadt verloren geht.
- **Dann:** Die ersten beiden Varianten bleiben im Spiel. Ohne Kolonie und tatsächliche Neugründungsmöglichkeit scheidet die Fraktion aus; ein Aufklärer genügt nicht. Besitz- und Vertragsfolgen folgen den Szenarioregeln.
- **Abwicklung:** Ein zusätzlicher Fall enthält eine Restflotte, Ladung, eine Reservierung und eine gemeinsame Operationszusage. Nach WIN-008 erfolgt die Bereinigung genau einmal; keine kostenlose Warenrückgabe, kein weiterer Flottenauftrag und keine verwaiste Zusage. Ein verbleibendes neutrales Gebäude behält nur seinen tatsächlichen Bestand.

## TEST-020 Speichern an kritischen Übergängen

- **Pakete:** P03 fortlaufend, P19. **Regeln:** ARCH-007, ARCH-011, ARCH-014, AI-012, DIP-002.
- **Gegeben:** Je ein Zustand während Beladung, Flug, Abstieg, Besetzung, Vertragskündigung und Toraktivierung.
- **Wenn:** Gespeichert, geladen und derselbe Befehlsablauf fortgesetzt wird.
- **Dann:** Mengen, IDs, Eigentum, Fristen, Wissensstand, Zusagen und Fortschritt bleiben korrekt. Keine duplizierte Fracht oder verlängerte Frist. Ergebnisse entsprechen dem Vergleichslauf ohne Laden auf derselben Testlaufzeit.
- **Zusatz:** Inkompatible oder beschädigte Speicherstände werden verständlich abgewiesen; der vorherige gültige Stand bleibt erhalten.

## TEST-021 Bedienung und Plattformnachweis

- **Pakete:** P20. **Regeln:** CORE-009 bis CORE-012, ARCH-015, ARCH-016.
- **Gegeben:** Vereinbarte Zielplattform und Hardware sowie das in M0 festgehaltene Lastprofil.
- **Wenn:** Der exportierte Build gestartet und die relevanten Aktionen mit dem vereinbarten Eingabegerät durchgeführt werden.
- **Dann:** Auswahl, Bau, Befehle, Ansichtswechsel, Pause, Warnungen und Speichern funktionieren. Leistungswerte werden getrennt für Simulation, KI und Darstellung erfasst und gegen die zuvor vereinbarten Budgets geprüft.
- **Nachweis:** Plattform, Build, Gerät, Szenariogröße und Messmethode dokumentieren. Nicht ausgeführte Exporte bleiben ungeprüft; kein Ersatz durch einen schnellen leeren Editorbildschirm.

## TEST-022 Vollständige Partie mit mehreren Computerfraktionen

- **Pakete:** P21. **Regeln:** CORE-001 bis CORE-003, AI-010, DIP-006, WIN-005.
- **Gegeben:** F05 ohne kostenlose Verstärkungswellen oder versteckte KI-Ressourcen.
- **Wenn:** Ein regulärer Durchlauf bis zum Szenarioende und ein gezielter kooperativer Durchlauf gespielt werden.
- **Dann:** Beobachtbar sind Aufbau, Forschung, physischer Transport, Raumkampf, eine Bodeninvasion, vorbereitete Verteidigung, Anpassung der KI und ein erfülltes Szenarioziel. Im kooperativen Durchlauf setzt ein Partner eine konkrete Zusage um und gewinnt gemeinsam mit dem Spieler.
- **Zusatz:** KI-Fraktionen handeln auch untereinander. Ein Konvoiverlust erlaubt wirtschaftlichen Wiederaufbau, sofern die eigene Grundversorgung erhalten blieb. Subjektive Verständlichkeit und taktische Wahlmöglichkeiten werden separat protokolliert.

## Lokaler M5 Nachweis vom 24. November 2026

P11–P13 sind in Produktversion 1.0.5 implementiert und automatisiert geprüft. [M5_NACHWEIS](M5_NACHWEIS.md) ordnet 74 Core-Tests, erfolgreichen Windows-Build und 103 grafische Einzelprüfungen mit 14 Bilddateien den Transport-, Raumkampf-, Landungs-, Eroberungs- und Speicherregeln zu. Die vollständige manuelle Bedienungsabnahme und die verpflichtende GitHub-Unity-Prüfung bleiben offen. Auf Nutzeranweisung keine Screenreader-/Computer-Use-Tests und keine weiteren sichtbaren Tests auf dem laufenden Arbeitsdesktop. Die bisherige Grafikabnahme erfolgte vor dieser Einschränkung; Wiederholungen benötigen eine isolierte Sitzung.

## Ausgeführter M4 Nachweis vom 23. November 2026

P08–P10 sind in Version 1.0.3 abgeschlossen. [M4_NACHWEIS](M4_NACHWEIS.md) dokumentiert die 58 Core-Tests, erfolgreichen Windows-Build, 91 grafischen Einzelprüfungen, elf kontrollierten Renderbilder und nativen Eingabeschritte. TEST-003/004 prüfen konkurrierende Reservierungen, Teilmengen, volle Lager, Abbruch und Verlust; der P08/P09-Anteil von TEST-005 prüft bewegte Ziele und Hintergrundsimulation. Die regulär finanzierte Kette bis Außenposten, Kristallimport, Elektronik und Sensorschiff erweitert TEST-006. TEST-011 und TEST-020 erhalten Raumaufklärung und militärisch-wirtschaftliche Save/Load-Fortsetzung einschließlich V3-Migration. Reale Konvoikämpfe aus P11, KI und vollständige Partie bleiben offen.

## Ausgeführter M3 Nachweis vom 23. November 2026

P06/P07 sind in Version 1.0.2 geprüft. Der vollständige Aufbau, die realen Befehle, Testbestände, Ergebniszuordnung und Grenzen stehen in [M3_NACHWEIS](M3_NACHWEIS.md). 43 Core-Tests (15 neue M3-Fälle), Windows-Build und 71 grafische Einzelprüfungen sind bestanden; sieben Renderbilder wurden kontrolliert. Tatsächliche Maus-/Tastatureingaben bestätigen Gefecht auf zwei Wegen, Reserve/Rückzug, Auswahlrahmen und Gruppen sowie eine Stellung mit zwei Verteidigungslinien und unterbrochenem Nachschub. TEST-007 und der P06-Anteil von TEST-011 sind damit nachgewiesen; militärische Speicherung erweitert den aktuellen Umfang von TEST-020. Reguläre Träger und vollständige Invasionen bleiben den späteren Szenarien vorbehalten.

## Zuordnung zusätzlicher Fachtests

Die detaillierten Beispiele `AI-T01` bis `AI-T05`, `DIP-T01` bis `DIP-T05`, `WIN-T01` bis `WIN-T04` und `SYS-T01` in [AI_DIPLOMACY](AI_DIPLOMACY.md) ergänzen diese Szenarien. Sie dürfen dieselben ausführbaren Fixtures verwenden. Die Anforderungs-IDs bleiben die fachliche Referenz; Testnamen werden bei der Enginewahl auf das tatsächliche Testwerkzeug abgebildet.

## Ausgeführter M1/M2-Kernnachweis vom 23. November 2026

Produktversion **1.0.1**, Unity **6000.3.29f1**, Speicherformat **2**. `powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1` führt die unabhängigen C#-Verhaltenstests aus. Der Lauf in `TestResults/Core/results.txt` meldet **28 bestandene Tests**. Die Implementierung und ausführbaren Prüfungen stehen in `Assets/PlanetOne/Core` und `Tests/Core/CoreTests.cs`; das erzeugte Ergebnisprotokoll wird nicht als Quellcode eingecheckt.

| Paket / Szenariobezug | Tatsächlich geprüfter Teilumfang |
|---|---|
| P02–P03 / TEST-001, ARCH-001, ARCH-008–010 | Feste Schritte bei unterschiedlicher Bildratenaufteilung; Pause mit vorgemerkten Befehlen; gleicher Wirtschaftsablauf bei 1×, 2× und 4×; gemeinsame Hintergrundsimulation; Berechtigungen, Duplikatschutz und strukturierte Rückmeldungen; gespeicherte Fraktionen und Peace/War-Szenariobeziehung. |
| P04 / SURFACE-005–007 | Bauplatz und Erreichbarkeit, physische Materialanlieferung, konkurrierende Reservierungen und zwei priorisierte Bauplätze. Straßenunterbrechung hält geladene Fracht an; Reparatur und Umweg setzen sie ohne Positionssprung fort. Bauabbruch vor/nach Beladung und Abriss erhalten die ausgewiesenen Mengen. Vier Rückfrachten an eine ansonsten volle Quelle kommen wieder frei. |
| P05 / TEST-002 und Wirtschaftsanteil TEST-006, ECO-002–007 | Erz-/Metall-/Bauteilproduktion, Nutzung ungebundener lokaler Vorräte, Unterbrechung ohne doppelten Rezeptverbrauch, Energie-/Arbeitskräftemangel und Prioritäten, Nahrungsmangel und reguläre Erholung sowie der unten beschriebene Langlauf. |
| P03, P04, P05 / aktueller Teil von TEST-020, ARCH-014 | Save/Load mit laufender Fracht, vorgemerkter Straßenreparatur, Rückfracht und Reservierungen; identische Fortsetzung; Ablehnung ungültiger Zustände. Migration sowohl einer gezielten Format-1-Fixture als auch eines unveränderten Originalspielstands aus dem ersten Prototyp. |
| Vorgezogener P08-Anteil / SPACE-002 | Der Mond bleibt relativ zu seinem bewegten Bezugsplaneten; dieser Test ist kein Nachweis von Schiffen oder Kolonisierung. |

### Reproduzierbarer Wirtschaftslanglauf

`SustainableColony` verwendet Seed **2026** und die bestehende Karte mit zwei Kolonien und einem neutralen Mond. Aster startet in dieser ausdrücklich definierten Fixture mit **16 Bewohnern, 60 Nahrung, 100 Metall und 20 Bauteilen**; alle anderen Güter sind null. Der Standardstart besitzt 40 Bauteile, die Fixture reduziert diesen Vorrat vor Beginn, damit der Ausbau den Eigenproduktionsnachweis auch für Bauteile erbringen muss. Nach dem Start werden keine Bestände injiziert; interplanetare Importe existieren in diesem Umfang nicht.

1. Auf Aster werden Solaranlage `(10,5)`, Hydroponik `(9,7)`, Mine `(4,4)`, Schmelze `(6,5)`, Werkstatt `(11,5)` und Wohnhabitat `(7,7)` über normale Bauaufträge bestellt. Auf Nereid entstehen Hydroponik `(9,7)` und Solaranlage `(10,5)` aus dessen eigenen Startvorräten. Koordinaten sind nullbasiert; die Straßen stammen aus der regulären Startkarte.
2. Nach **180 Simulationssekunden Aufbau** müssen alle angeforderten Aster-Gebäude fertig sein. Anschließend laufen **24.000 weitere Schritte à 0,05 Sekunden**, also volle **20 Simulationsminuten**. Jeder Schritt verlangt `FoodShortageSeconds == 0` und Produktivität 1; alle 1.000 Schritte wird die Speicherzustandsintegrität geprüft.
3. Danach werden vier Lager auf `(12,5)` bis `(15,5)` gebaut und für ihre Fertigstellung weitere 90 Simulationssekunden eingeräumt. Die abgeschlossenen Gebäude kosten insgesamt **116 Metall und 26 Bauteile** und übersteigen beide Startvorräte. Für alle sechs Güter wird der tatsächliche Bestand einschließlich geladener Fracht gegen Startbestand plus Produktion/Rückgewinnung minus Verbrauch abgeglichen.

Der erfolgreiche Lauf endet mit **28 Bewohnern und 21 Arbeitskräften**, **360 erzeugter und 311 verbrauchter Nahrung** sowie vier fertigen Erweiterungslagern. Diese Endwerte umfassen Aufbau, 20-Minuten-Phase und anschließenden Ausbau. Anfangsnahrung wurde damit nachweislich durch eigene Produktion ersetzt. Endliche Erzvorkommen bleiben endlich; der Test behauptet keine zeitlich unbegrenzte Selbstversorgung.

Störungen werden getrennt in ausgewiesenen Fixtures geprüft: Nahrungsknappheit ohne kostenlose Erholungsrationen, Energie-/Arbeitskräftemangel, Straßensperre und Umweg, Bauabbruch, Abriss sowie Rückfracht bei vollem Quelllager. Diese Fixtures sind keine zusätzlichen Ressourcenquellen im normalen Spiel.

### Getrennter Build- und Grafiknachweis

`tools/Unity.ps1 -Action Build` hat den Windows-Player in Produktversion **1.0.1** erfolgreich mit Exitcode 0 erzeugt. `tools/Test-Prototype.ps1 -Visible` hat anschließend **58 programmatische Prüfbedingungen** bestanden und vier Renderbilder für Sonnensystem, Oberfläche, Gebäudedetails und wiederhergestellte Oberfläche gespeichert. Belegordner: `TestResults/Smoke/20261123-172324`. Alle vier Bilder wurden visuell geprüft; die Produktversion ist im Player lesbar.

**Die vollständige manuelle Mausbedienungsprüfung der erweiterten Wirtschaft ist weiterhin offen**; beim aktuellen Prüflauf stand der native Computer-Use-Zugriff nicht zur Verfügung. T-006 in [DECISIONS](DECISIONS.md) legt deshalb für M1/M2 die technische Abnahme über normale Controller-Bedienpfade im sichtbaren Player und visuelle Bildkontrolle fest. Damit wird kein physischer Maustest als bestanden behauptet; dieser bleibt eine zusätzliche Praxisprüfung spätestens für M8. Forschung, Gefechte, KI, Diplomatie und die vollständigen späteren Szenarien bleiben ungeprüft.

## Lokaler M7-Nachweis vom 24. November 2026

P16–P18 sind in Version 1.0.8 lokal implementiert. Tests/Core/DiplomacyTests.cs ergänzt 30 Verhaltensszenarien (insgesamt 126 Core-Tests) für TEST-014 bis TEST-019 und den M7-Anteil von TEST-020: Rechte/Zustimmungen/Krieg, physischer Handel und Verlust, Koalitionssicht, exklusive Beiträge, reale gemeinsame Invasion mit festgelegtem Eigentümer, Räumungsfrist, bezahlter Torbau, friedlicher/gemeinsamer/gleichzeitiger Sieg, Änderungen im letzten Tick, Wiederaufbau und einmaliges Ausscheiden. KI-KI-Verhandlung sowie tatsächliche Handels-/Eskortausführung und begründete Kündigung sind unabhängig von Spielerbefehlen geprüft. Der unveränderte V5-Spielstand und M7-Save/Load-Fortsetzung bleiben Regressionen. Windows-Build und 131 fensterlose Player-Prüfungen bestehen; 18 Weltbilder werden erzeugt. [M7_NACHWEIS](M7_NACHWEIS.md) nennt Bestände, Zeitfenster und Grenzen. IMGUI/manuelle Bedienung und M8-Gesamtszenario sind ausdrücklich nicht dadurch abgenommen.
