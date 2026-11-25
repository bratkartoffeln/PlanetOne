# PlanetOne Gesamtes Spielkonzept

Stand: 23. November 2026. PlanetOne verbindet planetaren Aufbau, physische Warenlogistik, Raumstrategie, steuerbare Bodeninvasionen, Tower Defense und Diplomatie in einer gemeinsamen Echtzeitsimulation.

Diese Übersicht rekonstruiert den im Projekt erhaltenen Konzeptstand; sie ist kein wörtliches Transkript des früheren Chats. Verbindliche Detailregeln und stabile Anforderungs-IDs stehen in [GAME_DESIGN](GAME_DESIGN.md), [AI_DIPLOMACY](AI_DIPLOMACY.md) und [TECHNICAL_ARCHITECTURE](TECHNICAL_ARCHITECTURE.md). Vorgaben und Arbeitsannahmen unterscheidet [DECISIONS](DECISIONS.md). Die Übersicht ändert diese Regeln nicht. Das hier beschriebene Gesamtspiel ist das Entwicklungsziel; der bereits umgesetzte Stand ist am Ende separat aufgeführt.

Die Oberflächen verwenden seit Version 1.0.6 ein Hexraster aus sechseckigen Waben: sechs Nachbarn für Bauanschluss, Straßen und Bewegung, geometrisch passende Sicht und Reichweiten sowie Landezonen aus sieben Waben. Spielstandübernahme und Nachweise: [HEXRASTER_NACHWEIS](HEXRASTER_NACHWEIS.md).

Seit Version 1.0.7 ist M6 lokal umgesetzt und automatisiert geprüft: eigenständige Wirtschaft, Forschung, Erkundung, Kolonisierung, Operationsplanung und Rückzug über reguläre Befehle mit begrenztem Wissen. Einstieg über **M6 Gegen Computer spielen**; Nachweise, separate taktische Fixture und noch offene manuelle/Git-Abnahme: [M6_NACHWEIS](M6_NACHWEIS.md). Seit 1.0.8 ist auch M7 lokal implementiert und automatisiert geprüft: Verträge, physischer Handel, Koalitionssicht, gemeinsame Operationen, geschützter Abzug, Kapitulation und Sprungtorabschluss. Einstieg über **M7 Diplomatische Partie** beziehungsweise **Diplomatie und Sprungtor**. [M7_NACHWEIS](M7_NACHWEIS.md) trennt regulären Start, vorbereitete Fixtures und offene manuelle/CI-Abnahme.

## 1 Spielidee und Umfang

Der Spieler führt eine Weltraumfraktion: erkunden, besiedeln, produzieren, transportieren, erforschen, verteidigen und expandieren. Wirtschaft, Flotten, Bodenarmeen und diplomatische Beziehungen beeinflussen sich unmittelbar. Auf Planeten entstehen Bevölkerung, Waren, Forschung und Truppen. Flotten sichern Expansion und Versorgung. Bodenarmeen kämpfen auf den tatsächlich aufgebauten Kolonieflächen.

Einzelspieler gegen eine oder mehrere eigenständige Computerfraktionen ist der erste Umfang. Als Arbeitsannahme für das erste vollständige Referenzszenario gelten ein Sternsystem, drei Startwelten, zwei erschließbare beziehungsweise umkämpfte Monde, ein Spieler und zwei KI-Fraktionen. Das langfristige Weltmodell kann mehrere Sterne und Systeme aufnehmen; deren spielbare Umsetzung folgt später.

Grundlage: CORE-001 bis CORE-006, D-001 bis D-015.

## 2 Gemeinsame Spielwelt und Bedienung

Sonnensystem und Planetenoberfläche sind Ansichten derselben Simulation. Auch außerhalb der Kamera laufen Produktion, Transporte, Gefechte und KI weiter. Ein Ansichtswechsel vervielfacht keine Bestände und pausiert die Welt nicht.

Taktische Pause erlaubt Auswahl, Kamera, Bauplanung und Befehle. Die Spielzeit mit Bewegung, Produktion, Forschung, Schaden und Vertragsfristen steht dabei still. Vorgemerkte Aufträge werden beim Fortsetzen regulär geprüft. Auswahlrahmen, Mehrfachauswahl, Kontrollgruppen und sichtbare Befehlsziele unterstützen die militärische Steuerung.

Eine Übersicht erschließt Kolonien, Flotten, gefährdete Transporte und gemeinsame Operationen. Warnungen unterscheiden Invasionsvorbereitung, Abstieg, Versorgungsausfall, durchbrochene Verteidigung und bedrohte Verwaltung. Sie zeigen nur bekanntes Wissen und führen zum betroffenen Ort. Patrouille, Eskorte, Stellung halten, Ziel schützen und geordneter Rückzug funktionieren auch ohne geöffnete Ortsansicht.

Grundlage: CORE-007 bis CORE-012.

## 3 Sonnensystem und Aufklärung

Planeten bewegen sich auf berechenbaren Bahnen um ihren Stern; Monde um ihren Planeten. Schiffe sind deutlich schneller und fliegen frei auf einer gemeinsamen zweidimensionalen Spielebene. Die Darstellung darf schräg beziehungsweise in 2,5D erfolgen. Optische Höhe erzeugt keine zusätzlichen Kampfregeln.

Flugaufträge berücksichtigen bewegte Ziele. Eskorten und Verteidiger folgen ihrem Bezugsobjekt; Orbitalanlagen bleiben an ihren Himmelskörper gebunden. Umlaufbahnen dienen der Orientierung. Sternnähe, Asteroidenfelder oder Sensornebel können angezeigte Auswirkungen auf Bewegung, Sicht oder Risiko haben.

Gebiete sind unbekannt, erkundet oder aktuell beobachtet. Letzte Sichtungen bleiben zeitlich veraltetes Wissen. Unbeobachtete gegnerische Neubauten, Bewegungen oder Verluste werden nicht heimlich nachgetragen. Welttypen unterscheiden sich durch tatsächliche Vorkommen, Baufläche und Produktionsbedingungen. Jede reguläre Startwelt ermöglicht lokale Grundversorgung; seltene Güter können Expansion oder Handel erfordern.

Grundlage: SPACE-001 bis SPACE-007, AI-002.

## 4 Kolonisierung und Aufbau

Ein Raumhafen baut Frachter und Kolonisationsschiffe aus lokalen Gütern. Eine zugehörige Orbitalwerft baut freigeschaltete Kriegsschiffe mit real übertragenem Material, Energie und Produktionszeit.

Ein Kolonisationsschiff gründet auf einem geeigneten unbesiedelten Himmelskörper. Es wird genau einmal in deklarierte Startinfrastruktur umgewandelt; mitgeführte Güter und Besatzung bilden den Anfangsbestand. Wiederholte oder fehlgeschlagene Gründungsbefehle erzeugen keine Kopien. Fremde Kolonien erfordern Eroberung oder ausdrücklich vereinbarte Eigentumsübertragung.

Jeder bebaubare Himmelskörper besitzt zunächst eine zusammenhängende Kolonieregion. Aufbau und Kampf verwenden dieselben Gebäude, Straßen und Lager. Gelände beeinflusst Zugänglichkeit, Sicht und Deckung; mehrere glaubwürdige Angriffswege bleiben möglich.

Die Gebäude umfassen Verwaltung mit Startlager, Wohnraum, Energie, Nahrung, Minen, Metallverarbeitung, Bauteilproduktion, Lager und Raumhafen. Forschung, Elektronik, Kaserne, Fahrzeugwerk und Militärdepot erweitern die Kolonie.

Straßen und automatische Fahrzeuge verbinden die Wirtschaft. Baustellen erhalten reservierte Güter durch reale Lieferungen und benötigen Bauzeit und Baukapazität. Energie und Arbeitskräfte werden innerhalb der angeschlossenen Kolonie verteilt. Betriebsprioritäten, Abschaltung und begrenzte Energiepuffer steuern Engpässe. Bevölkerung benötigt Nahrung und Wohnraum. Nahrungsmangel stoppt Wachstum und senkt Produktivität; eine begrenzte Wiederaufbaufähigkeit bleibt erhalten.

Gebäude und Baustellen sind beschädigbar. Abbruch, Rückbau und Zerstörung besitzen getrennte Rückgewinnungsregeln. Geborgenes Material muss tatsächlich abgeholt werden und darf nie den eingebrachten Bestand überschreiten.

Grundlage: SPACE-008 bis SPACE-009, SURFACE-001 bis SURFACE-009, ECO-007.

## 5 Wirtschaft und Produktionsketten

Sechs interplanetar transportierbare Güterklassen bilden die Arbeitsannahme für die Grundwirtschaft:

| Gut | Herkunft | Verwendung |
|---|---|---|
| Nahrung | Landwirtschaft oder Bioreaktoren | Bevölkerung und militärische Versorgung |
| Erz | Endliche Lagerstätten und Minen | Metallproduktion |
| Metall | Erzverarbeitung | Gebäude, Bauteile und Elektronik |
| Bauteile | Metallverarbeitung | Bau, Schiffe, Fahrzeuge, Reparatur und Nachschub |
| Kristalle | Seltene Lagerstätten | Elektronik und bestimmte Forschung |
| Elektronik | Metall und Kristalle | Fortgeschrittene Anlagen, Einheiten und Forschung |

Jede Ware besitzt genau einen tatsächlichen Lager- oder Transportort. Reichssummen dienen nur der Übersicht. Energie, Bevölkerung, Arbeitskräfte, Lagerplatz und Munition sind lokale Zustände. Abgeschlossene Forschung und eigene Baupläne gelten reichsweit.

Produktionszyklen starten nur mit Eingaben und vorgesehenem Ausgaberaum. Material wird einmal entnommen; Energie- oder Arbeitskräftemangel pausiert den Fortschritt ohne erneuten Verbrauch. Fertige Produkte entstehen genau einmal. Laufende Produktion kann ihren bereits verbrauchten Input nicht zugleich exportieren.

Grundversorgung, einfache Transporte, Kolonisierung, Infanterie, Pioniere, Grundverteidigung und einfache Belagerung bleiben ohne Kristalle und Elektronik erreichbar. Normaler Schiffsflug verbraucht im Grundspiel keinen Treibstoff. Kredite, Warenbörsen und zusätzliche Spezialrohstoffe sind für die erste Wirtschaftsschleife nicht erforderlich.

Grundlage: ECO-001 bis ECO-008.

## 6 Logistik und Nachschub

Raumhäfen verbinden Oberfläche und Orbit über Transfers mit begrenzter Kapazität und Dauer. Beide Seiten bilden kein doppeltes Warenkonto. Feste Routen und ein automatisch versorgtes Kolonienetz verwenden Quelle, Ziel, Menge, Priorität und Sicherheitsregeln.

Mindestbestand, Zielbestand und Exportreserve regeln den Bedarf. Reservierte und bereits unterwegs befindliche Mengen zählen mit. Ein Auftrag reserviert Quellware, Frachtraum und Zielplatz genau einmal. Grundversorgung hat standardmäßig Vorrang vor Produktion, freigegebenem Bau und Vorratshaltung; Prioritäten bleiben einstellbar. Ladungsbündelung, Rückfracht und getrennte Nachfüllschwellen vermeiden unnötige Kleinstfahrten.

Bei Gefahr können Frachter Umwege nutzen, auf Eskorte warten oder ausdrücklich riskant durchbrechen. Sie berücksichtigen ausschließlich bekannte Gefahren. Eine Blockade wirkt durch bedrohte oder unterbrochene Transporte. Hafenverlust oder geänderte Zugangsrechte führen zu Warten, Rückfahrt oder zulässigem Ausweichziel. Geladene Ware bleibt beim tatsächlichen Träger.

Militärdepots verarbeiten Nahrung und Bauteile für Versorgung, Reparatur und örtliche Munition. Munition ist keine siebte Handelsware. Fahrzeuge versorgen Felddepots und Verbände über erreichbare Wege. Einheiten führen begrenzte Vorräte. Ohne Nachschub fallen Nachfüllen und Reparatur aus; Waffen ohne erforderliche Munition feuern nicht. Bewegung und Evakuierung bleiben möglich.

Zerstörte Transporter verlieren tatsächliche Ladung und noch eingeschiffte Truppen. Freigegebene Reservierungen stellen diese Verluste nicht wieder her. Eine spätere Bergungsfunktion ist noch kein Bestandteil der ersten Fassung.

Grundlage: LOG-001 bis LOG-013.

## 7 Forschung

Die fünf Forschungsrichtungen sind Industrie, Logistik, Raumfahrt und Flotten, Bodentruppen sowie Festungsbau. Ein gerichteter, zirkelfreier Technologiebaum kann Voraussetzungen zwischen diesen Richtungen verbinden. Sichtbare gesperrte Technologien erklären fehlende Voraussetzungen; Entdeckungen können weitere Technologien offenlegen.

Forschung benötigt einen Standort, Laborleistung, Energie, Arbeitskräfte und gegebenenfalls tatsächlich angeliefertes Versuchsmaterial. Laborverlust pausiert das Projekt, erhält aber den Fortschritt. Abgeschlossene Technologien bleiben bekannt. Eine Freischaltung liefert Baupläne; Einheiten und Gebäude müssen weiterhin produziert werden.

Eine mögliche Fortschrittskette lautet: Metall und Bauteile, Koloniemodule, Kristallwelt erschließen, Elektronik herstellen, fortgeschrittene Sensoren und spezialisierte Verteidigung entwickeln. Grundlegende Gegenwehr und Transportfähigkeit dürfen nicht hinter unerreichbaren Voraussetzungen liegen.

Grundlage: RES-001 bis RES-007.

## 8 Flotten und Raumkampf

| Rolle | Hauptaufgabe |
|---|---|
| Aufklärer | Sicht und Früherkennung bei geringer Kampfkraft |
| Korvette | Verfolgung und Überfälle auf leichte Ziele |
| Eskortfregatte | Schutz von Transporten und Positionen |
| Torpedoschiff | Angriff auf schwere Schiffe und Stationen, benötigt Begleitschutz |
| Frachter | Gütertransport mit begrenztem Frachtraum |
| Landungstransporter | Tatsächliche Truppen und Startvorräte transportieren |
| Kolonisationsschiff | Neue Kolonie aus mitgeführter Ausrüstung gründen |

Gefechte verwenden reale Positionen, Eigentümer, Sicht, Angriffsrechte und Betriebsbedingungen. Schaden bleibt erhalten; Reparatur kostet Zeit und Bauteile. Rückzug benötigt einen erreichbaren Weg und beseitigt weder Verfolger noch Gefahren.

Orbitalbeschuss erfordert ein geeignetes Schiff, aufgeklärte Ziele und einen angekündigten Feuerauftrag. Das Schiff bindet sich an eine angreifbare Position; Feuerhäufigkeit und Verbrauch sind begrenzt. Schilde, geeignete Flugkörperabwehr, planetare Geschütze und Entsatzflotten ermöglichen Gegenwehr. Beschuss beschädigt oder zerstört, überträgt aber keinen Planetenbesitz.

Grundlage: COMBAT-001 bis COMBAT-002, COMBAT-007 bis COMBAT-009.

## 9 Bodentruppen und Gefechte

| Rolle | Hauptaufgabe |
|---|---|
| Linieninfanterie | Aufklärung, Deckung und Gebäudebesetzung |
| Panzerabwehrtrupp | Schwere Fahrzeuge bekämpfen |
| Kampfpanzer | Beweglicher Durchbruch und direktes Feuer |
| Mobile Artillerie | Befestigte Stellungen mit Aufklärung und Munition bekämpfen |
| Mobile Flugabwehr | Vorstöße und Landezonen schützen |
| Pionier- und Versorgungsfahrzeug | Feldbau, Reparatur und Nachschub |

Kasernen und Fahrzeugwerke benötigen Material, Zeit und lokale Rekrutierungskapazität. Truppen im Transporter sind weiterhin dieselben Einheiten und nicht gleichzeitig am Boden verfügbar.

Direktfeuer erfordert freie Schusslinie. Artillerie kann geeignete Hindernisse überwinden, braucht aber eine aufgeklärte Zielposition. Beschuss einer letzten bekannten Position liefert keine heimliche Sicht auf bewegte Gegner. Infanterie nutzt Deckung und geeignete Gebäude; Fahrzeuge benötigen befahrbare Zugänge.

Grundlage: COMBAT-003 bis COMBAT-007.

## 10 Invasion und Eroberung

Eine Invasion durchläuft tatsächliche Phasen: Aufklärung, Sammlung, Anflug, Abstieg, Brückenkopf, Bodenoperation und Sicherung. Abbruch und Evakuierung bleiben möglich. Der Spieler steuert die Gefechte selbst.

Örtliche Raumkontrolle erleichtert den Anflug; die Vernichtung aller Feindschiffe ist keine Landebedingung. Eine Landezone muss groß genug, zugänglich und erreichbar sein. Gebäude, Gelände und lokale Schutzanlagen begrenzen sie. Aktive Flächenschilde verhindern direkte Landung innerhalb ihres Bereichs, erlauben aber einen Bodenangriff von außen.

Transporter sind im Raum, im sichtbaren angreifbaren Übergang oder an der Landezone. Truppen werden erst nach tatsächlichem Entladen aktiv. Alle Wellen stammen aus produzierten und transportierten Verbänden. Ein Brückenkopf braucht Landefläche, Pioniere oder Depotmodul und reale Vorräte. Nach Verlust des Orbits bleiben gelandete Truppen vor Ort.

Geeignete Infanterie muss die Verwaltungszentrale physisch besetzen und sichern. Strategischer Besitz wechselt erst mit gesicherter Verwaltung und erreichbarem Versorgungspunkt, also Landekopf oder nutzbarem erobertem Raumhafen. Gegner im Sicherungsbereich unterbrechen die Sicherung. Versteckte Resttruppen außerhalb dieses Bereichs verhindern den Wechsel nicht und verschwinden auch nicht.

Zivile Anlagen gesicherter Bezirke können übernommen werden; militärische Anlagen benötigen eigene Ausschaltung oder Besetzung. Lager behalten reale Bestände. Zerstörte Kontrollanlagen hinterlassen reparierbare Standorte. Bei gemeinsamer Eroberung gilt der zuvor vereinbarte neue Eigentümer. Eine Evakuierung braucht erreichbare Abholung und freie Transportplätze.

Grundlage: INV-001 bis INV-011.

## 11 Tower Defense

Verteidigung entsteht aus vorbereiteten Feuerzonen, Engstellen, Mauern, Toren, Versorgung und mobilen Reserven. Angreifer wählen alternative Wege und Landezonen. Mauern und Tore sind zerstörbar; geschlossene Ringe bleiben überwindbar.

| Anlage | Aufgabe und Gegenwehr |
|---|---|
| Schnellfeuerturm | Gegen leichte Ziele; durch Panzerung und Reichweite zu bekämpfen |
| Panzerabwehrstellung | Gegen schwere Fahrzeuge; anfällig für Infanterie, Artillerie und Überlastung |
| Flugabwehrstellung | Gegen Landungstransporter und geeignete Flugkörper; durch Bodenangriff oder andere Anflugwege zu umgehen |
| Artilleriestellung | Gegen entfernte oder konzentrierte Bodenziele; anfällig für Gegenaufklärung und Durchbruch |
| Schildgenerator | Lokaler Schutz gegen Orbitalfeuer; durch Energieausfall oder Bodenangriff auszuschalten |
| Planetare Abwehrbatterie | Gegen Schiffe im markierten nahen Orbitalbereich |

Gewöhnliche Bodenflugabwehr trifft Transporter in definierten Übergangsphasen und keine beliebigen Schiffe auf der Systemkarte. Türme benötigen Baukosten, Fläche und ihre ausgewiesene Energie-, Personal- und Munitionsversorgung. Diese Bedingungen begrenzen Festungen; ein pauschales Turmlimit ist nicht erforderlich.

Depots und Speicher puffern Ausfälle. Feldbau benötigt versorgte Bauquellen oder ausgerüstete Pioniere, Material und Zeit. Verteidigungsgruppen halten Zugänge, schützen Depots oder weichen auf eine vorgegebene Linie zurück. Mobile Reserven handeln innerhalb ihrer Aufträge.

Grundlage: DEF-001 bis DEF-008.

## 12 Eigenständige Computerfraktionen

KI-Fraktionen wirtschaften nach denselben Regeln wie der Spieler: eigene Güter, Warteschlangen, Frachter, Forschung und Rekrutierung. Im normalen Schwierigkeitsgrad gibt es keine verdeckten Ressourcenboni oder kostenlos ersetzten Truppen. Ihr Entscheider erhält ausschließlich eigenes Wissen, öffentliche Informationen, Vertragsinformationen und legitime Aufklärung.

Wirtschaftsplanung unterscheidet Rohstoff-, Verarbeitungs-, Transport-, Wege-, Energie- und Arbeitskräftemangel. Reserven schützen Wiederaufbau und Grundverteidigung. Strategie, Forschung, Operation, Taktik und Diplomatie setzen tatsächlich ausführbare Ziele. Für Rohstoffzugang kommen eigene Produktion, Besiedlung, Handel, Schutzverträge oder Eroberung infrage.

Operationen speichern Kräfte, Transportplätze, Vorräte, Sammelpunkt, Route, Nachschub, Risiken, Startbedingungen und Rückzugsplan. Sie werden vorbereitet, ausgeführt, abgeschlossen, ausgesetzt oder begründet abgebrochen. Dieselbe Einheit kann nicht gleichzeitig exklusiv mehreren Operationen zugesagt werden.

Gegen aufgeklärte starke Stellungen kann die KI Landezone oder Richtung wechseln, Artillerie und Begleitschutz ergänzen, Energie und Versorgung angreifen, Barrieren durchbrechen, Verstärkung abwarten oder abbrechen. Nach einem gescheiterten Angriff bewertet sie ihren Plan neu. Verteidigung verbindet Türme und Reserven; beschädigte oder schlecht versorgte Verbände prüfen Reparatur, Rückzug und gegebenenfalls Kapitulation.

Handelsorientierte, expansionistische, sicherheitsorientierte und technologieorientierte Profile gewichten Interessen unterschiedlich. Alle können handeln, kämpfen und kooperieren, auch untereinander. Schwierigkeit verändert Planung und Koordination. Entscheidungen und Zusagen bleiben erklärbar; Speichern erhält Wissen, Pläne und Reservierungen.

Grundlage: AI-001 bis AI-012.

## 13 Diplomatie und Kooperation

Jedes Fraktionspaar besitzt Frieden oder Krieg. Fehlende Verträge bedeuten keine Feindschaft. Kriegserklärung und Vertragsbruch sind bewusste Befehle; automatische Waffenwahl löst keinen Krieg aus. Ein Friedensschluss beendet gegenseitige Angriffe. Das Ende einer Mindestfriedensdauer beginnt keinen neuen Krieg.

Vertragsarten sind Waffenstillstand, Nichtangriff, Handel, Transit und Nutzung, Verteidigungspakt sowie Koalition. Sie benennen Parteien, Rechte, Pflichten, Orte, Fristen und Gegenleistungen. Angebote können angenommen, abgelehnt oder zurückgezogen werden; aktive Verträge auslaufen, gekündigt oder bewusst gebrochen werden.

Handel transportiert tatsächlich reservierte Waren. Verluste, Verspätung oder Platzmangel verändern den Vertragsstatus, beginnen aber keinen automatischen Krieg. Militärische Landung, Stationierung, Reparatur und Transit sind getrennte Rechte. Handel erlaubt keine beliebige Truppenlandung oder Entnahme fremder Bestände. Feindliche Landung benötigt Kriegszustand und erfüllte Invasionsbedingungen.

Ein Verteidigungspakt gilt für vereinbarte äußere Angriffe, nicht automatisch für selbst begonnene Angriffskriege. Gemeinsame Offensiven benötigen Zustimmung. Konkrete Zusagen betreffen Kräfte, Material, Bereitschaft und Voraussetzungen. Der spätere Eigentümer einer Eroberung wird vorab vereinbart. Bei geänderter Lage melden Partner Verzögerung, Ersatzvorschlag oder Rückzug.

Koalitionen teilen aktuelle Aufklärung und können gemeinsam siegen; Bestände, Baupläne und Armeen bleiben eigenständig. Neue Mitglieder benötigen die Zustimmung aller. Vertrauen beruht auf tatsächlichen Lieferungen, Hilfe, Grenzachtung, Konflikten und Vertragsbrüchen. Annahmen und Ablehnungen erklären ihre wichtigsten Gründe.

Nach Ende von Stationierungsrechten gilt eine sichtbare Räumungsfrist, auch bei Vertragsbruch. Bisher berechtigte Gasttruppen dürfen abziehen und einladen, aber nicht aus dem Inneren der Basis angreifen, besetzen oder neu bauen. Der Gastgeber darf diese abziehenden Kontingente während der Frist ebenfalls nicht angreifen. Externe Angriffsflotten erhalten diesen Schutz nicht. Nach Fristablauf gelten die aktuellen Beziehungen; niemand verschwindet automatisch.

Grundlage: DIP-001 bis DIP-011.

## 14 Szenarioziel und Niederlage

Als Arbeitsannahme für das erste Szenario dient ein interstellares Sprungtor. Eine Fraktion oder formelle Koalition muss erforderliche Forschung, reale Komponentenlieferungen, Bau und abschließende Aktivierung leisten. Der bauliche Eigentümer benötigt die Technologien; Partner können Material, Transporte und Schutz beitragen.

Aktivierungsbeginn veröffentlicht Betreiber, Teilnehmer, Standort und verbleibende Sicherungszeit. Gegner können Kontrolle oder Versorgung unterbrechen. Der Verlust notwendiger Betriebsbedingungen setzt die Aktivierungsphase zurück. Ebenso erzwingen Beitritt, Austritt oder Ausscheiden eines Koalitionsmitglieds eine neue Prüfung und einen Neustart mit aktualisierter Teilnehmerliste.

Nach erfolgreicher Sicherungsphase gewinnen alle gültigen festgehaltenen Teilnehmer gemeinsam. Es folgt kein Pflichtkrieg unter Verbündeten. Versteckte Restaufklärer verhindern den Sieg nicht. Alle verbliebenen Fraktionen können friedlich kooperieren, müssen das Tor aber trotzdem vollständig errichten und aktivieren. Gleichzeitige gültige unabhängige Abschlüsse ergeben einen geteilten Szenarioerfolg.

Hauptstadtverlust allein beendet eine Fraktion nicht. Sie scheidet durch bestätigte Kapitulation aus oder wenn sie weder Kolonie noch tatsächlich nutzbare Neugründungsmöglichkeit besitzt. Ein ausgerüstetes Kolonieschiff braucht dafür auch einen legal erreichbaren Standort.

Ausscheiden wird genau einmal verarbeitet: vereinbarte Übergaben erhalten Identitäten und Bestände; Befehle, Produktion, Verträge und Zusagen enden. Nicht übertragene mobile Einheiten werden außer Dienst gestellt, ihre Ladung als ausgeschiedener Bestand beziehungsweise Verlust verbucht. Nicht übertragene Gebäude werden neutral und inaktiv, behalten ihre Lager und können physisch gesichert werden.

Grundlage: WIN-001 bis WIN-008, D-013.

## 15 Gestaltung und Technik

Verbindlich sind Unity und C#, eingerichtet mit Unity 6000.3.29f1. Der erste Build richtet sich an Windows mit Maus und Tastatur. Weitere Desktop-, Web- und Mobilziele sowie Konsolen benötigen eigene Export- und Bediennachweise.

Die Gestaltung bleibt minimalistisch: dunkler Weltraum, einfache Geometrie, zurückhaltende Palette, orthografische Kamera und gut lesbare Zustände. Beschriftung und Formen ergänzen Farben. Sichtbare Spieltexte und Dokumentation sind zunächst Deutsch; technische Identifikatoren Englisch. Texte werden für spätere Übersetzung von der Logik getrennt.

Ein Unity-unabhängiger C#-Kern verwaltet gemeinsame Welt, feste Simulationsschritte und stabile Identitäten. Spieler und KI nutzen dieselbe validierte Befehlsschnittstelle. Darstellung und Eingabegeräte sind getrennte Adapter. Definitionen für Kosten, Zeiten und Reichweiten sind von veränderlichem Zustand getrennt.

Waren und Einheiten existieren an genau einem Ort. Laden, Entladen, Gründung, Eroberung und Zerstörung erhalten diese Integrität. Speichern und Laden bewahrt Zeit, Zufallszustand, Besitz, Produktion, Fracht, Reservierungen, Wissen, Forschung, Verträge, KI-Pläne und laufende Operationen. Ungültige Daten müssen nachvollziehbar abgewiesen werden.

Automatisierte Prüfungen sichern Regeln und Übergänge; sichtbare Spieltests prüfen Bedienung und Lesbarkeit. Leistung wird am tatsächlichen Szenario gemessen. Der Prototyp liefert keine Performancegarantie für das Gesamtspiel.

Grundlage: ARCH-001 bis ARCH-016, D-008, D-011, D-014 bis D-016, T-001 bis T-006.

## 16 Abgrenzung und offene Balance

Mehrere Systeme, Sprungverbindungen als bespielbare Erweiterung, Gasriesenförderung, zusätzliche Schiffsklassen, Bergung, Kampagnen und besondere Szenarien sind spätere Ausbaustufen. Mehrspieler, vollständige bebaubare Planetenkugeln, frei gesteuerte Raumhöhe, ein eigenständiges Luftkampfspiel und beliebig große Galaxien gehören nicht zum ersten Lieferumfang. Onlinekonto, Cloud und ML-Dienst sind keine Voraussetzung.

Kosten, Produktionsraten, Reichweiten, Schaden, Schildstärke, Vorräte, Sicherungsdauer, Forschungsbaumumfang und Kartenabmessungen bleiben überprüfbare Balanceparameter. Vorläufige Werte der aktuellen Koloniewirtschaft stehen in den Detailregeln und Definitionsdaten; sie werden nicht als endgültige Gesamtspielbalance festgeschrieben.

## 17 Entwicklungsstand und Reihenfolge

Aktuell ist M7 in 1.0.8 lokal umgesetzt und automatisiert geprüft; siehe [M7_NACHWEIS](M7_NACHWEIS.md). Historischer Nachweis zu M4: **M4 ist in Produktversion 1.0.3 abgeschlossen.** Schiffsbau, freie Erkundung, bewegte Ziele, Eskorte, bezahlte Koloniegründung, physische Frachter, automatische Hafenversorgung, feste Routen, Kristalle, Elektronik und fünf verzweigte Technologien sind vorhanden. 58 Core-Tests, Windows-Build, 91 grafische Einzelprüfungen, elf kontrollierte Renderbilder und gezielte native Eingaben sind nachgewiesen. [M4_NACHWEIS](M4_NACHWEIS.md) unterscheidet Bedienprüfung, automatisierten Wirtschaftsdurchlauf und verbleibende Grenzen. Die folgenden Angaben zu M3 und 1.0.1 bleiben historisch.

Aktualisierung nach dem anschließenden M3-Auftrag: Produktversion **1.0.2** ergänzt reguläre Rekrutierung, Bodengefechte, Sicht, Mauern/Tore, vier Stellungen, Depots und Pioniernachschub. **43 Core-Tests**, Windows-Build, **71 grafische Einzelprüfungen**, sieben Renderbilder und gezielte echte Maus-/Tastaturprüfungen sind erfolgreich. Umfang, vorläufige Balance und Grenzen stehen in [M3_NACHWEIS](M3_NACHWEIS.md). Die nachfolgenden Angaben zu 1.0.1 halten den Ausgangsstand der Rekonstruktion fest.

Der dokumentierte Ausgangsstand ist Produktversion 1.0.1. M0 ist abgeschlossen; M1 und M2 sind nach T-006 technisch abgenommen. Vorhanden sind gemeinsame Welt und Zeit, Sonnensystem und Oberflächen, Bau, lokale Transporte, Erz-/Metall-/Bauteilwirtschaft, Nahrung, Bevölkerung, Arbeitskräfte, Energiepuffer, Betriebsprioritäten, Straßenunterbrechung, Rückbau, Pause und Save/Load. Die Prüfkarte hat zwei eigene Kolonien und einen neutralen Mond.

Dokumentierte Nachweise: Windows-Build, 28 Core-Tests und 58 programmatische Prüfbedingungen im sichtbaren Player mit vier kontrollierten Renderbildern. Der zusätzliche physische Mausbedienungstest und andere Plattformen bleiben offen. Diese Übersicht behauptet keine neue Testausführung.

| Meilenstein | Inhalt | Stand nach M7 |
|---|---|---|
| M0 | Unity und Werkzeuge | Abgeschlossen |
| M1 | Gemeinsame Simulation, Befehle und Speichergrundlage | Technisch abgenommen nach T-006 |
| M2 | Lokale Koloniewirtschaft | Technisch abgenommen nach T-006 |
| M3 | Bodentruppen, Gefechte, Türme, Mauern und Nachschub | P06 und P07 in 1.0.2 abgeschlossen |
| M4 | Schiffsbau, Kolonisierung, Frachter und Forschung | P08–P10 in 1.0.3 abgeschlossen |
| M5 | Raumkampf, Landung und durchgängige Invasion | In 1.0.5 lokal umgesetzt und automatisiert geprüft; manuelle Abnahme/Git-Integration offen, siehe [M5_NACHWEIS](M5_NACHWEIS.md) |
| M6 | Eigenständige wirtschaftliche und militärische KI | In 1.0.7 lokal umgesetzt und automatisiert geprüft; manuelle Abnahme/Git-Integration offen |
| M7 | Verträge, Koalitionen und gemeinsamer Sieg | In 1.0.8 lokal implementiert und automatisiert geprüft; manuelle Abnahme/Git-Integration offen |
| M8 | Gesamtintegration, Exporte und Referenzszenario | Offen |

Der aktuelle Paketstatus und die Abnahmekriterien werden weiterhin zentral im [IMPLEMENTATION_PLAN](IMPLEMENTATION_PLAN.md) geführt. Konkrete Prüfszenarien stehen in [ACCEPTANCE_TESTS](ACCEPTANCE_TESTS.md). Neue Regelentscheidungen werden in den zuständigen Fachregeln und [DECISIONS](DECISIONS.md) nachgeführt; diese Konzeptübersicht wird bei inhaltlichen Änderungen entsprechend aktualisiert.
