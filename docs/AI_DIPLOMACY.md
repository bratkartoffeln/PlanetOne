# PlanetOne KI Diplomatie und Siegbedingungen

Diese Spezifikation definiert eigenständig handelnde Computerfraktionen, verbindliche Zusammenarbeit und einen gemeinsamen Szenariosieg. Sie ergänzt die Wirtschafts-, Raum- und Bodenregeln in [GAME_DESIGN.md](GAME_DESIGN.md). Die Umsetzung verwendet Unity mit C#.

Stand 23. November 2026: Der erste geprüfte Aufbauprototyp besitzt eine gemeinsame Wirtschafts- und Weltensimulation, aber noch keine eigenständig handelnde KI, Diplomatie oder Szenariosieglösung. Die Anforderungen dieser Datei bleiben Zielverhalten für M6 und M7; ihre spätere Abnahme ist in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md) ausgewiesen.

## 1 Verbindlichkeit und Umfang

- Anforderungen mit `AI-`, `DIP-` und `WIN-` sind verbindliches Zielverhalten für die jeweils zugeordneten Entwicklungsphasen in [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md).
- Das erste vollständige Szenario enthält einen menschlichen Spieler und zwei Computerfraktionen. Die Datenstrukturen dürfen nicht auf genau einen Gegner zugeschnitten sein.
- Entscheidungen einer KI müssen aus ihrem Wissen, ihren verfügbaren Mitteln und ihren Verpflichtungen erklärbar sein. Eine bestimmte interne Algorithmenbibliothek ist nicht vorgeschrieben.
- Konkrete Planungsintervalle, Bewertungsschwellen, Vertragsfristen und Aktivierungszeiten sind konfigurierbare Balanceparameter. Diese Datei definiert ihre Bedeutung, keine endgültigen Zahlenwerte.
- Regeln gelten zwischen allen Fraktionen. KI gegen KI verwendet dieselben Verträge, Kampfregeln und Siegbedingungen wie KI gegen Spieler.

## 2 Wissen und wirtschaftliche Fairness

### AI-001 Gleiche Simulationsregeln

Computerfraktionen bezahlen Gebäude, Einheiten, Forschung, Reparaturen und Transporte aus ihren tatsächlichen Beständen. Sie besitzen eigene Produktionswarteschlangen und Frachter. Vernichtete Einheiten und Ladungen werden nicht automatisch ersetzt. Der normale Schwierigkeitsgrad gewährt keine verdeckten Ressourcen- oder Produktionsboni.

### AI-002 Begrenzter Informationszugang

Die KI entscheidet anhand eines fraktionsbezogenen Wissensstands. Er umfasst eigene Zustände, öffentliche Informationen, gültige Vertragsinformationen und tatsächlich aufgeklärte fremde Zustände. Die Sichtprüfung erfolgt vor der Übergabe an den Entscheider; ein nachträglicher Hinweis, geheime Werte nicht zu benutzen, reicht nicht.

Letzte Sichtungen speichern Beobachtungszeit und damaligen Zustand. Unbeobachtete Bewegung, Neubau und Vernichtung aktualisieren diese Sichtungen nicht. Vermutete Gegnerpositionen sind als Vermutung zu behandeln. Öffentlich angekündigte Toraktivierungen sind unabhängig von Sensoren bekannt.

### AI-003 Geteilte Aufklärung

Aktive Koalitionsmitglieder teilen ihre aktuelle Sensoraufklärung. Handel, Waffenstillstand, Nichtangriff und Transit allein gewähren keine vollständige Sichtfreigabe. Endet die Koalitionsmitgliedschaft, bleiben frühere Sichtungen als veraltetes Wissen erhalten; neue Sensordaten werden nicht mehr übertragen.

### AI-004 Reserven und Engpässe

Die KI unterscheidet fehlende Rohstoffe, fehlende Verarbeitungskapazität, fehlende Transportkapazität, blockierte Wege und fehlende Energie oder Arbeitskräfte. Sie behebt die erkannte Ursache. Bereits reservierte Güter und zugesagte Einheiten stehen anderen Plänen nicht erneut zur Verfügung.

Eine konfigurierbare Wiederaufbaureserve schützt die Herstellung einfacher Transporte und Grundverteidigung. Die KI darf diese Reserve in einer erklärten existenziellen Notlage einsetzen; der Entscheidungsgrund wird protokolliert.

## 3 Planung und militärische Anpassung

### AI-005 Entscheidungsebenen

| Ebene | Eingaben | Verbindliche Ausgabe |
|---|---|---|
| Wirtschaft | Lokale Bestände, Bedarf, Lieferungen, Kapazitäten | Priorisierte Produktion, Bauaufträge und Transportbedarf |
| Forschung | Bekannte Bedrohungen, Standorte, eigene Versorgung | Erreichbares Forschungsziel mit benötigtem Material |
| Strategie | Bekannte Kräfte, Interessen, Verträge, Szenarioziel | Wenige priorisierte Ziele und zugehörige Ressourcenbudgets |
| Operation | Ziel, verfügbare Kräfte, Transport- und Versorgungsplan | Zusammengestellter Verband mit Start- und Abbruchbedingungen |
| Taktik | Lokale Sicht, Gelände, Bewaffnung, Vorräte | Ausführbare Befehle für tatsächlich kontrollierte Einheiten |
| Diplomatie | Nutzen, Vertrauen, Bedrohung, Verpflichtungen | Angebot, Annahme, Ablehnung oder begründete Kündigung |

Eine übergeordnete Priorität kann einen untergeordneten Plan ändern. Reservierungen werden dabei ausdrücklich übertragen oder freigegeben. Die taktische Steuerung darf keine zusätzlichen Schiffe oder Truppen voraussetzen, die in der Operation nicht verfügbar sind.

### AI-006 Ziele mit Alternativen

Ein Ziel beschreibt ein Ergebnis, beispielsweise eine Kristallversorgung sichern. Die KI bewertet mindestens die tatsächlich verfügbaren Handlungsarten: eigene Produktion, Besiedlung, Handel, Schutzvertrag oder militärische Eroberung. Nicht jede Persönlichkeit bewertet sie gleich. Nicht erfüllbare Voraussetzungen machen einen Plan vorläufig ungeeignet.

### AI-007 Zustände einer Operation

`Proposed → Preparing → Ready → Executing → Completed`

Zusätzliche Übergänge führen aus aktiven Zuständen nach `Suspended`, `Aborting` oder `Failed`. Eine pausierte Operation kann nach erneuter Prüfung fortgesetzt werden. `Aborting` endet nach Rückzug oder Übergabe der Kräfte in `Cancelled` oder `Failed`.

Jede Operation enthält mindestens:

- Ziel und Zielort, verantwortliche Fraktion und vorgesehener Eigentümer eroberter Anlagen;
- zugewiesene Einheiten, Transportplätze und Materialreservierungen;
- Sammlungspunkt, vorgesehene Route und Nachschubquelle;
- Voraussetzungen für Beginn und Fortsetzung;
- bekannte Risiken, Rückzugsziel und Abbruchbedingungen;
- aktuellen Zustand sowie einen menschenlesbaren Grund der letzten Änderung.

Eine Operation beginnt erst, wenn ihre erforderlichen realen Kräfte, Transportplätze und Startvorräte vorhanden sind. Fehlen sie, bleibt sie in Vorbereitung oder wird verworfen. Ein einzelnes Schiff darf nicht gleichzeitig für zwei exklusive Operationen zugesagt sein.

### AI-008 Invasion und Tower Defense

Eine Invasion berücksichtigt die in [GAME_DESIGN.md](GAME_DESIGN.md) festgelegten Lande- und Versorgungsregeln. Die KI untersucht bekannte Landeflächen, Zugänge, Turmrollen, Reichweiten und Hindernisse. Sie besitzt mindestens folgende mögliche Reaktionen auf eine aufgeklärte starke Stellung:

- andere Landezone oder Angriffsrichtung auswählen;
- passende Infanterie, Artillerie oder Begleitschutz ergänzen;
- aufgeklärte Versorgungs- oder Energieanlagen angreifen;
- eine erreichbare Barriere durchbrechen;
- auf Verstärkung warten, blockieren oder die Operation abbrechen.

Sie muss nicht sämtliche Reaktionen in jeder Lage nutzen. Nach einem fehlgeschlagenen Angriff aufgrund einer bekannten unveränderten Verteidigung wird der bestehende Plan neu bewertet. Ein identischer erneuter Angriff erfordert eine begründete Änderung, etwa zusätzliche Kräfte oder eine neue Ablenkung.

### AI-009 Verteidigung und Rückzug

Die KI verteidigt wertvolle Anlagen mit zusammenwirkenden Türmen, erreichbarer Versorgung und mobilen Reserven. Sie darf eine Nebenstellung räumen, um Raumhafen oder Verwaltungszentrale zu sichern. Türme ersetzen keine mobile Reaktion auf andere Landezonen.

Beschädigte oder unzureichend versorgte Verbände prüfen Rückzug, Reparatur, Verstärkung und Kapitulation. Ein Rückzug benötigt einen erreichbaren Weg beziehungsweise verfügbare Transporter; er entfernt keine eingeschlossenen Einheiten aus der Simulation.

### AI-010 Eigene Interessen und Persönlichkeit

| Profil | Stärker gewichtete Interessen | Typischer Anlass zum Umdenken |
|---|---|---|
| Handelsorientiert | Sichere Routen, Vertragstreue, regelmäßige Lieferungen | Blockaden und nicht erfüllte Lieferzusagen |
| Expansionistisch | Neue Standorte, Ressourcenabdeckung, kurze Wege | Überdehnung und hohe Verteidigungskosten |
| Sicherheitsorientiert | Reserven, geschützte Grenzen, glaubwürdige Partner | Entstehung einer dominanten Nachbarfraktion |
| Technologieorientiert | Forschung, seltene Güter, spezialisierte Produktion | Fehlende Rohstoffe oder unzureichender Schutz |

Profile verändern Bewertungsgewichte; sie erzwingen weder ständigen Krieg noch bedingungslosen Frieden. Alle Profile können handeln, kämpfen, kooperieren und Verträge kündigen. Computerfraktionen verhandeln auch ohne Beteiligung des Spielers miteinander.

### AI-011 Schwierigkeitsgrade

Schwierigkeit verändert konfigurierbar Planungshorizont, Koordination, Reservehaltung und die Qualität der Alternativenbewertung. Grundlegende Regelverstöße, falsche Sicht und kostenlose Einheiten sind keine Schwierigkeitsmechanik. Spätere Ressourcenboni müssen separat ausgewiesen und optional sein.

### AI-012 Nachvollziehbarkeit und Persistenz

Für Entwicklung und Tests protokolliert die KI Entscheidung, verwendeten Wissensstand, wesentliche Alternativen und gewählte Aktion. Spielersichtbare Meldungen erklären Zusagen, Verzögerungen und Abbrüche, ohne geheime Informationen anderer Fraktionen offenzulegen.

Speichern und Laden erhält Wissen, Pläne, Reservierungen, Zusagen und Persönlichkeitsparameter. Das Laden darf keine Einheiten doppelt zuweisen, Vertragsfristen verlängern oder eine bereits begonnene Invasion neu erzeugen.

## 4 Verträge und Zugangsrechte

### DIP-001 Vertragsarten

| Art | Rechte und Pflichten | Nicht automatisch enthalten |
|---|---|---|
| Waffenstillstand | Gegenseitige Kämpfe für vereinbarte Mindestdauer beenden | Handel, Transit, gemeinsamer Sieg |
| Nichtangriffspakt | Gegenseitige Angriffe untersagen; geregelte Kündigung | Militärischer Zugang und Beistand |
| Handelsvertrag | Güter, Mengen, Gegenleistung und Lieferorte festlegen | Truppenlandung und freie Warenentnahme |
| Transit und Nutzung | Benannte zivile oder militärische Zugänge erlauben | Besiedlung und Eigentumsübertragung |
| Verteidigungspakt | Hilfe bei definierten äußeren Angriffen zusagen | Automatische Beteiligung an Angriffskriegen |
| Koalition | Nichtangriff, geteilte Aufklärung, gemeinsame Planung und Siegzuordnung | Zusammenlegung von Armeen und Lagerbeständen |

Verträge können nebeneinander bestehen. Jeder Vertrag benennt Beteiligte, Umfang, Beginn, Laufzeit beziehungsweise Kündigungsfrist und erforderliche Gegenleistungen. Militärische Landung, Stationierung, Hafenreparatur und Transit sind getrennt prüfbare Rechte.

### DIP-002 Vertragszustände

`Proposed → Accepted → Active → Terminating → Expired`

Alternativ enden Angebote in `Rejected` oder `Withdrawn`. Ein aktiver Vertrag kann durch einen bewussten Vertragsbruch nach `Breached` wechseln. Zustandswechsel besitzen einen Simulationszeitpunkt und einen Grund. Ein beendeter Vertrag wird nicht durch Laden oder eine wiederholte Nachricht reaktiviert.

Die Annahme prüft den aktuellen Vertragsstand aller Beteiligten. Das gemeinsame Beitreten verfeindeter Fraktionen erfordert vorher eine gültige Beendigung ihres gegenseitigen Kriegs. Bei konkurrierenden Angeboten wird der später verarbeitete Vorschlag erneut geprüft.

### DIP-003 Physischer Handel

Ein Handel benennt Quelle, Ziel, Gut, Menge, Gegenleistung und Lieferverantwortung. Waren werden reserviert und tatsächlich transportiert. Lieferungen werden nur in der tatsächlich angekommenen Menge gutgeschrieben; derselbe Frachtbestand kann nicht zugleich verkauft und für einen Bauauftrag ausgegeben werden.

Verlust, Verspätung und fehlender Lagerplatz erzeugen einen sichtbaren Vertragsstatus und neue Planung. Sie erklären den Nichterfüllungsgrund und können Vertrauen beeinflussen, lösen aber keinen automatischen Krieg aus. Eine Vertragskündigung dupliziert keine bereits unterwegs befindliche Ware; deren Eigentum und Rücklieferungsregel bleiben festgehalten.

### DIP-004 Zugang und Eigentum

Ein Handelsfrachter darf bei passendem Recht handeln, aber keine Kampftruppen ausladen. Besiedlung, militärische Landung und Stationierung prüfen ihre eigenen Rechte. Eigene und berechtigte verbündete Einheiten dürfen Tore passieren.

Bei friedlicher Bauzusammenarbeit errichtet und besitzt in der ersten vollständigen Version der Planeteneigentümer die dortigen regulären Koloniegebäude. Partner können Materialien liefern und erlaubte Truppen stationieren. Während einer feindlichen Invasion dürfen Angreifer nach den Feldbauregeln eigene Depots und Feldanlagen errichten; diese bleiben Eigentum ihrer Errichter. Gemeinsame Nutzung bedeutet keine automatische Steuerung fremder Einheiten oder freie Entnahme fremder Vorräte.

Eine feindliche Landung prüft einen gültigen Kriegszustand und die Invasionsregeln, nicht die Zustimmung des Verteidigers. Eine friedliche militärische Landung benötigt dagegen ausdrücklich passende Zugangsrechte. Gemeinsame Operationen können eigene Felddepots über vereinbarte Versorgung mit dem vorgesehenen neuen Planeteneigentümer teilen.

### DIP-005 Beistand und Kriegserklärung

Ein Verteidigungspakt nennt geschützte Fraktionen beziehungsweise Gebiete und die zugesagte Hilfe. Er greift bei einem äußeren Angriff im vereinbarten Umfang. Die Folgen eines selbst begonnenen Angriffskriegs lösen keine automatische Verteidigungspflicht aus.

Eine gemeinsame Offensive benötigt die ausdrückliche Zustimmung ihrer Teilnehmer. Eine Fraktion kann einen eigenen Krieg beginnen, zieht dadurch aber keine nicht zustimmenden Partner in den Kriegszustand. Bestehende Kriege und Beistandspflichten werden beim Koalitionsbeitritt angezeigt und geprüft.

### DIP-006 Gemeinsame Operationen

Angebote verwenden konkrete Ziele wie Konvoi eskortieren, Mond verteidigen, Brückenkopf versorgen oder Raumhafen erobern. Eine Zusage nennt Kräfte beziehungsweise Material, frühesten Bereitschaftszeitpunkt und Voraussetzungen. Die zugesagten Mittel werden reserviert.

Vor Beginn einer gemeinsamen Eroberung wird der künftige Planeteneigentümer vereinbart. Der letzte Treffer und die erste eintreffende Infanterie ändern diese Zuordnung nicht. Die tatsächlichen Eroberungsvoraussetzungen aus der Spielregel gelten weiterhin.

Ein Partner darf bei erheblicher Lageänderung umplanen. Er meldet Grund, betroffene Zusage und einen Ersatzvorschlag oder Rückzug. Die verbleibenden Teilnehmer prüfen daraufhin ihren Plan neu. Eine abgebrochene Zusage gibt ihre Reservierungen frei und kann Vertrauen mindern.

### DIP-007 Vertrauen und Angebotsbewertung

Beziehungen berücksichtigen nachweisbare Ereignisse: erfüllte Lieferungen, geleistete Hilfe, Vertragsbruch, eingehaltene Grenzen, konkurrierende Ansprüche und bekannte militärische Bedrohung. Ereignisse besitzen Urheber, Zeitpunkt und Betroffene. Mehrfach verarbeitete Meldungen zählen nicht mehrfach.

Die Oberfläche zeigt die wichtigsten Gründe einer Annahme oder Ablehnung, etwa zuverlässiger Handel, beanspruchter Mond oder bedrohte Grenze. Ein positiver Beziehungswert erzwingt kein wirtschaftlich oder militärisch unmögliches Angebot.

### DIP-008 Kündigung und Verrat

Ordentliche Kündigung beachtet die vereinbarte Frist. Ein Angriff auf geschützte Partner verlangt einen ausdrücklichen Vertragsbruchbefehl; ein gewöhnlicher Bewegungsbefehl oder automatisches Zielverhalten darf ihn nicht auslösen.

Vertragsbruch beendet betroffene Rechte, erzeugt Rufverlust und informiert die Betroffenen. Auch andere Fraktionen können den öffentlich bekannten Bruch bewerten. Sichtfreigaben enden zum wirksamen Austritt, nicht erst nach der Rückführung stationierter Truppen.

### DIP-009 Räumung stationierter Kräfte

Nach Ende von Stationierungsrechten beginnt eine angekündigte lokale Räumungsfrist. Dies gilt auch bei unangekündigtem Vertragsbruch. Betroffene Gasttruppen erhalten einen Evakuierungszustand: Abzug und Einladen sind erlaubt; Angriff, Besetzung, Neubau und weiteres Eindringen sind gesperrt.

Während dieser Frist dürfen Gastgeber die betroffenen abziehenden Kräfte ebenfalls nicht angreifen. Die Schutzwirkung gilt ausschließlich für die bisher berechtigt stationierten Kontingente und ihre örtliche Evakuierung, nicht für externe Angriffsflotten oder neue Landungen. Die Oberfläche zeigt Frist und betroffene Kräfte.

Nach Ablauf gelten die dann bestehenden Beziehungen und Kampfregeln. Einheiten verschwinden nicht automatisch. Räumungsdauer und zulässige Abzugswege müssen eine tatsächliche Evakuierung ermöglichen; Kartenszenarien und Tests prüfen dies ausdrücklich.

### DIP-010 Koalitionsverwaltung

Eine neue Mitgliedschaft benötigt die Zustimmung aller bestehenden Mitglieder sowie des Beitretenden. Mitglieder behalten eigene Bestände und Entscheidungen. Änderungen werden allen Mitgliedern und im Rahmen öffentlicher Siegankündigungen den übrigen Fraktionen gemeldet.

Die Koalition speichert Mitglieder, gemeinsame Operationen und vereinbarte Zugangsrechte. Ein gemeinsamer Sieg folgt ausschließlich den Regeln unter `WIN-001` bis `WIN-006`. Der Abschluss eines einzelnen Handelsvertrags macht eine Fraktion nicht zum Mitsieger.

### DIP-011 Grundbeziehung und Kriegszustand

Jedes Fraktionspaar besitzt unabhängig von seinen Verträgen eine symmetrische Grundbeziehung `Peace` oder `War`. Ohne andere Szenariovorgabe beginnt es in `Peace`; fehlende Verträge bedeuten keine automatische Feindschaft. Eine normale Angriffsauswahl gegen ein friedliches Ziel wird abgewiesen und erklärt die erforderliche Kriegserklärung.

`DeclareWar` ändert die Beziehung ausdrücklich nach `War`. Aktive Nichtangriffs-, Waffenstillstands- oder Koalitionspflichten verlangen vorher beziehungsweise atomar einen ausdrücklichen Vertragsbruch gemäß DIP-008. Automatische Waffenwahl kann keinen Krieg auslösen. Die Räumungsrechte aus DIP-009 bleiben auch im Krieg für die dort bezeichneten Kontingente erhalten.

Eine beiderseits angenommene Friedens- oder Waffenstillstandsvereinbarung setzt das Paar nach `Peace` und beendet gegenseitige Angriffsaufträge. Das Ablaufen einer Mindestdauer erzeugt keinen automatischen neuen Krieg; eine neue Kriegserklärung ist erforderlich. Eine Koalition darf keinen gegenseitigen Krieg ihrer Mitglieder enthalten. Konflikte mit außenstehenden Fraktionen werden paarweise behandelt und ziehen Partner nur nach deren tatsächlicher Zustimmung beziehungsweise Beistandspflicht hinein.

Freier Raum außerhalb ausgewiesener Kolonie-/Orbitalzugänge benötigt kein Transitabkommen. Geschützte Zugänge besitzen einen sichtbaren Geltungsbereich. Eine unerlaubte friedliche Annäherung erzeugt bei Entdeckung eine Warnung oder Zugangsablehnung, aber keine kostenlose Kenntnis unentdeckter Schiffe und keinen automatischen Krieg.

## 5 Szenarioziel und Ausscheiden

### WIN-001 Standardszenario Sprungtor

Das erste vollständige Szenario endet durch das Errichten und Aktivieren eines interstellaren Sprungtors. Dafür sind definierte Forschung, real angelieferte Komponenten, ein fertiggestellter Standort und eine abschließende Sicherungsphase nötig. Materialien stammen aus den bestehenden Güterketten; keine unsichtbaren Siegpunktlieferungen ersetzen den Transport.

Der Szenariodatensatz nennt erforderliche Technologien, Gütermengen, zulässigen Standort, Betriebsbedingungen und Aktivierungsdauer. Die konkreten Werte bleiben Balancingdaten. Alle Parteien können grundsätzlich dieselben Voraussetzungen erreichen.

### WIN-002 Betreiber und gemeinsame Beiträge

Eine einzelne Fraktion oder eine formelle Koalition betreibt das Projekt. Das Tor besitzt einen festgelegten baulichen Eigentümer. Diese Fraktion muss die Forschungsanforderungen erfüllen; Koalitionsmitgliedschaft überträgt keine Baupläne. Andere Mitglieder können tatsächliche Materiallieferungen, Transporte und Schutz beitragen. Mitgliedschaft allein stellt fehlendes Material nicht bereit.

### WIN-003 Sichtbare Aktivierung

Beim Aktivierungsbeginn prüft die Simulation Forschung, Bauzustand, gelieferte Güter, Kontrolle und Betriebsversorgung. Sie veröffentlicht Betreiber, beteiligte Fraktionen, Standort und verbleibende Sicherungszeit. Während der Sicherungsphase können Gegner das Projekt militärisch oder über dessen Versorgung stören.

Ein Verlust der erforderlichen Kontrolle oder Betriebsbedingungen bricht die Aktivierungsphase ab und setzt deren Fortschritt zurück. Vorhandene Gebäude und nicht verbrauchte Waren bleiben entsprechend ihrem tatsächlichen Zustand erhalten. Ein Neustart erfordert eine erneute vollständige Prüfung.

### WIN-004 Feste Teilnehmer während der Sicherungsphase

Beim Start wird die Teilnehmerliste der betreibenden Koalition gespeichert. Beitritt, Austritt, Ausscheiden eines Mitglieds oder Auflösung ändern diese Liste und brechen die laufende Aktivierung ab. Nach erneuter Prüfung kann eine neue Aktivierung mit aktualisierter Liste beginnen.

Die Oberfläche zeigt diese Folge vor einer geplanten Mitgliedschaftsänderung. Ein Beitritt in den letzten Sekunden verleiht keinen sofortigen Sieg. Eine ehemalige Mitgliedschaft verleiht ebenfalls keinen späteren Sieganspruch.

### WIN-005 Gemeinsamer Abschluss

Nach einer vollständig erfüllten Sicherungsphase gewinnen die gespeicherten und weiterhin gültigen Teilnehmer gemeinsam. Es folgt kein verpflichtender Krieg zwischen ihnen. Übrig gebliebene versteckte Aufklärer verhindern den Abschluss nicht.

Die Simulation verarbeitet Kontroll-, Versorgungs- und Mitgliedschaftsänderungen eines Zeitschritts vor dessen Abschlussprüfung. Ein im selben Schritt ungültig gewordenes Projekt kann deshalb keinen Sieg auslösen. Mehrere im selben Schritt gültig abgeschlossene unabhängige Projekte führen zu einem ausgewiesenen geteilten Szenarioerfolg.

### WIN-006 Friedlicher Abschluss

Alle verbleibenden Fraktionen dürfen sich zu einer Koalition zusammenschließen. Der Zusammenschluss allein beendet die Partie nicht: Das Projekt muss weiterhin errichtet, versorgt und aktiviert werden. Eine friedlich erfüllte Zielbedingung ist ein regulärer Sieg.

### WIN-007 Hauptstadtverlust und Ausscheiden

Der Verlust der Hauptstadt beendet eine Fraktion nicht automatisch. Bestehende Kolonien, Einheiten, Güter und Verträge bleiben bestehen, soweit ihre tatsächlichen Eigentums- und Vertragsregeln dies erlauben. Führung und Produktion können an verbliebenen Standorten wiederaufgebaut werden.

Eine Fraktion scheidet durch bestätigte Kapitulation aus oder wenn sie zugleich keine eigene Kolonie und keine tatsächlich einsatzfähige Neugründungsmöglichkeit mehr besitzt. Ein einsamer Aufklärer zählt nicht als Neugründungsmöglichkeit. Ein vorhandenes Kolonieschiff zählt nur mit erforderlicher Ausrüstung und einem nach den Szenarioregeln legal erreichbaren Gründungsstandort.

Die KI darf bei aussichtsloser Lage Kapitulation verhandeln. Gebiets- und Einheitentransfers müssen dabei ausdrücklich vereinbart werden. Ausscheiden erzeugt keine neuen Einheiten für den Sieger und klärt verbleibende Besitzstände gemäß den Kapitulationsbedingungen beziehungsweise Szenarioregeln.

### WIN-008 Abwicklung ausgeschiedener Fraktionen

Ausscheiden wird als einmaliger Zustandswechsel abgewickelt. Zuerst werden ausdrücklich vereinbarte Übergaben mit bestehenden IDs und tatsächlichen Beständen ausgeführt. Danach endet die Befehls- und Produktionsfähigkeit der Fraktion; ihre laufenden Aufträge und exklusiven Zusagen werden beendet und ungenutzte Reservierungen freigegeben. Dies erstattet keine bereits verbrauchten oder transportierten Waren.

Standard für verbleibenden Restbesitz: Nicht übertragene mobile Einheiten werden außer Dienst gestellt und aus der aktiven Simulation entfernt; ihre mitgeführte Ladung wird als ausgeschiedener Bestand beziehungsweise Verlust verbucht. Nicht übertragene Gebäude werden neutral und inaktiv, behalten ihre tatsächlichen Lagerbestände und können durch physische Sicherung und Reparatur wieder genutzt werden. Es entsteht keine neue kostenlose Einheit und kein fernübertragener Warenbestand.

Verträge und Koalitionsmitgliedschaft werden beendet; andere Teilnehmer erhalten einen nachvollziehbaren Grund und planen betroffene Lieferungen oder Operationen neu. Eine dadurch geänderte Siegergruppe unterbricht die Aktivierung nach WIN-004. Der vollständige Übergang wird vor weiterer Kampf-, Liefer- und Siegauswertung des ausgeschiedenen Akteurs wirksam.

## 6 Überprüfbare Abnahmeszenarien

Die Szenarien werden auf reproduzierbaren Testkarten mit konfigurierten Entscheidungsintervallen ausgeführt. Ein Test gibt der KI ausreichend simulierte Planungszeit; er prüft das Verhalten innerhalb eines angegebenen Zeitfensters statt an einem bestimmten Renderbild.

| Test | Aufbau und Handlung | Erwartetes Ergebnis | Anforderungen |
|---|---|---|---|
| AI-T01 | Gegner baut außerhalb sämtlicher KI-Sensoren einen Turm | KI-Wissen enthält ihn erst nach zulässiger Aufklärung; davor keine darauf gestützte Zielwahl | AI-002 |
| AI-T02 | Produktion reicht, Lieferkapazität unterschreitet den bekannten Verbrauch | KI behebt den Transportengpass oder reduziert Bedarf, statt den Engpass als fehlende Mine zu behandeln | AI-004 |
| AI-T03 | Geplante Invasion hat zu wenige Transportplätze | Keine Bodeneinheit erscheint kostenlos am Ziel; Vorbereitung wartet, wird angepasst oder abgebrochen | AI-007 |
| AI-T04 | Ein bekannter Turmriegel schlägt einen Angriff zurück; Alternative ist erreichbar | KI ändert Route, Kräfte, Ziel oder Zeitpunkt begründet; keine unbegrenzte identische Angriffsschleife | AI-008 |
| AI-T05 | Zwei Operationen fordern dasselbe Eskortschiff | Genau eine exklusive Zusage; die andere Operation plant ohne dieses Schiff neu | AI-004, AI-007 |
| DIP-T01 | Handelsfrachter mit Truppenladung versucht ohne Landerecht zu entladen | Handel bleibt erlaubt, Truppenlandung wird mit nachvollziehbarem Grund abgewiesen | DIP-001, DIP-004 |
| DIP-T02 | Ein Mitglied beginnt einen eigenen Angriffskrieg | Nicht zustimmende Partner erhalten keinen automatischen Kriegszustand | DIP-005 |
| DIP-T03 | Zwei Verbündete erobern vereinbarungsgemäß gemeinsam einen Planeten | Festgelegter Eigentümer erhält die Kontrolle nach realer Sicherung; letzter Treffer ist unerheblich | DIP-006 |
| DIP-T04 | Stationierungsvertrag endet mit Truppen in fremder Basis | Sichtbare Räumungsfrist, keine sofortige Innenattacke, tatsächlicher Abzug statt Löschung | DIP-008, DIP-009 |
| DIP-T05 | Alle Beteiligten sind Computerfraktionen | Handel, Verhandlungen, gemeinsame Operation und Kündigung funktionieren ohne Spielerbefehle | AI-010, DIP-001 |
| WIN-T01 | Koalition liefert alle Komponenten und erfüllt die Sicherungsphase | Alle gültigen Teilnehmer gewinnen gemeinsam | WIN-001 bis WIN-005 |
| WIN-T02 | Eine Fraktion tritt kurz vor Aktivierungsende bei | Laufende Phase bricht ab; kein sofortiger Sieg; neuer Start verwendet neue Teilnehmerliste | WIN-004 |
| WIN-T03 | Hauptstadt fällt, zweite eigene Kolonie bleibt erhalten | Fraktion bleibt handlungsfähig und kann weiter teilnehmen | WIN-007 |
| WIN-T04 | Aktivierung endet, feindlicher Aufklärer bleibt verborgen | Szenario endet regulär; keine Jagd auf letzte unbeteiligte Einheit nötig | WIN-005 |
| SYS-T01 | Während einer gemeinsamen Invasion speichern und laden | Wissen, Verpflichtungen, Reservierungen, Fracht und Vertragsfristen bleiben erhalten | AI-012, DIP-002 |

Technische Modulgrenzen, Serialisierung und Testausführung werden in [TECHNICAL_ARCHITECTURE.md](TECHNICAL_ARCHITECTURE.md) festgelegt. Die Abnahme erfolgt pro Entwicklungsphase; noch nicht implementierte Anforderungen werden nicht als erfüllt markiert.
