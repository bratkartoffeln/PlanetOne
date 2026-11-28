# PlanetOne Spielregeln

## Ergänzung: auswählbare Darstellung

- **GUI-019:** Ein kurzer Rechtsklick mit ausgewählten Einheiten erteilt direkt einen Bewegungsauftrag; über einem sichtbaren Kriegsgegner wird daraus ein gültiger Angriff für passende bewaffnete Einheiten. Strg + Rechtsklick öffnet nur das Objekt-/Zielmenü, einschließlich Diplomatie; das Öffnen erteilt keinen Befehl. Rechtsziehen verschiebt die Kamera ohne anschließenden Klickbefehl. Auswahlrahmen umschließen das tatsächliche Modell, der Angriffscursor ist ein rotes Fadenkreuz. Neutrale Ziele sind gesperrt; verdeckte Ziele erhalten keine verräterische Angriffsmarkierung und werden nicht automatisch angegriffen.
- **GUI-020:** „Planet zentrieren“ bewegt die Kamera sanft zum Planeten und folgt seiner laufenden Umlaufposition. Manuelles Verschieben, Drehen, Zoomen oder Pos1 beendet den Fokus. Planetenauswahl in der Seitenliste führt zum Ziel im Sonnensystem, ohne dessen Oberfläche automatisch zu öffnen. Aus einer geöffneten Oberfläche erfolgt zuerst die animierte Systemrückkehr und danach die sanfte Anfahrt. Kamera und Fokus verändern weder Umlaufbahnen noch Spielaufträge.
- **GUI-021:** Planetenkugeln sollen bei Eigenrotation und Kameradrehung keine harte Texturkante am Längengradübergang zeigen. Die bestehenden typabhängigen Atlanten werden nahtfrei abgetastet; Oberflächenkontrast und Typunterschiede bleiben sichtbar.

- **GUI-015:** Die Schrift ist getrennt von der Oberflächengröße zwischen 90 und 140 Prozent einstellbar. Layout A/B/C erhalten dieselben Spielaktionen. Einstellungen zeigen eine Vorschau; Menüzeilen passen sich an die Schrift an, umfangreiche Inhalte bleiben scrollbar. Globale Navigation, ausgewähltes Objekt und konkrete Aktion sind voneinander erkennbar; Prüfkarten bleiben vom regulären Spiel getrennt.
- **GUI-016:** Bewegte Modelle werden zwischen den Simulationszuständen geglättet und richten sich weich nach ihrer Fahrt aus. Gerundete Straßen und Kurierdarstellung verwenden dieselbe Kurvengeometrie einschließlich Gebäudeanschlüssen. Die zugrunde liegende Erreichbarkeit, Fahrtdauer und Warenposition bleiben im autoritativen Hexpfad geregelt; Kantenstraßen sind nicht Teil dieser Fassung. Umlaufbahnen behalten beim Zoomen eine annähernd konstante Bildschirmbreite.
- **GUI-017:** Geschosse, Mündungsbilder, Treffer und Explosionen beruhen auf tatsächlich ausgeführten Schüssen beziehungsweise Zerstörungen. Effekte erzeugen keinen zweiten Schaden und verbrauchen keine zweite Munition. Verdeckte Ziele und Treffer dürfen nicht durch die Darstellung verraten werden. Geschütztürme können unabhängig von der Fahrzeugausrichtung drehen. Nach Laden werden vergangene Effekte nicht als neue Schüsse abgespielt.
- **GUI-018:** Der Wechsel zwischen System und Oberfläche erhält eine kurze Kamerafahrt mit 1,8 Sekunden Animationszeit plus variabler Oberflächenvorbereitung. Der Aufbau verteilt sich während der Atmosphärenblende auf mehrere Frames. Diese Fahrt ist Darstellung derselben Welt und kein Schiffsflug oder Landeauftrag. Währenddessen verhindern Eingabesperren widersprüchliche Weltbefehle; die gemeinsame Simulation und ihr bestehender Pausenzustand bleiben maßgeblich.

- **GUI-013:** Das Spiel startet im Hauptmenü. Reguläre Szenarien und Prüfkarten sind getrennt erkennbar. Startwerte sind prüfbare ganze Zahlen; neue Partien verwenden die bestehenden endlichen Startbedingungen. Menü und Einstellungen halten die Simulationsfortschaltung an, ohne den gespeicherten Pausenzustand zu ändern. Eine kurze Starthilfe erklärt vorhandene Aktionen und bietet direkte Navigation.
- **GUI-014:** Acht unabhängige lokale Speicherplätze besitzen jeweils eine vorherige gültige Sicherung. Vorhandene Einzeldateien bleiben unverändert; unterstützt lesbare Stände können in einen neuen Platz übernommen werden. D-041 hebt die Pflicht zur Unterstützung sämtlicher alter Formate während der Entwicklung auf. Belegte manuelle Speicherziele, Laden, Partiewechsel und Beenden verlangen im Spiel eine ausdrückliche Bestätigung. F5 aktualisiert den aktiven Platz beziehungsweise verwendet beim ersten Speichern einen freien Platz; bei vollem Katalog wird nichts ungefragt überschrieben. Beschädigte oder nicht unterstützte Dateien bleiben mit Fehlermeldung sichtbar; eine Sicherung wird niemals stillschweigend anstelle des gewählten Stands geladen.

- **GUI-012:** Häufige Objektbefehle bleiben in allen Layouts sichtbar, Spezialaufgaben öffnen sich gezielt. Seit GUI-019 öffnet **Strg + Rechtsklick** das benannte Objekt-/Zielmenü; kurzer Rechtsklick erteilt den direkten Auftrag, Rechtsziehen bleibt Kamerabewegung. Öffnen, Schließen und Navigieren im Menü erteilt keine Spielaufträge. Objekt, Welt und Akteure bleiben während eines Menüs fest zugeordnet; verlorene Sicht oder Eigentumsänderung sperrt veraltete Aktionen. Außenklick wird beim Schließen verbraucht. Rückbau zeigt eine ausdrückliche Bestätigung. [Historischer Befehls-GUI-Nachweis](BEFEHLS_GUI_NACHWEIS.md).

- **GUI-001:** Kommandozentrale, Orbitale Brücke und Freier Weltraum sind Layouts derselben Partie. Wechsel verändert weder Zeit, Güter, Befehle noch Sichtwissen. Alle vorhandenen Spielbereiche bleiben erreichbar.
- **GUI-002:** Layout, Oberflächengröße, separate Schriftgröße und Anzeige der Wabengrenzen sind lokale Einstellungen außerhalb des Spielstands. Ungültige Einstellungen dürfen den Start nicht verhindern.
- **GUI-003:** Geöffnete Menüs/Einstellungen sperren Weltbefehle und darunterliegende GUI-Eingaben. Kamera und Auswahlprojektion verwenden den tatsächlichen Weltbereich des jeweiligen Layouts. Eine Auswahl über einer GUI-Fläche darf nicht als Weltbefehl interpretiert werden.
- **GUI-004:** Texturen und Modellverfeinerungen erhalten die Wabengeometrie, Sichtgrenzen und eindeutigen Zustandsanzeigen. Transparente Gebäudevorschauen stellen dieselben Modelle wie die Welt dar und kennzeichnen gültige/ungültige Platzierung nach den gemeinsamen Regeln. Sichtbare Materialstapel und Bauphasen folgen tatsächlicher Lieferung und Bauarbeit. Referenzspiele dienen der Gestaltung, nicht als Quelle übernommener Grafikdateien; eine Rastervorlage ist kein fertiges räumliches Modell.
- **GUI-005:** Gebäude, Bodeneinheiten, Gütertransporter, Schiffe und Himmelskörper besitzen räumliche Geometrie. Die Kamera kann um die Welt gedreht und geneigt werden. Bewegte Modelle zeigen mit der Vorderseite in ihre tatsächliche Bewegungsrichtung; Stillstand erhält die letzte Ausrichtung, Kameradrehung verändert keine Einheit.
- **GUI-006:** Die Oberfläche bildet zusammenhängende, unregelmäßige Berge und Täler ab. Materialübergänge und Felsgruppen folgen der Landschaft. Straßen, Bodenobjekte, Wabengrenzen und Auswahl berücksichtigen die reale Höhe. Verdeckte Bodenpunkte dürfen nicht anstelle der vordersten sichtbaren Landschaft ausgewählt werden. Spielregeln bleiben gemäß T-037 vom Relief getrennt.

- **GUI-007:** Auswahl und Szenenwechsel sind getrennt. Ein Planetenklick zeigt bekannte Objektdaten und eine ausdrückliche Oberflächenaktion; ein Doppelklick darf diese direkt ausführen. B/C verwenden kompakte schließbare Auswahlkarten. Kopfzeile, Karten und Verwaltungsfenster dürfen keine Weltbefehle auslösen oder einen wartenden Zielbefehl verbrauchen. Ein gültiges Ziel wird durch dieselbe validierte Befehlsschnittstelle wie andere Eingaben verarbeitet.
- **GUI-008:** Die Systemkamera kann mit Rechtsziehen vom Zentralstern weg verschoben werden; kurze Rechtsklicks und Ziehen sind getrennte Gesten. Der Hintergrund ist räumlich fest, folgt der Blickrotation und zeigt beim Verschieben eine schwache, entfernungsabhängige Parallaxe. Planeten besitzen langsame Eigenrotation. Es gibt kein dem Mauszeiger folgendes Informationspopup; Namensschilder sind Textbreite plus je ein Leerzeichen links/rechts.
- **GUI-009:** Auch die Oberfläche lässt sich mit Rechtsziehen verschieben. Ziehen sendet keinen Einheitenbefehl; kurze Rechtsklicks senden erst beim Loslassen oder beenden einen Bau-/Zielmodus. GUI-Flächen können keine Geste starten. Planetentextur, Landschaft und Modelle entsprechen dem gespeicherten Typ; Gaswelten zeigen schwebende Basen und Fluggeräte. Pause hält die Eigenrotation an.
- **GUI-010:** Der sichtbare Planetenradius überschreitet nie den halben Sternradius. Monde bleiben deutlich kleiner als ihre Hauptwelt. Asteroiden werden gemäß D-039/T-060 zur besseren Erkennbarkeit erneut vergrößert und bleiben kleiner als Monde; die frühere Viertel-Mond-Grenze ist ersetzt. Ein diffuser Staubring markiert den Gürtel und blendet beim Annähern aus. Alle Raumpositionen und Eingabeprojektionen verwenden denselben vergrößerten Entfernungsmaßstab. Die Korona bleibt bei orthografischem Schwenken um den Stern zentriert. Asteroiden sind zusätzlich über kartierte Zielmarken und die Flottenverwaltung auswählbar; Nahansicht und Zielmarken berücksichtigen ihre tatsächliche räumliche Lage und Größe.
- **GUI-011:** Der Zentralstern ist in allen drei Layouts auswählbar. Seine Karte und Detailansicht zeigen Typ, Masse, Radius, Temperatur und Leuchtkraft des festen Sternmodells. Ein Stern besitzt keine besiedelbare Oberfläche. Auswahl, Detailanzeige, Zentrieren und Layoutwechsel verändern weder Zeit noch Befehle oder Ressourcen. Menüs und GUI-Flächen sperren darunterliegende Sternklicks.

Aktueller Darstellungs-/Baunachweis: [GUI_BEWEGUNG_BAU_NACHWEIS](GUI_BEWEGUNG_BAU_NACHWEIS.md) mit chronologisch getrennten Ergebnissen. Der frühere Stand bestand 177 Core-Tests und die dokumentierten Player-Läufe. Für spätere Verkehrsänderungen sind 7 neue Bodentests sowie 11 neue und 11 bestehende Raumtests bestanden; die erneute vollständige Core-/Unity-Abnahme einschließlich direkter Befehle, Fokus und Texturen steht aus. Historische Darstellungsstände: [GUI_WELTRAUM_NACHWEIS](GUI_WELTRAUM_NACHWEIS.md) und [GUI_GRAFIK_NACHWEIS](GUI_GRAFIK_NACHWEIS.md).

Diese Spezifikation beschreibt das verbindliche Zielverhalten des Spiels. Sie dient als fachliche Vorlage für die Umsetzung durch Codex. Anforderungs-IDs bleiben bei Umformulierungen stabil; fachliche Änderungen werden ausdrücklich dokumentiert.

Status: Zielkonzept, abgenommene M1–M4-Grundlagen und lokal automatisiert geprüftes M5 in 1.0.5; dessen Grenzen stehen in [M5_NACHWEIS](M5_NACHWEIS.md). Historisch: Produktversion 1.0.3 ergänzt Schiffsbau, Gründung, Raumfracht, Kristalle, Elektronik und Forschung; Nachweise und vorläufige Balance in [M4_NACHWEIS](M4_NACHWEIS.md). Historischer M3-Nachweis: Stand 23. November 2026, Produktversion 1.0.2. Der Unity-Windows-Player verbindet Sonnensystem, Koloniewirtschaft und Bodengefechte mit gemeinsamer Zeit und Save/Load. 43 Core-Tests, Windows-Build und 71 grafische Einzelprüfungen sind bestanden; sieben Renderbilder und gezielte echte M3-Maus-/Tastaturbedienung wurden geprüft. Details stehen in [M3_NACHWEIS](M3_NACHWEIS.md). Die umfassenden Anforderungen dieses Dokuments sind damit nicht vollständig umgesetzt. Den aktuellen Paketstand führt [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md). Zahlen zu Kosten, Reichweiten und Zeiten bleiben vorläufige Balanceparameter.

Die normale Aufbaukarte besitzt zwei eigene Kolonien und einen neutralen Mond. Bauaufträge benötigen einen erreichbaren Straßenanschluss; Straßen sind vorläufig kostenfrei. Bevölkerungswachstum, Nahrungsverbrauch, Betriebsprioritäten, Straßendemontage, Bauabbruch und Rückbau sind umgesetzt. M3 ergänzt eine separat ausgewiesene Gefechtsfixture mit endlichen Armeen und Beständen. M4 ergänzt regulär finanzierte Raumfahrt und Forschung. M5 ergänzt Raumkampf, Landung und örtliche Eroberung. KI und dynamische Diplomatie folgen später. Die Darstellung bleibt minimalistisch; die vollständige manuelle Abnahme sämtlicher älterer Wirtschaftsfunktionen ist weiterhin offen.

## 1 Spielidee und Geltungsbereich

**CORE-001** PlanetOne ist eine Echtzeit-Aufbaustrategie im Weltraum. Der Kernkreislauf lautet: erkunden, besiedeln, produzieren, transportieren, erforschen, verteidigen und expandieren. Wirtschaft, Raumkrieg, Bodenkrieg und Diplomatie beeinflussen einander.

**CORE-002** Auf Planeten entstehen Waren, Bevölkerung, Forschung und Bodentruppen. Raumflotten ermöglichen Expansion, schützen Warenströme und unterstützen Invasionen. Bodenarmeen erobern oder verteidigen tatsächlich aufgebaute Kolonien.

**CORE-003** Das Grundspiel ist ein Einzelspielerspiel gegen eine oder mehrere eigenständige Computerfraktionen. Das erste vollständige Integrationsszenario enthält den Spieler und zwei Computerfraktionen, damit Konkurrenz und Kooperation gleichzeitig prüfbar sind.

**CORE-004** Die erste vollständige Fassung besitzt ein Sonnensystem, bebaubare Planeten und mindestens einen bebaubaren Mond. Mehrere Systeme, Sprungverbindungen, Gasriesenförderung, zusätzliche Schiffsklassen und besondere Szenarien sind Erweiterungen. Architektur und Spielstände dürfen einen Planeten nicht mit der gesamten Welt gleichsetzen.

**CORE-005** Mehrspieler, eine komplette bebaubare Planetenkugel, frei gesteuerte Höhenbewegung im Raum, ein eigenständiges Luftkampfspiel und beliebig große Galaxien gehören nicht zum ersten Lieferumfang.

**CORE-006** Verbindliche KI-, Diplomatie-, Koalitions- und Siegbedingungen stehen in [KI und Diplomatie](./AI_DIPLOMACY.md). Dieses Dokument definiert örtliche Eroberung, aber keinen davon abweichenden Fraktions- oder Szenariosieg.

## 2 Gemeinsame Zeit und Bedienung

**CORE-007** Raumansicht und Oberflächenansicht zeigen dieselbe fortlaufende Spielwelt. Ein Ansichtswechsel erzeugt keine zweite Simulation, keine neuen Einheiten und keine automatische Zeitpause. Auch nicht betrachtete Orte werden weiter simuliert.

**CORE-008** Der Einzelspieler kann jederzeit taktisch pausieren. Während der Pause sind Auswahl, Kamera, Bauplanung und Befehlserteilung möglich; Bewegung, Produktion, Forschung, Schaden, Vertragsfristen und sonstige Spielzeitfortschritte stehen still. Befehle werden beim Fortsetzen regulär geprüft und ausgeführt.

**CORE-009** Auswahlrahmen, Mehrfachauswahl, Kontrollgruppen und kontextabhängige Befehle stehen auf beiden militärischen Ebenen zur Verfügung. Ein sichtbares Befehlsziel und eine verständliche Ablehnungsursache verhindern unklare Aufträge.

**CORE-010** Eine dauerhaft erreichbare Übersicht zeigt Kolonien, Flotten, kritische Transporte und gemeinsame Operationen. Warnungen führen mit einem Klick zum betreffenden Ort, ohne dort automatisch Befehle auszulösen.

**CORE-011** Warnklassen unterscheiden mindestens entdeckte Invasionsvorbereitung, beginnenden Abstieg, Versorgungsengpass, durchbrochene Verteidigung und bedrohte Verwaltung. Die Anzeige beruht auf dem verfügbaren Wissen der eigenen Fraktion.

**CORE-012** Dauerbefehle umfassen mindestens Patrouille, Eskorte, Stellung halten, Ziel schützen und geordneten Rückzug. Automatische Zielwahl und vorab festgelegte Garnisonsaufträge müssen ohne geöffnete Oberflächenansicht funktionieren.

## 3 Sonnensystem und Aufklärung

**SPACE-001** Schiffe bewegen und bekämpfen sich auf einer gemeinsamen zweidimensionalen Spielebene. Eine schräge oder isometrische Darstellung ist zulässig. Die dargestellte Höhe verändert weder Reichweite noch Trefferwahrscheinlichkeit.

**SPACE-002** Jeder Planet besitzt einen Stern als Bezugskörper und eine berechenbare Keplerellipse mit dem Stern in einem Brennpunkt. Ein Mond besitzt eine entsprechende Bahn relativ zu seinem Planeten. Gleiche Zeiten überstreichen gleiche Flächen; für denselben Bezugskörper gilt `T²/a³ = konstant`. Himmelskörper bewegen sich deutlich langsamer als Schiffe. Maßstab und Zeitkonstanten sind Spielparameter nach T-043; keine N-Körper-Simulation. Bestehende Kreisbahnen werden beim Laden positionsstetig als Sonderfall `e=0` übernommen.

**SPACE-003** Flugaufträge zu beweglichen Zielen berechnen erreichbare Treffpunkte. Eskorte und planetare Verteidigung folgen ihrem Bezugsobjekt. Stationsanlagen im Orbit bleiben an ihren zugehörigen Himmelskörper gebunden.

**SPACE-004** Umlaufbahnen sind Orientierung und keine vorgeschriebenen Flugwege. Sternnähe, Asteroidenfelder und Sensornebel können Bewegung, Sicht oder Risiko beeinflussen; ihre Wirkung muss angezeigt werden.

**SPACE-005** Die Karte unterscheidet unbekannte, erkundete und aktuell beobachtete Gebiete. Aktuelle gegnerische Zustände sind nur mit ausreichender Sicht verfügbar. Eine letzte Sichtung zeigt ihren veralteten Kenntnisstand und wird nicht heimlich aktualisiert.

**SPACE-006** Unterschiedliche Welttypen verändern Vorkommen, Baufläche und Produktionsbedingungen. Lebensfreundliche Welten begünstigen Versorgung und Bevölkerung; Gesteinswelten und Monde begünstigen passende Rohstoffgewinnung. Ein Weltname allein erzeugt keine unsichtbaren Produktionsboni.

**SPACE-010** Fels-, Wüsten-, bewachsene, Ozean-, Eis- und Gaswelten sind ausdrücklich gespeicherte Weltmerkmale. Für den festen sonnenähnlichen Stern werden plausible Bahnentfernungen mit Albedo, Wasserverfügbarkeit und Atmosphäre kombiniert; Abstand allein garantiert weder flüssiges Wasser noch Flora. Planetentypen dürfen sichtbare Oberflächen nicht nur umbenennen. Ozeane besitzen gesperrte Wasserfelder; Gaswelten besitzen keinen festen Boden, keine Straßen, Bodenminen, Mauern oder Straßentore.

**ECO-014** Lokale Gasstationslogistik nutzt Frachtdrohnen zwischen Plattformen. Kapazität, Lade-/Entladevorgänge, Rückflüge, Reservierungen, Abbruch, Verlust und Save/Load verwenden dieselbe Warenintegrität wie Bodentransporte. Luftkorridore brauchen keine Straßen und überfliegen Plattformen. Kolonisierung überträgt genau die Besatzung und Ladung des echten Kolonieschiffs. Keine kostenlose atmosphärische Erzquelle; Erz/Metall müssen per Raumfracht eintreffen. Bestehende Energie-/Produktionsrezepte bleiben vorläufig Spielbalancewerte.

**SPACE-011** Manche neue Sternsysteme besitzen einen dünn besetzten Asteroidengürtel in einer freien Bahnregion zwischen äußeren Gesteinswelten und Gasriese. Vorhandensein, Bahnen, Größen und endliche Vorkommen werden aus dem System-Seed erzeugt und gespeichert. Kartierte Positionen sind öffentlich, aktuelle Restbestände und Reservierungen erfordern Aufklärung. Die Kamera und das Laden eines Spielstands erzeugen keine neuen Ressourcen.

**SPACE-012** Aktive Schiffe besitzen physische Körperabstände. Bewegung prüft auch die Strecke innerhalb eines Ticks; Kreuzung und Gegenverkehr dürfen keine Körper durchdringen. Gemeinsame Ziele und Häfen erhalten getrennte freie Ankunftsplätze. Später belegte Ankunftsplätze werden nach anhaltendem Stillstand neu bewertet. Eigene Blockierer können vorübergehend ausweichen, ohne Auftrag oder Fracht zu verlieren; Gegner oder fremde Verbündete werden dafür nicht kommandiert. Eine tatsächlich geschlossene Blockade bleibt stehen. Schiffsbau und Rückkehr aus der Atmosphäre warten auf freien Platz, statt überlappend zu erscheinen oder nochmals zu bezahlen.

**ECO-015** Freie Frachter mit Raumtransporttechnologie können aufgeklärte Asteroiden abbauen. Ein Auftrag reserviert freie Restvorkommen und Lagerplatz am ausgewählten eigenen Raumhafen. Erz wird über Zeit aus dem Vorkommen in die Schiffsladung übertragen und anschließend physisch zurückgeflogen und entladen. Konkurrierende Aufträge, Abbruch, Zerstörung und Eigentumswechsel müssen Bestände und Reservierungen erhalten; verlorene Ladung wird als Verlust verbucht. Schiffsbau bleibt kostenpflichtig, ein Gürtel stellt keinen direkten globalen Warenbonus bereit.

**SPACE-007** Der Startstandort jeder regulären Fraktion enthält eine erreichbare lokale Grundversorgung. Seltene Rohstoffe dürfen Expansion oder Handel erfordern; ihre Verteilung darf die ersten Transporter oder einfache Gegenwehr nicht unmöglich machen.

**SPACE-008** Ein betriebsfähiger Raumhafen fertigt einfache Frachter und Kolonisationsschiffe aus lokal angelieferten Grundgütern. Eine zugeordnete Orbitalwerft fertigt die freigeschalteten Kriegsschiffe; ihr Bau und ihre Produktion benötigen Zeit, Energie und tatsächlich an den Orbitzugang übertragene Materialien. Ein fertiges Schiff erscheint genau einmal am zugehörigen Ausgang. Nicht betrachtete Werften produzieren nach denselben Regeln.

**SPACE-009** Ein Kolonisationsschiff kann auf einem geeigneten, unbesiedelten Himmelskörper an einem zulässigen Standort gründen. Bereits bestehende fremde Kolonien benötigen eine Eroberung oder ausdrückliche Eigentumsübertragung. Bei erfolgreicher Gründung wird das Kolonisationsschiff genau einmal in die deklarierte Startinfrastruktur umgewandelt; seine tatsächlich mitgeführten Güter und Besatzung bilden den Anfangsbestand. Fehlgeschlagene oder wiederholte Gründungsbefehle erzeugen keine zusätzliche Kolonie und verbrauchen das Schiff nicht erneut.

## 4 Kolonie und Oberfläche

**SURFACE-001** Jeder bebaubare Himmelskörper besitzt zunächst genau eine zusammenhängende Kolonieregion. Aufbau und Bodengefecht finden auf derselben Karte mit denselben Gebäuden, Straßen und Vorräten statt. Die Bodenfelder sind regelmäßige Sechsecke in Wabenstruktur. Jedes innere Feld besitzt sechs über gemeinsame Kanten erreichbare Nachbarn; Darstellung, Auswahl, Bau, Straßen und Bodennavigation verwenden dieses Raster. Reichweiten und Sicht beziehen sich auf seine tatsächliche Geometrie.

**SURFACE-002** Die Karte besitzt mehrere glaubwürdige Angriffsoptionen. Gelände beeinflusst Begehbarkeit, Befahrbarkeit, Sicht und Deckung. Eine zufällige Platzierung darf notwendige Zugänge nicht ausschließlich durch unzerstörbare Barrieren sperren.

**SURFACE-003** Grundlegende Gebäudefunktionen sind Verwaltung mit Startlager, Wohnraum, Energie, Nahrung, Bergbau, Metallverarbeitung, Bauteilproduktion, Lager und Raumhafen. Forschung, Elektronik, Kaserne, Fahrzeugwerk und Militärdepot erweitern die Kolonie.

**SURFACE-004** Die Startausrüstung enthält die Materialien und anfänglichen Kapazitäten zum Aufbau der Grundversorgung. Ein ausgeklapptes Koloniemodul stellt Verwaltung, begrenztes Lager und eine anfängliche Arbeitskräftebasis bereit. Eine Kolonie erzeugt keine zusätzlichen Baumaterialien aus dem Nichts.

**SURFACE-005** Straßen verbinden Betriebe und Lager. Automatische Transportfahrzeuge bewegen konkrete Warenmengen; Weglänge, Erreichbarkeit und Transportkapazität begrenzen die Versorgung. Der Spieler verwaltet Verkehrsnetz und Prioritäten, keine einzelnen alltäglichen Fahrten.

**SURFACE-006** Bauaufträge reservieren benötigte Waren, und die Waren müssen die Baustelle erreichen. Fortschritt benötigt ein tatsächlich zugewiesenes, vor Ort angekommenes Baufahrzeug, einen verfügbaren Versorgungsanschluss und Zeit. Eine fertiggestellte Anlage wird erst mit erfüllten Betriebsbedingungen aktiv.

Konkretisierung 1.0.18 nach T-063: Höchstens zwei der vier Kurierkapazitäten einer Kolonie übernehmen einen Baueinsatz; Anfahrt, Arbeit und Rückfahrt belegen dieselbe Kapazität. Damit bleiben mindestens zwei Plätze für Materiallogistik verfügbar. Betriebspriorität und anschließend ältere Kennung bestimmen die Zuweisung freier Fahrzeuge. Noch fehlendes Material bindet kein Baufahrzeug. Nach vollständiger Lieferung fährt es vom Kommandozentrum über den realen Weg zur Baustelle; erst dort wird das Material einmalig verbraucht. Gebäude-Arbeitszeiten sind gegenüber 1.0.17 verdoppelt, bei Nahrungsmangel sinkt die Bauleistung mit der Produktivität. Nach Fertigstellung, Abbruch, Zerstörung oder ungültiger Besitzzuordnung fährt das Fahrzeug tatsächlich zurück; ein unterbrochener Rückweg gibt die Kapazität nicht vorzeitig frei. Gaswelten verwenden ihre vorhandenen Frachtdrohnen ohne Straßen.

Neue reguläre Oberflächen werden gemäß T-061 mit 40 × 32 Waben erzeugt. Waldflächen folgen unregelmäßigen, reproduzierbaren Flecken aus dem System-Seed. Dies ist eine veränderbare Kartengestaltung; gespeicherte Karten und explizit dimensionierte Prüfkarten werden nicht durch eine Darstellungsoption umgebaut.

**SURFACE-007** Baustellen und Gebäude können beschädigt werden. Bau unter Beschuss ist möglich, erzeugt aber weder sofortige Fertigstellung noch Unverwundbarkeit. Abbruch, Abriss und Zerstörung besitzen getrennte, konfigurierbare Rückgewinnungsregeln; kein Vorgang darf mehr Material zurückgeben als eingebracht wurde.

Arbeitsannahme M1/M2: Unverbrauchte Bestände bleiben bei freiwilligem Rückbau vollständig erhalten. Nach Baubeginn beziehungsweise bei Abriss werden je Gut abgerundet 50 % der Baukosten geborgen; Eingaben eines abgebrochenen Produktionszyklus werden vollständig zurückgegeben. Bereits für einen laufenden Minenzyklus entnommenes Erz kehrt dabei ins Vorkommen zurück. Geborgenes Material bleibt zunächst vor Ort und wird physisch abgeholt. Beladene Anlieferungen fahren mit ihrer tatsächlichen Fracht zur Quelle zurück; deren Platz bleibt ab Beladung reserviert. Das Kommandozentrum ist vor freiwilligem Abriss geschützt. M3 ergänzt militärische Zerstörung ohne Baukostenrückgewinnung: örtlicher Bestand geht verloren, bereits geladene Kurierfracht bleibt physisch erhalten. Konkrete M3-Balance und Rekrutierungsrückgewinnung stehen in [M3_NACHWEIS](M3_NACHWEIS.md) und DECISIONS, T-008–T-010.

**SURFACE-008** Energie und Arbeitskräfte werden innerhalb der eigenen angeschlossenen Kolonie verteilt. Fehlende Verbindung oder Kapazität ist sichtbar. Betriebsprioritäten können Verteidigung, Versorgung oder Industrie bevorzugen; begrenzte Energiespeicher puffern kurze Ausfälle.

Arbeitsannahme M1/M2: Versorgung und Betrieb erfordern die Verbindung zum Kommandozentrum. Drei Prioritätsstufen und Ein-/Ausschalten steuern die Zuteilung; Hydroponik beginnt mit hoher Priorität. Das Kommandozentrum speichert bis zu 20, eine Solaranlage bis zu 10 Energieeinheiten. Wirtschaft und Transport verwenden dieselben gespeicherten Zustände auch bei geschlossener Oberflächenansicht.

**SURFACE-009** Eigene und ausdrücklich berechtigte verbündete Einheiten dürfen automatische Tore passieren. Sonstige Durchfahrt und Gebäudenutzung richten sich nach den diplomatischen Rechten.

**SURFACE-010** Bodeneinheiten, aktive Kuriere und Baufahrzeuge teilen eine Kollisionsprüfung mit typabhängigen Körperradien. Routen berücksichtigen bekannte Belegung; tatsächliche Schrittbewegung verhindert Überlappungen auch zwischen Tickpositionen. Eigene Einheiten können eine freie Lücke durch vorübergehendes Platzmachen öffnen, bei Bedarf als lokale Kette. Ursprüngliche Befehle bleiben erhalten. Gelände, Gebäude, Landeflächen, Rechte und fremde Körper werden nicht durch Ausweichen übergangen. Ohne legale freie Ausweichfläche bleibt der Auftrag blockiert. Rekrutierung und Entladung warten auf einen freien Ausgang; eingeladene Truppen existieren bis zum Transfer nur im Transporter.

## 5 Wirtschaft und Waren

**ECO-001** Das Grundspiel besitzt genau sechs interplanetar transportierbare Güterklassen:

| Gut | Quelle oder Rezept | Verwendungszweck |
| --- | --- | --- |
| Nahrung | Landwirtschaft oder Bioreaktoren | Bevölkerung und militärische Versorgung |
| Erz | Lagerstätten und Minen | Metallherstellung |
| Metall | Erzverarbeitung | Bau, Bauteile und Elektronik |
| Bauteile | Metallverarbeitung | Schiffe, Fahrzeuge, Bau, Reparatur und militärischer Nachschub |
| Kristalle | Seltene Lagerstätten | Elektronik und bestimmte Forschungsvoraussetzungen |
| Elektronik | Metall und Kristalle | Fortgeschrittene Anlagen, Einheiten und Forschung |

**ECO-002** Alle sechs Güter besitzen tatsächliche Lagerorte und Mengen. Globale Summen sind Anzeigen, keine frei verfügbaren Reichskonten. Verbrauch ist nur aus lokal erreichbaren Beständen oder aus angelieferter Ladung möglich.

**ECO-003** Energie, Bevölkerung beziehungsweise Arbeitskräfte, Lagerkapazität und örtlicher Munitionsvorrat sind lokale Zustände. Forschungsergebnisse und freigeschaltete Baupläne sind reichsweit verfügbares Wissen. Aktuelle Kartensicht unterliegt weiterhin den Aufklärungsregeln.

**ECO-004** Die Grundversorgung benötigt keine Kristalle oder Elektronik. Einfache Frachter, Koloniemodule, Infanterie, Pioniere, Grundverteidigung und eine einfache Belagerungsmöglichkeit bleiben mit Nahrung, Erz, Metall und Bauteilen erreichbar.

**ECO-005** Ein Produktionszyklus startet nur bei verfügbaren Eingaben und vorgesehenem Ausgaberaum. Eingaben werden beim Start verbindlich entnommen. Fehlende Energie oder Arbeitsleistung pausiert den Zyklus ohne erneuten Verbrauch. Ausgabe erfolgt genau einmal nach Abschluss.

**ECO-006** Waren in einem laufenden Produktionszyklus sind nicht parallel exportierbar. Beim Abbruch gelten die ausgewiesenen Rückgewinnungsregeln. Zerstörung dupliziert weder Eingaben noch Ausgaben.

**ECO-007** Nahrungsmangel stoppt zuerst Bevölkerungswachstum und reduziert anschließend die Produktivität. Eine lokale Mindestfähigkeit zum Wiederaufbau bleibt erhalten; militärische Vernichtung wird dadurch nicht rückgängig gemacht. Konkrete Grenzwerte gehören zur Balancekonfiguration.

Arbeitsannahme M1/M2: Nahrung wird am Kommandozentrum tatsächlich verbraucht, eine Einheit je Bewohner und 120 Simulationssekunden; Hydroponik und Lager müssen sie dorthin liefern. Bei Versorgung und freiem Wohnraum wächst die Bevölkerung alle 45 Sekunden um einen Bewohner. Nach 15 Sekunden anhaltendem Mangel sinkt die Produktivität über weitere 60 Sekunden bis auf 35 %. Die Arbeitskräftebasis beträgt abgerundet 75 % der Bevölkerung, vermindert durch Produktivität, jedoch mindestens vier Personen, soweit vorhanden. Das Kommandozentrum bietet 16, ein Wohnhabitat weitere 12 Plätze. Diese Werte stehen zentral in `Definitions.cs`; Erholung erzeugt keine kostenlosen Lebensmittel oder Baumaterialien.

**ECO-008** Normaler Schiffsflug benötigt im Grundspiel keinen verbrauchbaren Treibstoff. Preise, Kredite, freie Warenbörsen und zusätzliche Spezialrohstoffe sind nicht erforderlich, um die anfängliche Wirtschaftsschleife zu schließen.

## 6 Interplanetare und militärische Logistik

**LOG-001** Raumhäfen bilden den Übergang zwischen Oberfläche und Weltraum. Be- und Entladung sowie der Oberflächentransfer besitzen begrenzte Kapazität und benötigen Zeit. Ein orbitaler Hafenanschluss gehört funktional zum Raumhafen; er schafft kein zweites unabhängiges Warenkonto.

**LOG-002** Der Spieler kann feste Routen und ein automatisch versorgtes Kolonienetz anlegen. Ein Transportauftrag nennt Quelle, Ziel, Waren, Menge, Priorität und Sicherheitsregel. Frachter werden einem Auftrag oder einem gemeinsamen Pool zugeordnet.

**LOG-003** Automatische Versorgung verwendet Mindestbestand, Zielbestand und Exportreserve je Gut und Kolonie. Bereits reservierte und unterwegs befindliche Lieferungen zählen bei der Bedarfsermittlung mit. Exportiert wird nur verfügbare Ware oberhalb der eigenen Reserve.

**LOG-004** Ein gültiger Auftrag reserviert Quellware, Transportkapazität und Zielkapazität. Dieselbe Menge darf nicht mehrfach reserviert werden. Abbruch, Verlust oder ungültig gewordene Rechte lösen betroffene Reservierungen auf und lassen den Bedarf neu bewerten.

**LOG-005** Grundversorgung hat standardmäßig Vorrang vor aktiver Produktion, freigegebenen Bauaufträgen und zusätzlicher Vorratshaltung. Der Spieler kann Prioritäten ausdrücklich ändern. Lange Bauwarteschlangen bestellen nicht automatisch sämtliche später benötigten Waren gleichzeitig.

**LOG-006** Frachter bündeln geeignete Ladung und nutzen mögliche Rückfracht. Nachfüllschwelle und Zielbestand unterscheiden sich, um ständige Kleinstlieferungen zu vermeiden. Ein laufender Auftrag bleibt stabil, solange keine relevante Gefahr oder Ungültigkeit auftritt.

**LOG-007** Sicherheitsregeln erlauben Umweg, Warten auf Eskorte oder ausdrücklich riskanten Durchbruch. Die Logistik kennt nur aufgeklärte Gefahren. Eine Blockade ist die tatsächliche Bedrohung oder Unterbindung von Transporten, kein pauschales Abschalten planetarer Produktion.

**LOG-008** Die Oberfläche meldet Engpässe mit Ursache und betroffenen Folgen, beispielsweise fehlende Produktion, fehlender Frachtraum, kein erreichbarer Weg oder blockierter Hafen. Eine Prognose unterscheidet zugesagte Lieferung von unsicherer Ankunft.

**LOG-009** Militärdepots verwenden Nahrung und Bauteile für Versorgung, Reparatur und örtliche Munition. Munition ist kein siebtes interplanetar handelbares Gut. Transportfahrzeuge beliefern Felddepots und Einheiten über tatsächlich erreichbare Wege.

**LOG-010** Kampfverbände führen begrenzte eigene Vorräte. Eine unterbrochene Lieferung stoppt zunächst Nachfüllen und Reparatur. Eine Waffe ohne notwendige Munition kann nicht feuern. Bewegung und Evakuierung bleiben möglich; Einheiten verschwinden nicht durch fehlende Versorgung.

**LOG-011** Ein Landungstransporter bringt reale Einheiten und begrenzte Anfangsvorräte mit. Ein Depotmodul erlaubt einen Brückenkopf, erzeugt jedoch keine kostenlose Armee oder unendliche Versorgung. Bereits verfügbare lokale Ressourcen können nach Sicherung genutzt werden.

**LOG-012** Zerstörte Transporter verlieren ihre tatsächlich geladene Fracht und ihre noch nicht entladenen Einheiten. Die Auflösung von Reservierungen stellt verlorene Waren nicht wieder her. Bergung ist eine spätere ausdrückliche Erweiterung und darf Verluste nicht stillschweigend zurückbuchen.

**LOG-013** Bei Eroberung oder Sperrung eines Zielhafens wird die Lieferberechtigung neu geprüft. Ein Frachter liefert nicht automatisch an den neuen Gegner. Er wartet, kehrt zurück oder erhält ein zulässiges Ausweichziel; bereits geladene Ware bleibt bis zur tatsächlichen Umladung in seinem Besitz.

**LOG-014** Platzmachen verändert keine Warenmenge, Reservierung, Fahrzeugkennung oder bezahlte Produktion. Bodenkuriere fahren auf der physisch geprüften Straßenkurve; vorübergehendes Ausweichen und Wiedereinordnen müssen Position und Ladung erhalten. Ein Baufahrzeug arbeitet während des Ausweichens nicht weiter. Ankunftsslot und Hafentransfer verwenden bei Spieler und KI dieselbe Dockregel. Warte- und Ausweichzustände setzen sich nach Speichern/Laden identisch fort.

## 7 Forschung und Freischaltung

**RES-001** Forschung besteht aus einem gerichteten, zirkelfreien Abhängigkeitsgraphen. Die fünf Richtungen sind Industrie, Logistik, Raumfahrt und Flotten, Bodentruppen sowie Festungsbau. Voraussetzungen dürfen mehrere Richtungen verbinden.

**RES-002** Grundlegende Ziele und erreichbare nächste Schritte sind sichtbar. Seltene Entdeckungen können zusätzliche Technologien offenlegen. Jede gesperrte sichtbare Technologie zeigt ihre fehlenden Voraussetzungen an.

**RES-003** Forschung benötigt Laborleistung, Energie, Arbeitskräfte und gegebenenfalls Versuchsmaterial. Ein Projekt hat einen ausführenden Forschungsstandort; physische Proben müssen dort tatsächlich eintreffen. Forschungswissen darf reichsweit geteilt werden, Versuchsmaterial nicht teleportiert werden.

**RES-004** Ein begonnener Forschungsfortschritt bleibt bei Laborverlust erhalten, wird aber angehalten. Eine Fortsetzung benötigt einen geeigneten neuen Standort und gegebenenfalls die dort erneut erforderliche Ausstattung. Bereits abgeschlossene Technologien gehen durch örtliche Verluste nicht verloren.

**RES-005** Die ersten Kolonie- und Transporttechnologien benötigen ausschließlich Grundgüter. Eine Forschungsabhängigkeit darf nicht voraussetzen, dass genau die erst freizuschaltende Fähigkeit bereits verfügbar ist.

**RES-006** Eine Freischaltung liefert Baupläne, keine fertigen Gebäude oder Einheiten. Herstellung benötigt passende Produktionsanlagen und lokal verfügbare Waren. Neue Fähigkeiten und sinnvolle Alternativen sind wichtiger als ausschließlich prozentuale Verbesserungen.

**RES-007** Beispiel einer erreichbaren Kette: lokale Metall- und Bauteilwirtschaft, Koloniemodule, Erschließung einer Kristallwelt, Elektronikproduktion, fortgeschrittene Sensoren und spezialisierte Verteidigung. Einfache Gegenmittel gegen Festungen bleiben vor dieser Kette verfügbar.

## 8 Raumkampf und Bodenkampf

**COMBAT-001** Gefechte verwenden reale Einheiten mit Eigentümer, Position, Schaden, Aufklärung und gültigen Befehlen. Sichtprüfung, diplomatische Angriffsrechte und notwendige Betriebsbedingungen gelten auf beiden Ebenen.

**COMBAT-002** Grundlegende Raumrollen sind:

| Rolle | Aufgabe | Begrenzung |
| --- | --- | --- |
| Aufklärer | Sicht und Früherkennung | Geringe Kampfkraft |
| Korvette | Verfolgung und Überfälle auf leichte Ziele | Schwach gegen vorbereitete schwere Verteidigung |
| Eskortfregatte | Schutz von Transporten und Positionen | Höhere Kosten und geringere Beweglichkeit |
| Torpedoschiff | Angriff auf schwere Schiffe und Stationen | Benötigt Schutz gegen leichte Angreifer |
| Frachter oder Landungstransporter | Waren oder entsprechend belegte Truppenplätze transportieren | Begrenzte Kapazität und schwache Selbstverteidigung |
| Kolonisationsschiff | Startausrüstung und Besatzung für eine neue Kolonie transportieren | Wird bei erfolgreicher Gründung verbraucht |

**COMBAT-003** Grundlegende Bodenrollen sind:

| Rolle | Aufgabe | Begrenzung |
| --- | --- | --- |
| Linieninfanterie | Aufklärung, Deckung und Gebäudebesetzung | Verwundbar gegen Flächenfeuer |
| Panzerabwehrtrupp | Abwehr schwerer Fahrzeuge | Braucht Schutz gegen Infanterie |
| Kampfpanzer | Beweglicher Durchbruch und direktes Feuer | Verwundbar gegen vorbereitete Panzerabwehr |
| Mobile Artillerie | Bekämpfung befestigter Stellungen | Braucht Aufklärung, Munition und Begleitschutz |
| Mobile Flugabwehr | Schutz von Vorstößen und Landezonen | Geringe Wirkung gegen schwere Bodenziele |
| Pionier- und Versorgungsfahrzeug | Feldbau, Reparatur und Nachschub | Geringe eigene Kampfkraft |

**COMBAT-004** Kasernen und Fahrzeugwerke benötigen Material, Zeit und lokale Rekrutierungskapazität. Einheiten in Transportern zählen weiter zum Bestand und können nicht gleichzeitig als Bodeneinheiten eingesetzt werden. Entladen erzeugt keine Kopie.

**COMBAT-005** Direkte Waffen benötigen eine freie Schusslinie. Artillerie darf geeignete Hindernisse überwinden, benötigt aber eine aufgeklärte Zielposition. Letzte bekannte Positionen können als Flächenziel dienen, liefern jedoch keine laufende Information über unsichtbare Einheiten.

**COMBAT-006** Infanterie nutzt geeignete Deckung und besetzbare Gebäude. Fahrzeuge benötigen befahrbare Zugänge. Vorteile durch Gelände werden angezeigt und sind datengetrieben; optische Höhe allein erzeugt keinen unbezeichneten Bonus.

**COMBAT-007** Schaden bleibt nach dem Gefecht bestehen. Reparatur benötigt geeignete Einrichtungen oder Pioniere, Zeit und Bauteile. Geordneter Rückzug ist ein eigener Auftrag und löscht keine Gegnerverfolgung oder Weggefahr.

**COMBAT-008** Orbitalbeschuss benötigt ein geeignetes Schiff, eine gültig aufgeklärte Zielposition und einen angekündigten Feuerauftrag. Das Schiff bindet sich dafür vorübergehend an eine angreifbare Position; Feuerhäufigkeit und Verbrauch sind begrenzt.

**COMBAT-009** Lokale Schilde, geeignete Flugkörperabwehr, planetare Geschütze und Entsatzflotten bieten Gegenwehr gegen Orbitalunterstützung. Beschuss kann Anlagen beschädigen oder zerstören, aber keine Eigentumsübernahme auslösen.

## 9 Invasion und örtliche Eroberung

**INV-001** Eine Invasionsoperation besitzt die nachvollziehbaren Phasen Aufklärung, Sammlung, Anflug, Abstieg, Brückenkopf, Bodenoperation und Sicherung. Abbruch und Evakuierung sind mögliche Übergänge. Die Phasen beschreiben tatsächliche Bedingungen und ersetzen keine frei steuerbaren Gefechte.

**INV-002** Örtliche Raumkontrolle senkt das Transportrisiko. Eine vollständige Vernichtung aller feindlichen Schiffe im System ist keine Landebedingung. Verbliebene Gegner können den Anflug und spätere Lieferungen weiterhin angreifen.

Arbeitsannahme ab 1.0.6 zu INV-003: Die Landezone besteht aus einer zentralen Wabe und ihren sechs Nachbarn. Alle sieben müssen frei und ohne aktiven gegnerischen Schild sein; überlappende Landereservierungen sind unzulässig. Vier Truppenplätze bleiben unabhängig von der Flächengröße.

**INV-003** Eine Landezone muss ausreichend groß, begehbar und erreichbar sein. Gebäude, ungeeignetes Gelände und aktive lokale Schutzanlagen können die Auswahl begrenzen. Ein aktiver Flächenschild verhindert direkte Landung innerhalb seines Schutzbereichs; Bodentruppen können von außen heranrücken.

**INV-004** Ein Transporter befindet sich genau an einem Ort: im Raum, in der Übergangsphase oder an seiner Landezone. Sichtbare Abstiegs- und Aufstiegsphasen sind angreifbar. Truppen werden erst nach erfolgreichem Entladen auf der Oberfläche aktiv.

**INV-005** Alle Invasionswellen sind produzierte und transportierte Verbände. Verstärkungen entstehen aus Produktions-, Flug- und Landekapazität. Es gibt keine kostenlosen, von der gegnerischen Wirtschaft unabhängigen Standardwellen.

**INV-006** Ein Brückenkopf benötigt eine gesicherte Landefläche, entladene Pioniere beziehungsweise ein Depotmodul und Startvorräte. Weitere Transporte sind für Ersatz und längere Operationen erforderlich. Der Verlust des Orbits entfernt bereits gelandete Einheiten nicht.

**INV-007** Die planetare Verwaltungszentrale wird durch geeignete Infanterie physisch besetzt. Die Sicherung benötigt eine kurze lokale Dauer und wird durch gegnerische Kräfte im definierten Sicherungsbereich unterbrochen. Eine globale Eroberungsuhr ohne Bodenkontrolle ist unzulässig.

**INV-008** Der strategische Besitz eines Planeten wechselt, wenn Verwaltungszentrale und ein erreichbarer Versorgungspunkt der angreifenden Operation gesichert sind. Versorgungspunkt ist entweder ein eigener Landekopf oder ein nutzbarer eroberter Raumhafen. Bei einer gemeinsamen Operation dürfen verschiedene Teilnehmer die Einrichtungen sichern, sofern gültige Zugangs- und Versorgungsrechte ihre Nutzung durch den vereinbarten neuen Eigentümer erlauben. Der strategische Besitz folgt dieser Vereinbarung; Einheiten und Feldanlagen der Partner werden dadurch nicht automatisch übertragen.

**INV-009** Zivile Anlagen gesicherter Bezirke können übernommen werden; aktive militärische Anlagen benötigen eigene Ausschaltung oder Besetzung. Verbleibende gegnerische Einheiten bleiben bestehen. Eine einzelne außerhalb des Sicherungsbereichs versteckte Einheit blockiert den Besitzwechsel nicht.

**INV-010** Eroberte Lager behalten ihren tatsächlichen Warenbestand. Anlagen benötigen nach Sicherung gegebenenfalls Reparatur und gültige Versorgung. Zerstörte notwendige Kontrollanlagen hinterlassen einen reparierbaren Standort, damit Beschuss keine dauerhaft uneroberbare Kolonie erzeugt.

**INV-011** Evakuierung benötigt erreichbare Landezonen, verfügbare Transportplätze und eine tatsächlich mögliche Abholung. Einheiten ohne Abholweg bleiben vor Ort. Rückzug, Kapitulation und endgültige Fraktionsniederlage sind voneinander getrennte Vorgänge.

## 10 Tower Defense und Verteidigungsanlagen

**DEF-001** Tower Defense entsteht aus Aufklärung, vorbereiteten Feuerzonen, Engstellen, gestaffelten Landungen und mobilen Reserven. Angreifer sind nicht verpflichtet, eine fest vorgeschriebene Route zu benutzen.

**DEF-002** Mauern und Tore sind zerstörbar. Vollständig geschlossene Verteidigungsringe bleiben durch Beschuss oder andere gültige Angriffsoptionen überwindbar. Wegfindung muss blockierte Wege erkennen, statt Einheiten endlos an dieselbe Stelle zu schicken.

**DEF-003** Vier grundlegende Geschützrollen und zwei spätere Schutzrollen sind vorgesehen:

| Anlage | Hauptwirkung | Gegenmittel |
| --- | --- | --- |
| Schnellfeuerturm | Leichte Infanterie und Fahrzeuge | Panzerung und größere Reichweite |
| Panzerabwehrstellung | Schwere Fahrzeuge | Infanterie, Artillerie und Zielüberlastung |
| Flugabwehrstellung | Landungstransporter und geeignete Flugkörper | Bodenangriff und andere Anflugkorridore |
| Artilleriestellung | Entfernte oder konzentrierte Bodenziele | Gegenaufklärung und schneller Durchbruch |
| Schildgenerator | Lokaler Schutz gegen Orbitalfeuer | Energieausfall oder Angriff am Boden |
| Planetare Abwehrbatterie | Schiffe im markierten nahen Orbitalbereich | Bodentruppen, Gegenbeschuss und Flottenkoordination |

**DEF-004** Gewöhnliche Bodenflugabwehr wirkt auf Transporter während definierter planetarer Übergangsphasen. Sie trifft keine beliebigen Schiffe auf der Systemkarte. Planetare Abwehrbatterien besitzen einen gesonderten, sichtbaren Orbitalbereich.

**DEF-005** Türme benötigen die ausgewiesenen Ressourcen, Energie beziehungsweise Personal und gegebenenfalls Munition. Kosten, Baufläche, Betrieb, Versorgung und fehlende Beweglichkeit begrenzen Festungen; ein pauschales Turmlimit ist im Grundspiel nicht erforderlich.

**DEF-006** Speicher und Depots puffern kurze Versorgungsausfälle. Ein Energieverlust darf weder unbemerkt ignoriert werden noch jede angeschlossene Anlage ohne Anzeige dauerhaft zerstören. Prioritäten und verbleibende Vorräte sind sichtbar.

**DEF-007** Feldanlagen benötigen eine versorgte Bauquelle oder einen ausgerüsteten Pionier, freie Baufläche, angeliefertes Material und Bauzeit. Eine Baustelle blockiert Gelände entsprechend ihrem tatsächlichen Bauzustand und ist angreifbar.

**DEF-008** Verteidigungsgruppen können einen Zugang halten, ein Depot schützen oder bei einem konfigurierten Verlustzustand auf eine Rückzugslinie wechseln. Türme wählen gültige Ziele nach ihrer Rolle; mobile Reserven reagieren nur innerhalb ihrer erteilten Aufträge.

## 11 Balanceparameter und Nachweis

Regeln mit Anforderungs-ID sind fachliche Invarianten. Die folgenden Werte sind getrennt konfigurierbar und müssen im Prototyp erprobt werden:

| Parametergruppe | Vorläufiges Ziel |
| --- | --- |
| Orbitgeschwindigkeit | Strategische Weglängen verändern sich allmählich; Flugaufträge bleiben verständlich |
| Anfangswirtschaft | Grundversorgung, Ersatzfrachter und einfache Gegenwehr sind lokal erreichbar |
| Vorräte und Lieferzeiten | Unterbrechungen erzeugen ein sichtbares Reaktionsfenster |
| Landung | Aufgeklärte Anflüge erlauben eine sinnvolle Reaktion vor vollständigem Entladen |
| Türme und Belagerung | Vorbereitete Stellungen sind effizient; passende mobile Gegenmittel bleiben wirtschaftlich erreichbar |
| Sicherungsdauer | Kurze physische Besetzung genügt nicht zum überraschenden Gesamtwechsel ohne Gegenwehr |
| Szenarioumfang | Mehrere Fraktionen sind spielbar, ohne ständig jede Kolonie einzeln steuern zu müssen |

Konkrete Kosten, Mengen, Reichweiten, Schadenswerte, Sekunden und Kartengrößen werden beim jeweiligen Umsetzungspaket festgelegt und als vorläufige Daten dokumentiert. Sie dürfen ohne Änderung der grundlegenden Regeln angepasst werden.

Technische Zuständigkeiten stehen in [Technische Architektur](./TECHNICAL_ARCHITECTURE.md). Umsetzungspakete und überprüfbare Abnahmekriterien stehen im [Umsetzungsplan](./IMPLEMENTATION_PLAN.md). Neue Funktionen gelten erst nach dem dort festgelegten Nachweis als abgeschlossen.

## 12 Fachliche Grenzen der ersten Fassung

Die folgenden Abgrenzungen verhindern, dass ein Umsetzungspaket unbemerkt weitere Spielsysteme voraussetzt:

- Eine Kolonieregion besitzt eine planetare Verwaltungszentrale; zusätzliche Verwaltungsgebäude dürfen die Eroberungsbedingung nicht vervielfachen.
- Eine Fraktion besitzt mehrere lokale Warenbestände, aber keine sofort nutzbare globale Materialreserve.
- Ein aufgeklärter Feindkontakt ist kein dauerhafter Sensor und keine Erlaubnis zur Kenntnis verdeckter Produktionspläne.
- Eine verbündete Fraktion ist kein zusätzlicher frei steuerbarer Teil der eigenen Armee; Weisungs- und Nutzungsrechte folgen der Diplomatiespezifikation.
- Ein unterbrochener Import ist ein wirtschaftlicher Engpass, keine pauschale Niederlage und kein automatisches Abschalten aller Gebäude.
- Ein besetzter Planet, eine ausgeschiedene Fraktion und ein gewonnenes Szenario sind drei getrennte Zustände.

## Ergänzende Verkehrs- und Befehlsregeln für 1.0.18

**LOG-015:** Bodenstraßen besitzen Rechtsverkehr auf zwei getrennten Richtungsfahrspuren mit gerundeten Kurven und getrennten Gebäudeeinfahrten. Gegenverkehr fährt aneinander vorbei. Fahrzeuge warten vor konfliktierenden Kreuzungsfahrten; tatsächliche Ladungen, Baustellen und Fahrzeuge bleiben erhalten. Gasfracht nutzt weiterhin Fluggeräte ohne Straßen.

**SURFACE-011:** Je ein Panzer kann auf zwei benachbarten Waben stehen. Bewegte Einheiten nutzen eine freie Umfahrung, bevor sie stehende eigene Einheiten zum Platzmachen veranlassen. Aktive Baufahrzeuge bleiben während der Arbeit unbeweglich; andere Einheiten umfahren die Arbeitsstelle oder warten. Abschluss und Abbruch erlauben erst wieder die reguläre Rückfahrt.

**GUI-022:** Aus Mausbefehlen entstehen transparente bogenförmige Zielpfeile, grün für Bewegung und rot für Angriff. Sie folgen dem bewegten Akteur und sichtbaren Zielen, verschwinden bei Ankunft bzw. tatsächlichem Angriff und werden bei Befehlsersatz entfernt. Sie markieren den Auftrag, keine ballistische Berechnung des Waffenfluges und keine garantierte direkte Fahrstrecke.

## LOG-016 Versorgungsnetz und sichtbarer Warenfluss

LOG-015 wird in der Darstellung durch ein Versorgungsnetz ersetzt: keine dauerhaften Straßenflächen, größere Bodenfahrzeuge mit getrennten Richtungsfahrkorridoren. Verbindungen werden mit Start-/Zielauswahl vollständig geplant; der zweite Endpunkt darf ein zukünftiger Bauplatz sein. Ein fehlgeschlagener Auftrag verändert keine Teilstrecke. Waren fahren weiterhin physisch zwischen erreichbaren Lagern und Gebäuden. Eine Gebäudeauswahl zeigt relevante Lieferungen, die zuschaltbare Logistikansicht das ganze eigene Netz samt Richtung, Fracht und Wartezustand. Gaswelten nutzen weiterhin Frachtdrohnen.

### LOG-017 / GUI-023 / SURFACE-012 – aktuelle Ergänzung

Transportverbindungen erscheinen als schmale, gerundete Straßenmarkierungen. Endpunktplanung und optionale Logistikansicht bleiben erhalten. Angriffsbefehle führen zur erreichbaren Schussposition und anschließend zum Beschuss. Zielbögen sind transparenter und nur bei ausgewählten Akteuren sichtbar. Mauern und Tore verbinden benachbarte Waben zu schmalen zusammenhängenden Befestigungen, einschließlich Ecken und umlaufender Anlagen (D-049–051, TEST-042).
Bei Bodenbefehlen nach SURFACE-012 fahren Einheiten freie Strecken direkt. Vor einem Richtungswechsel folgen sie innerhalb der Wegpunktwabe einem Bogen mit tangentialem Ein- und Auslauf. Jede Kette hinterlässt Abdrücke entlang ihres eigenen Wegs. Energiebalken folgen der interpolierten Modellposition. Bei den vier in Mauern integrierbaren Geschütztürmen trägt ein fester Schaft den Drehkranz über der Mauerkrone.

### LOG-018 – Freie Wege und Nutzungsspuren

Auf festem Boden können Kuriere und Baufahrzeuge ohne Straßenausbau zwischen erreichbaren Gebäuden fahren. Wiederholte Fahrten verstärken sichtbare Spuren entlang der tatsächlich befahrenen Wabenkanten. Ungenutzte Spuren verblassen. Eine Spur reserviert kein Feld und gewährt keine Bau- oder Lieferberechtigung. Auf Gasplaneten nutzt die Kolonie Luftlogistik. Diese Regel ersetzt die Straßenplanung aus LOG-017. Lagerzugriff und Baukosten richten sich weiterhin nach tatsächlich verfügbaren und erreichbaren Gütern.

Eine Gebäudeausfahrt gibt den nächsten Transporter 0,30 Simulationssekunden nach der letzten tatsächlichen Ausfahrt frei. Das gilt auch bei der Rückfahrt nach dem Entladen. Das Zeitfenster wird mit dem Gebäude gespeichert. Ausfahrten an anderen Gebäuden arbeiten unabhängig. Unterwegs bleibt der Abstand zwischen Kurieren eine Darstellungsaufgabe.

## ECO-009 · Atmosphärische Rohstoffgewinnung

Jede Gasriesenkolonie teilt sich einen beim Erzeugen des Systems bestimmten Fördervorrat von 1600 bis 2400 Einheiten Wasserstoff. Ein Gasförderer kostet 16 Metall und 4 Bauteile und benötigt im Betrieb 3 Energie sowie 2 Arbeitskräfte. Bei voller Produktivität liefert ein sechssekündiger Zyklus 2 Einheiten. Beim Start werden sie dem Gasband entnommen und zwei Plätze im Stationsspeicher reserviert. Erst mit Abschluss wird die Charge verfügbar. Rückbau gibt eine unfertige Charge ins Gasband zurück.

Der Stationsspeicher fasst 24 Waren. Bei fehlendem Platz wartet der nächste Zyklus. Reguläre Frachtdrohnen bedienen erreichbare Lager und den konfigurierten Bedarf des Raumhafens. Ein Raumhafen auf einem Gasriesen fordert zunächst 8 Wasserstoff an. Hafenbestand, Exportreserve, Schiffsladung und Handelslieferungen folgen den gemeinsamen Warenregeln.

### LOG-019 · Heimatfahrzeuge und Depotreserve

Jedes fertiggestellte Gebäude besitzt einen eigenen Transporter. Das Kontrollzentrum stellt zusätzlich drei Reservefahrzeuge, ein Lager ein weiteres. Zuerst fährt der Transporter des Versenders. Ist er gebunden, kann der Empfänger die Ware abholen. Sind beide beschäftigt, hilft eine erreichbare Depotreserve. Abholfahrt, Verladung, Lieferung und Rückkehr brauchen tatsächlich Zeit. Material bleibt bis zum Ende der Verladung in der Quelle reserviert. Der Fuhrpark einschließlich Fahrzeugkennungen und Heimatstandorten wird mitgespeichert. Bis zu zwei Fahrzeuge des Kontrollzentrums dürfen gleichzeitig Bauaufträge übernehmen.

Beim Rückbau wechseln die Fahrzeuge des Gebäudes in ein erreichbares eigenes Depot. Ein Transporter, der dort sein Rückbaumaterial ablädt, beendet seine Fahrt. Auf Baustellen bleibt das Modell beim Warenumschlag sichtbar. F3 blendet Typ- und Ladehinweise auf der Oberfläche ein oder aus.

### Bodenverteidigung und Instandsetzung

Eine Toröffnung verbindet zwei gegenüberliegende Hexkanten. Benachbarte eigene Mauern, Tore und Türme bestimmen die Ausrichtung. Geschütztürme schließen an die Mauer an und behalten ihren unabhängigen Geschützaufsatz. Eigene Fußtruppen können beim Bewegen zu einer Wabe drei getrennte Standplätze belegen. Weitere Gruppenmitglieder suchen ein Nachbarfeld.

Panzer bekämpfen auch während eines Marschbefehls sichtbare Gegner in Reichweite. Nach dem Gefecht schwenkt ihr Turm während der Fahrt nach vorn. Bei höchstens 75, 50 und 25 Prozent Struktur sinken Bewegungstempo beziehungsweise Produktionsgeschwindigkeit auf 90, 75 und 55 Prozent. Fußtruppen zeigen einen Verband und einen zunehmend asymmetrischen Gang. Fahrzeuge und Gebäude zeigen je nach Zustand Blechschäden, Funken, Rauch und Feuer.

Die unbewaffnete mobile Werkstatt kostet 10 Metall, 8 Bauteile, 2 Nahrung und bindet 2 Bewohner. Vier der bezahlten Bauteile gehen als Ersatzteile an Bord. Im Stand wählt sie im Umkreis von zwei Feldabständen das anteilig am stärksten beschädigte eigene Fahrzeug. Ein Bauteil repariert bis zu 16 Strukturpunkte. Das Militärdepot füllt ihren Vorrat auf maximal acht Teile. Für gezielte Gebäudereparaturen gibt es den Reparaturbefehl. Ein blinkender Schraubenschlüssel zeigt den Arbeitsort an.
