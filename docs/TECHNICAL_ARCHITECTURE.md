# Technische Architektur für die Umsetzung

Die Umsetzung verwendet Unity und C#. Der Simulationskern bleibt von UnityEngine unabhängig; Darstellung, Eingabe und Plattformzugriffe werden über Unity angebunden. Die konkrete Editorinstallation und ihre Laufzeitnachweise werden in [M0](IMPLEMENTATION_PLAN.md) nach dem [Entscheidungsstand](DECISIONS.md) festgehalten. Die folgenden Regeln beschreiben das Zielverhalten; ihr Vorhandensein im Dokument ist noch kein Implementierungsnachweis.

## Projektaufteilung und erster Prototyp

Die erste Umsetzung trennt `Assets/PlanetOne/Core` für reine C#-Simulation, `Assets/PlanetOne/Runtime` für Unity-Darstellung und Bedienung, `Assets/PlanetOne/Editor` für Projektvorbereitung und Buildwerkzeuge sowie `Assets/PlanetOne/Scenes` für die startbare Szene. Unabhängige Kernprüfungen liegen unter `Tests/Core`. Assemblygrenzen verhindern einen versehentlichen UnityEngine-Zugriff aus dem Kern.

Ein Unity-Laufzeitcontroller besitzt die einzige aktive Simulation. Sonnensystem und Oberfläche wechseln lediglich ihre Darstellung. Die orthografische Kamera zeigt einfache Geometrie über einer gemeinsamen taktischen Ebene. Der Built-in-Renderer hält das erste Projekt ohne zusätzliche Renderpipeline und ohne externe Detailassets überschaubar.

Der erste vertikale Prototyp kombiniert das Fundament von P01–P05 mit dem Darstellungs- und Orbitanteil von P08. Die vorgezogene Ansicht macht P08 noch nicht vollständig: Schiffsbau, Koloniegründung und deren Nachweise folgen weiterhin im vorgesehenen Paket. Ebenso gelten Transporte auf lokalen Wegen nicht als Nachweis interplanetarer Frachter.

Implementierungsstand M1/M2, Produktversion 1.0.1: `GameSimulation` verarbeitet Zeit, Berechtigungen, Befehle und Wege; die Teildateien `SimulationEconomy.cs` und `SimulationLogistics.cs` betreiben Bevölkerung, Produktion, Bau sowie physische Transporte. `Definitions.cs` enthält Kosten, Zeiten und Kapazitäten, `SimulationState.cs` den gespeicherten Zustand und `SaveCodec.cs` dessen Prüfung und Migration. Die Wirtschaft verwendet zwei Bauplätze pro Kolonie, priorisierte Energie-/Arbeitskräftevergabe und lokale Eingangs-, Ausgangs- und Rücknahmereservierungen. 28 Core-Verhaltenstests, der Unity-Windows-Build und 58 programmatische Prüfbedingungen im sichtbaren Player sind bestanden; vier Renderbilder wurden visuell kontrolliert. Die vollständige manuelle Mausbedienungsabnahme der erweiterten Wirtschaft ist eine gesonderte, noch offene Prüfung.

## Visuelle Leitlinien

Der vom Nutzer gewünschte minimalistische Stil verwendet dunkle Hintergründe, zurückhaltende Geländeoberflächen und klare Silhouetten. Gebäude erhalten wenige charakteristische Formen; Auswahl, Baufortschritt, fehlende Versorgung und laufende Produktion bleiben deutlich unterscheidbar. Akzentfarben unterstützen Text, Symbole und Form statt sie zu ersetzen.

Umlaufbahnen, Bauvorschau, Lieferbewegungen und Reichweiten sind funktionale Darstellungen. HUD-Bereiche zeigen nur die zum aktuellen Ort oder Auftrag benötigten Informationen. Effekte, Texturen und zusätzliche Grafikpakete sind kein Abschlusskriterium des ersten Prototyps. Sichtprüfung umfasst Lesbarkeit bei der vereinbarten Auflösung und verständliche Rückmeldungen bei ungültiger Platzierung.

## Grundsätze

- **ARCH-001:** Es gibt einen autoritativen Spielzustand und eine gemeinsame Simulationszeit für alle Systeme, Planeten, Transporte, Gefechte und Fraktionen.
- **ARCH-002:** Darstellung und Eingabe lesen abgeleitete Ansichten und erzeugen Befehle. Sie verändern Inventare, Trefferpunkte oder Besitz nicht direkt.
- **ARCH-003:** KI und Spieler verwenden denselben Befehlspfad einschließlich Besitz-, Ressourcen-, Sicht- und Vertragsprüfung.
- **ARCH-004:** Ein Ansichtswechsel lädt Darstellung nach Bedarf, erhält aber die Simulation aller betroffenen Orte. Ausgeblendete Kämpfe werden nach denselben Regeln berechnet.
- **ARCH-005:** Daten und Logik werden ausreichend getrennt, um zentrale Regeln ohne Renderfenster prüfen zu können. Die Architektur verlangt keinen späteren kostenlosen Enginewechsel und kein selbst entwickeltes universelles Framework.

## Systemgrenzen

| Modul | Verantwortlich für | Darf nicht selbst bestimmen |
|---|---|---|
| Simulation | Takt, stabile IDs, Befehlsreihenfolge, Zufallszustand und Ereignisse | Rendering oder Eingabegeräte |
| World | Himmelskörper, Orbits, Oberflächen, Navigation und räumliche Abfragen | Waren aus dem Nichts oder diplomatische Ausnahmen |
| Economy | Lager, Reserven, Rezepte, Arbeitskräfte, Energie und Bauaufträge | Frachterbewegung oder physische Fernübertragung |
| Logistics | Lieferbedarf, Transportreservierung, Beladung, Wege und Entladung | Globale Verfügbarkeit lokaler Vorräte |
| Research | Projektvoraussetzungen, lokale Versuchsmaterialien, Fortschritt und Freischaltungen | Geschenkte Ressourcen oder direkte Gebäudespawns |
| Combat | Sichtbare Ziele, Reichweite, Schaden, Munition, Reparatur und Zerstörung | Planetare Besitzwechsel allein durch einen Treffer |
| Invasion | Truppenverladung, Landung, Brückenkopf, Versorgung, Besetzung und Evakuierung | Kostenlose Truppenwellen |
| Diplomacy | Beziehungen, Vertragsrechte, Koalitionen und gemeinsame Zusagen | Direkte Kontrolle sämtlicher verbündeter Einheiten |
| AI | Wahrnehmung, Prioritäten, Produktions- und Operationspläne, Befehle | Ungefilterter Zugriff auf gegnerischen Zustand |
| Objectives | Projektlieferungen, Aktivierung, Siegergruppe, Kapitulation und Niederlage | Verdeckte Sonderbedingungen aus dem UI |
| Persistence | Versionierte Zustandsabbilder und Wiederherstellung | Verweis auf kurzlebige Renderobjekte als Spielidentität |
| Presentation | Kameras, Auswahl, HUD, Karten, Warnungen und Effekte | Eigene parallele Berechnung autoritativer Treffer oder Ressourcen |
| Platform | Speicherort, Geräteinput, Audio, Dateizugriff und Exportanbindung | Abweichende Kernregeln je Plattform |

Ein Modul ist eine fachliche Zuständigkeit; daraus folgen nicht zwingend separate Prozesse, Pakete oder Projekte. Mit der kleinsten übersichtlichen Struktur beginnen. Laufzeit-Plugins, verteilte Dienste und Netzwerkarchitektur sind nicht Bestandteil des Grundgerüsts.

## Daten und Entitäten

**ARCH-006:** Unveränderliche Definitionen und veränderlicher Zustand sind getrennt. Definitionen besitzen stabile IDs für Güter, Rezepte, Gebäude, Schiffe, Bodeneinheiten, Waffen, Forschung und Szenarien. Kosten und Zeiten sind Konfiguration, keine Kopien in UI und KI.

| Zustand | Mindestens benötigte Informationen |
|---|---|
| GameState | Formatversion, Szenario- und Definitionsversion, Tick, Seed und Zufallszustände, nächster ID-Wert, Fraktionen und Orte |
| Faction | ID, eigene Forschung, Projekte, diplomatische Beziehungen, persönliches Wissen und KI-Zustand falls vorhanden |
| CelestialBody | ID, System, Typ, übergeordneter Körper, Orbitparameter, optionale Oberfläche und Koloniezugehörigkeit |
| Surface | Terrain, Höhen-/Deckungsdaten, Navigationshindernisse, Gebäude, Bezirke, lokale Sicht und Landezonen |
| Inventory | Ort/Entität, Kapazität, physische Menge je Gut, ausgehende Reservierungen und reservierter Zielraum |
| ProductionJob | Rezept, lokale Inputreservierung, bereits verbrauchter Input, Fortschritt, Outputziel und Blockiergrund |
| Unit | stabile ID, Eigentümer, Typ, Lebenspunkte, Ausrüstung, Munition, Befehl, Lebenszyklus, Ort und gegebenenfalls CarrierId |
| Building | stabile ID, Eigentümer, Standort, Baufortschritt, Lebenspunkte, Energie-/Personalbelegung und Funktion |
| TransportJob | Quelle, Ziel, Frachtmanifest, Reservierungen, zugewiesener Träger, Zustand und Abbruchgrund |
| Operation | Ziel, beteiligte Fraktionen, vorgesehener Eigentümer eroberter Orte, reservierte Kräfte und Zustandsfortschritt |
| Treaty | Parteien, explizite Rechte/Pflichten, Geltungsorte, Status, Fristen, Zusagen und dokumentierte Verletzungen |
| VictoryProject | Eigentümer/Koalition, gelieferte Komponenten, Aktivierungsstatus, festgehaltene Mitgliedschaft und Fortschritt |

Eigene Güterbestände sind vollständig lesbar. Gegnermengen, Ziele und Produktionsschlangen werden nicht dadurch öffentlich, dass dieselbe Datenstruktur in der Simulation existiert.

### Ortsmodell

**ARCH-007:** Eine aktive Einheit besitzt genau einen Ort vom Typ `Space`, `Surface`, `Transfer` oder `Embarked`. `Transfer` beschreibt insbesondere den sichtbaren Abstieg oder Aufstieg eines Transporters. Ein Ortswechsel erhält ID, Eigentümer, Schäden, Munition und Befehlsfortsetzung. Geladene Einheiten existieren nicht zusätzlich auf der Oberfläche.

Der Lebenszyklus wird getrennt geführt: `Active`, `Destroyed`, `Consumed` oder `Retired`. Ein bei Koloniegründung verbrauchtes Schiff ist `Consumed`, ein beim Fraktionsausscheiden außer Dienst gestelltes Schiff `Retired`. Entfernte Einheiten nehmen nicht mehr an Ortsabfragen oder Befehlen teil; ihre IDs werden nicht neu vergeben und ihr Abgangsgrund bleibt für Integritätsprüfung und Spielstand eindeutig.

Ein Raumfrachter besitzt einen Systemort und ein Frachtmanifest. Ein Landungstransporter kann Güter und Einheiten gemäß Kapazitätsdefinition tragen. Ein Kolonisationsschiff enthält deklarierte Startausrüstung und Besatzung; bei Gründung wird es genau einmal verbraucht und in den Kolonieanfangszustand überführt.

Der Raumhafen besitzt einen Oberflächenstandort und einen zugehörigen Orbitzugang. Güterwechsel zwischen beiden Seiten erfordern einen Transferauftrag, Transferzeit und freie Kapazität. Für die erste Umsetzung darf der lokale Shuttleflug abstrahiert dargestellt werden; Waren müssen währenddessen weiter einem benannten Transportzustand zugeordnet sein.

## Befehle und Ereignisse

**ARCH-008:** Ein Befehl enthält `commandId`, `issuerFactionId`, gewünschte Ausführungsreihenfolge, Befehlstyp, Entitätsreferenzen und Nutzdaten. Namen dienen als Schema, nicht als Sprachfestlegung.

Beispiele: `PlaceBuilding`, `QueueProduction`, `AssignRoute`, `MoveUnits`, `AttackTarget`, `EmbarkUnits`, `BeginLanding`, `CaptureBuilding`, `ProposeTreaty`, `CommitOperation`, `StartResearch`, `ActivateVictoryProject`.

Bei Verarbeitung prüft die Simulation:

1. Existieren Auftraggeber und referenzierte Entitäten noch?
2. Ist der Auftraggeber zum Steuern oder Beantragen berechtigt?
3. Erlauben aktuelle Sicht und Kenntnis die konkrete Zielauswahl?
4. Erlauben Vertrag, Ort, Phase und Technologie die Aktion?
5. Sind Kapazität, Material und mögliche Reservierungen konsistent?

Unzulässige Befehle liefern einen stabilen Grundcode, etwa `InsufficientStock`, `NoLandingRights`, `TargetNotVisible` oder `DestinationUnavailable`. UI und KI reagieren auf denselben Grund. Eine erfolgreiche Validierung ist keine Garantie, dass ein späterer Flug nicht durch einen Angriff unterbrochen wird.

**ARCH-009:** Wiederholung derselben commandId darf keinen zweiten Ressourcenverbrauch oder Spawn auslösen. Sichtbezogene Angriffe auf veraltete Positionen sind als Angriff auf einen Ort zu kennzeichnen; sie dürfen unsichtbare bewegte Ziele nicht weiter verfolgen.

Ereignisse wie `ShipmentArrived`, `LandingDetected`, `SupplyInterrupted`, `TreatyBroken` und `ColonyCaptured` enthalten Tick, Beteiligte und Ort. Ereignisse werden vor Anzeige nach Wissen gefiltert. Debugprotokolle dürfen umfassender sein, sind aber kein Informationskanal für die KI.

Im vorhandenen M1/M2-Kern sind `PlaceBuilding`, `PlaceRoad`, `RemoveRoad`, `CancelConstruction`, `DemolishBuilding`, `SetBuildingEnabled`, `SetBuildingPriority`, `SetPaused` und `SetTimeScale` implementiert. Eingereihte Bau-/Betriebsbefehle werden kopiert und am Tickbeginn erneut geprüft. Nur die Spielerfraktion darf Pause und Tempo unmittelbar steuern. `CommandResult` liefert Erfolg, Grundcode, Befehl und Tick; `SimulationEvent` zusätzlich Fraktion, Ort und Entität. Jeweils die letzten 128 Einträge bleiben im Zustand. Fraktionen und paarweise Szenariobeziehungen `Peace`/`War` sind gespeichert; KI-Wahrnehmung und verhandelte Verträge sind noch nicht umgesetzt.

## Zeit und Reihenfolge

**ARCH-010:** Die Simulation verwendet feste Zeitschritte; Rendering interpoliert zwischen Ergebnissen. Ein Startwert von 20 Simulationsschritten pro Sekunde ist eine zu messende Arbeitsannahme, keine feste Produktanforderung.

Ein dokumentierter Tick besitzt eine stabile Reihenfolge:

1. Befehle übernehmen und validieren; Fristen und Vertragsänderungen anwenden.
2. Orbitpositionen und planetenrelative Bezugspunkte fortschreiben.
3. Energie, Arbeitszuweisung, Produktion, Bau und vorhandenen Nachschub fortschreiben.
4. Wege, Bewegung, physische Transporte und atomare Übergaben ausführen.
5. Sicht und Sensorwissen aktualisieren.
6. Zielwahl, Feuer, Schaden, Reparatur und Zerstörung verarbeiten.
7. Landungs-/Besetzungsfortschritt, Forschung und Missionsziele aktualisieren.
8. Sichtgefilterte Ereignisse und Lesemodelle veröffentlichen; KI plant Befehle für folgende Ticks.

Innerhalb eines Schritts muss die Iterationsreihenfolge stabil sein. Ein zerstörter Träger darf danach weder entladen noch erobern. Ziel- und Koalitionsänderungen im Tick müssen vor einer Siegfeststellung berücksichtigt werden.

Taktische Pause hält alle gameplayrelevanten Takte an, einschließlich Forschung, KI-Handlungen und Diplomatiefristen. Befehle werden währenddessen als Vorschau eingereiht und beim Fortsetzen erneut validiert. UI-Animationen dürfen weiterlaufen. Es entstehen durch Pausieren keine Produktions- oder Informationsvorteile für die KI.

Reproduzierbarkeit gilt zunächst für dieselbe Spiel-/Definitionsversion auf derselben unterstützten Laufzeit. Plattformübergreifende Bitidentität oder Netzwerk-Lockstep werden nicht vorausgesetzt. Zufallsquellen und ihre Zustände müssen dennoch kontrollierbar und speicherbar sein.

## Ressourcenintegrität und Logistik

**ARCH-011:** Physischer Bestand, Reservierung und erwartete Lieferung sind unterschiedliche Werte. Eine ausgehende Reservierung verringert die verfügbare Menge, aber verschiebt noch keine Ware. Eine erwartete Lieferung darf nicht lokal verarbeitet werden.

`lokal verfügbar = physischer Bestand - feste noch nicht verbrauchte Reservierungen`, mindestens null. `exportierbar = lokal verfügbar - geschützte Exportreserve`, ebenfalls mindestens null. Eine Exportreserve bleibt für örtliche Nahrung, Reparatur und Wiederaufbau nutzbar. Zweckgebundene Materialzusagen werden als explizite Reservierungen geführt. Zielkapazität berücksichtigt eingehende Reservierungen, ohne deren Inhalt als vorhandene Ware auszugeben.

Ein Transport durchläuft `Requested -> Reserved -> Loading -> InTransit -> Unloading -> Completed`. Er kann in `Waiting`, `Rerouting`, `Cancelled` oder `Lost` übergehen. `Loading` und `Unloading` besitzen Dauer und nachverfolgbare Mengen; jedes Teilpaket wird atomar zwischen genau zwei Inventaren verschoben.

Die lokale Umsetzung verwendet `DeliveryOrder` für vorgemerkte Lieferungen und `ShipmentState` mit `Loading`, `InTransit`, `Unloading`, `Returning` oder `Recovering`. `RouteBlocked` hält einen unterbrochenen Transport mitsamt Ladung an; ein verfügbarer Umweg erhält seine aktuelle Position. `ReservedReturnSpace` sichert ab Beladung den Rücknahmeplatz der Quelle, bis die tatsächliche Entladung ihn freigibt oder ein Abbruch ihn in eine Eingangsreservierung überführt. So können vier gleichzeitig zurückkehrende beladene Kuriere die Kolonie nicht allein wegen einer inzwischen vollgelaufenen Quelle blockieren. Militärischer Verlust und interplanetare Transfers sind noch nicht Teil dieser Implementierung.

- Quelle, Zielkapazität und Träger werden konsistent reserviert. Ein Fehler darf keine Teilreservierung dauerhaft verwaisen lassen.
- Vor Beladung abgebrochene Aufträge geben die Reservierung frei; Material verbleibt in der Quelle.
- Nach Beladung bleibt Material im Träger, bis es tatsächlich entladen, zerstört oder gesondert geborgen wird. Abbruch erzeugt keine Erstattung am Ursprungsort.
- Bei Verlust wird nur tatsächlich geladene Ware als Verlust gebucht; Reservierungen werden bereinigt.
- Ein erobertes Ziel löst Neubewertung von Eigentum, Handelsrechten und Lieferung aus. Fracht wird nicht automatisch an den neuen Feind verschenkt.
- Periodische Integritätsprüfungen erkennen negative Bestände, Überbuchung, doppelte UnitIds und verwaiste Reservierungen.

Zerstörte Fracht wird in der ersten Version als Verlust erfasst. Bergung ist eine spätere Funktion. Warenerhaltung gilt unter Berücksichtigung expliziter Quellen (Förderung, Produktion, Szenariostart) und Senken (Verbrauch, Bau, Zerstörung).

## Navigation und wechselnde Ansichten

**ARCH-012:** Orbits sind vorhersehbare Bahnen, keine vollständige Gravitation. Bewegungsbefehle können eine feste Position, eine bewegte Entität oder eine planetenrelative Verteidigungsposition referenzieren. Die Navigation berechnet den Treffpunkt und überprüft ungültige Ziele erneut.

Oberflächennavigation berücksichtigt unterschiedliche Bewegungsprofile für Infanterie und Fahrzeuge, zerstörbare Hindernisse und Tore. Ein bekannter blockierter Weg wird neu geplant oder als Hindernisangriff bewertet. Die Wegfindung umgeht Mauern nicht durch Versetzen von Einheiten.

KI-Gefahrenkarten verwenden bekannte Stellungen. Einheiten reagieren auf neu entdeckte Hindernisse. Änderungen an Toren, Gebäuden und Mauern invalidieren die betroffenen Navigationsdaten.

Alle Oberflächen behalten ihre Simulationsdaten. Nur Darstellung, Animation und Detailobjekte können bei einem Ansichtswechsel entfernt werden. Eine Hintergrundoptimierung ist nur zulässig, wenn sie das relevante Verhalten erhält und durch Vergleichsszenarien geprüft wird.

## KI und Diplomatie als Verbraucher der Simulation

**ARCH-013:** Die KI erhält ein eigenes `FactionObservation` mit eigener Wirtschaft, erlaubten Verbündetendaten, sichtbaren Gegnern und datierten letzten Sichtungen. Regeln und öffentlich bekannte Terraininformationen sind erlaubt; verdeckte gegnerische Absichten sind es nicht.

Strategische Planung, Operationsplanung und taktische Befehle werden mit begrenzten, gestaffelten Rechenbudgets ausgeführt. Eine neue Wahrnehmung kann einen Plan invalidieren. Der Wechsel erhält einen nachvollziehbaren Grund; wiederholte Misserfolge führen zu einer neuen Bewertung statt identischer Endlosschleifen.

Vertragsprüfung ist eine zentrale Abfrage für Durchflug, Landung, Versorgung, Angriff und Bau. Verschiedene Ansichten dürfen keine abweichenden Rechte anwenden. Zusagen reservieren konkrete Einheiten oder Kapazitäten; dieselbe Fregatte wird nicht gleichzeitig zwei Verbündeten versprochen.

Die fachlichen Zustände und Siegbedingungen sind ausschließlich in [AI_DIPLOMACY](AI_DIPLOMACY.md) definiert. Sie werden hier nicht durch vereinfachte globale Feind-/Freund-Schalter ersetzt.

## Speichern und Laden

**ARCH-014:** Ein Spielstand wird an einer konsistenten Tickgrenze aufgenommen. Er enthält aktive Transporte und Reservierungen, eingeladene Einheiten, Bau-/Forschungsfortschritt, Munition, Sichtwissen, KI-Pläne, Verträge, Zusagen, Fristen, Orbits und das laufende Siegprojekt.

Definitionen werden über IDs und Versionen referenziert. Renderobjekte, Dateihandles und unkontrollierte Wallclock-Timer gehören nicht in den autoritativen Zustand. Navigation und Grafik dürfen nach dem Laden aus gespeicherten Fakten neu aufgebaut werden.

Speichern erfolgt über einen Plattformadapter möglichst atomar, mit Schutz des vorherigen gültigen Stands. Unbekannte oder inkompatible Versionen werden verständlich abgelehnt; keine stillschweigende Teilwiederherstellung. Migrationen erst für tatsächlich unterstützte ältere Formate implementieren.

Der aktuelle JSON-Schnappschuss verwendet **Speicherformat 2**. Es umfasst auch Bevölkerung, Nahrungsrest und -mangel, Wachstum, Energiespeicher, Prioritäten, Rückbau, Fracht samt Route und Rücknahmeraum, Fraktionen, Beziehungen, Ereignisse und ausstehende Befehle. Das ursprüngliche Format 1 wird ausdrücklich migriert: vorhandene Waren, IDs, Reservierungen und Produktionsfortschritte bleiben erhalten; neue Bevölkerungsdaten werden initialisiert und alte Rückfahrtrouten angepasst. Ein nur für bereits vorhandene Überbelegung benötigter Migrationsfreiraum schrumpft beim Freiwerden und lässt sich nicht erneut auffüllen. Eine unveränderte Originaldatei unter `Tests/Core/Fixtures/prototype-v1.json` sichert diesen Fall ab. Produktversion aus `VERSION`, Unity-Editorversion und Speicherformat sind getrennte Versionsarten.

Abnahme: Vor und nach einem Speichern/Laden bleiben eindeutige IDs, Mengen, Rechte und aktive Phasen identisch. Eine Fortsetzung desselben Befehlsablaufs liefert auf derselben Laufzeit dieselben überprüften Ergebnisse.

## Portabilität und Leistungsnachweis

**ARCH-015:** Eingaben werden als Spielaktionen modelliert, etwa `Select`, `IssueMove`, `Pause` und `OpenPlanet`. Maus, Tastatur, Touch und Controller sind mögliche Adapter. Die erste Oberfläche darf Maus/Tastatur priorisieren; Touch-Unterstützung wird dadurch nicht schon als fertig angesehen.

Keine absoluten Windows-Pfade in Spiellogik, keine direkte Annahme eines frei beschreibbaren Dateisystems und keine fest vorausgesetzten Hintergrundthreads. Auflösung, Texte und Bedienflächen skalierbar halten. Browser- und Mobilgrenzen vor plattformspezifischen Bibliotheken prüfen.

**ARCH-016:** Vor Optimierung messen: Simulationszeit, KI-Zeit, Pfadsuchen, aktive Transporte, Einheitenzahl, Speicher und Renderzeit getrennt erfassen. Wiederholte teure globale Suchen vermeiden; geeignete räumliche Indizes und Ereignisse erst am gemessenen Bedarf ausrichten.

Vorgeschlagene Referenzlast für die spätere Messung: drei Fraktionen, fünf Oberflächen, insgesamt 150 Gebäude, 120 Bodeneinheiten, 60 Kampfschiffe und 20 Transporter. Das ist ein vorläufiges Testszenario, kein versprochenes Einheitenlimit. In M0 Hardware und Zielbudgets festhalten; in M8 tatsächlich messen und gegebenenfalls Scope oder Optimierung begründen.

## Prüfbarkeit und Fehlerdiagnose

Debugwerkzeuge sollen Seed, Tick, Befehle, Inventaränderungen, aktive Reservierungen, KI-Entscheidungsgründe und Vertragszustände reproduzierbar anzeigen können. Der normale Spieler erhält verständliche Ursachen statt interner Identifikatoren.

Die konkreten Nachweise stehen in [ACCEPTANCE_TESTS](ACCEPTANCE_TESTS.md). Regeltests prüfen Integrität und Zustandswechsel; integrierte Tests prüfen Invasion, Diplomatie und Sieg; manuelle Tests prüfen Lesbarkeit und Bedienbarkeit. Reine Dokumentation hat noch keine Laufzeittests bestanden.
