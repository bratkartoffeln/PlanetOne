# PlanetOne Spielregeln

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

**SPACE-002** Jeder Planet besitzt einen Stern als Bezugskörper und eine vorgegebene Umlaufbahn. Ein Mond besitzt einen Planeten als Bezugskörper. Die Bahnen sind berechenbar und die Himmelskörper bewegen sich deutlich langsamer als Schiffe.

**SPACE-003** Flugaufträge zu beweglichen Zielen berechnen erreichbare Treffpunkte. Eskorte und planetare Verteidigung folgen ihrem Bezugsobjekt. Stationsanlagen im Orbit bleiben an ihren zugehörigen Himmelskörper gebunden.

**SPACE-004** Umlaufbahnen sind Orientierung und keine vorgeschriebenen Flugwege. Sternnähe, Asteroidenfelder und Sensornebel können Bewegung, Sicht oder Risiko beeinflussen; ihre Wirkung muss angezeigt werden.

**SPACE-005** Die Karte unterscheidet unbekannte, erkundete und aktuell beobachtete Gebiete. Aktuelle gegnerische Zustände sind nur mit ausreichender Sicht verfügbar. Eine letzte Sichtung zeigt ihren veralteten Kenntnisstand und wird nicht heimlich aktualisiert.

**SPACE-006** Unterschiedliche Welttypen verändern Vorkommen, Baufläche und Produktionsbedingungen. Lebensfreundliche Welten begünstigen Versorgung und Bevölkerung; Gesteinswelten und Monde begünstigen passende Rohstoffgewinnung. Ein Weltname allein erzeugt keine unsichtbaren Produktionsboni.

**SPACE-007** Der Startstandort jeder regulären Fraktion enthält eine erreichbare lokale Grundversorgung. Seltene Rohstoffe dürfen Expansion oder Handel erfordern; ihre Verteilung darf die ersten Transporter oder einfache Gegenwehr nicht unmöglich machen.

**SPACE-008** Ein betriebsfähiger Raumhafen fertigt einfache Frachter und Kolonisationsschiffe aus lokal angelieferten Grundgütern. Eine zugeordnete Orbitalwerft fertigt die freigeschalteten Kriegsschiffe; ihr Bau und ihre Produktion benötigen Zeit, Energie und tatsächlich an den Orbitzugang übertragene Materialien. Ein fertiges Schiff erscheint genau einmal am zugehörigen Ausgang. Nicht betrachtete Werften produzieren nach denselben Regeln.

**SPACE-009** Ein Kolonisationsschiff kann auf einem geeigneten, unbesiedelten Himmelskörper an einem zulässigen Standort gründen. Bereits bestehende fremde Kolonien benötigen eine Eroberung oder ausdrückliche Eigentumsübertragung. Bei erfolgreicher Gründung wird das Kolonisationsschiff genau einmal in die deklarierte Startinfrastruktur umgewandelt; seine tatsächlich mitgeführten Güter und Besatzung bilden den Anfangsbestand. Fehlgeschlagene oder wiederholte Gründungsbefehle erzeugen keine zusätzliche Kolonie und verbrauchen das Schiff nicht erneut.

## 4 Kolonie und Oberfläche

**SURFACE-001** Jeder bebaubare Himmelskörper besitzt zunächst genau eine zusammenhängende Kolonieregion. Aufbau und Bodengefecht finden auf derselben Karte mit denselben Gebäuden, Straßen und Vorräten statt.

**SURFACE-002** Die Karte besitzt mehrere glaubwürdige Angriffsoptionen. Gelände beeinflusst Begehbarkeit, Befahrbarkeit, Sicht und Deckung. Eine zufällige Platzierung darf notwendige Zugänge nicht ausschließlich durch unzerstörbare Barrieren sperren.

**SURFACE-003** Grundlegende Gebäudefunktionen sind Verwaltung mit Startlager, Wohnraum, Energie, Nahrung, Bergbau, Metallverarbeitung, Bauteilproduktion, Lager und Raumhafen. Forschung, Elektronik, Kaserne, Fahrzeugwerk und Militärdepot erweitern die Kolonie.

**SURFACE-004** Die Startausrüstung enthält die Materialien und anfänglichen Kapazitäten zum Aufbau der Grundversorgung. Ein ausgeklapptes Koloniemodul stellt Verwaltung, begrenztes Lager und eine anfängliche Arbeitskräftebasis bereit. Eine Kolonie erzeugt keine zusätzlichen Baumaterialien aus dem Nichts.

**SURFACE-005** Straßen verbinden Betriebe und Lager. Automatische Transportfahrzeuge bewegen konkrete Warenmengen; Weglänge, Erreichbarkeit und Transportkapazität begrenzen die Versorgung. Der Spieler verwaltet Verkehrsnetz und Prioritäten, keine einzelnen alltäglichen Fahrten.

**SURFACE-006** Bauaufträge reservieren benötigte Waren, und die Waren müssen die Baustelle erreichen. Fortschritt benötigt Baukapazität und Zeit. Eine fertiggestellte Anlage wird erst mit erfüllten Betriebsbedingungen aktiv.

Arbeitsannahme M1/M2: Pro Kolonie können zwei angeschlossene Baustellen zugleich fortschreiten. Betriebspriorität und anschließend die ältere Kennung bestimmen die Reihenfolge. Noch fehlendes Material belegt keinen Bauplatz; Material wird erst beim tatsächlichen Baubeginn verbraucht. Die Bauleistung sinkt bei Nahrungsmangel mit der Produktivität.

**SURFACE-007** Baustellen und Gebäude können beschädigt werden. Bau unter Beschuss ist möglich, erzeugt aber weder sofortige Fertigstellung noch Unverwundbarkeit. Abbruch, Abriss und Zerstörung besitzen getrennte, konfigurierbare Rückgewinnungsregeln; kein Vorgang darf mehr Material zurückgeben als eingebracht wurde.

Arbeitsannahme M1/M2: Unverbrauchte Bestände bleiben bei freiwilligem Rückbau vollständig erhalten. Nach Baubeginn beziehungsweise bei Abriss werden je Gut abgerundet 50 % der Baukosten geborgen; Eingaben eines abgebrochenen Produktionszyklus werden vollständig zurückgegeben. Bereits für einen laufenden Minenzyklus entnommenes Erz kehrt dabei ins Vorkommen zurück. Geborgenes Material bleibt zunächst vor Ort und wird physisch abgeholt. Beladene Anlieferungen fahren mit ihrer tatsächlichen Fracht zur Quelle zurück; deren Platz bleibt ab Beladung reserviert. Das Kommandozentrum ist vor freiwilligem Abriss geschützt. M3 ergänzt militärische Zerstörung ohne Baukostenrückgewinnung: örtlicher Bestand geht verloren, bereits geladene Kurierfracht bleibt physisch erhalten. Konkrete M3-Balance und Rekrutierungsrückgewinnung stehen in [M3_NACHWEIS](M3_NACHWEIS.md) und DECISIONS, T-008–T-010.

**SURFACE-008** Energie und Arbeitskräfte werden innerhalb der eigenen angeschlossenen Kolonie verteilt. Fehlende Verbindung oder Kapazität ist sichtbar. Betriebsprioritäten können Verteidigung, Versorgung oder Industrie bevorzugen; begrenzte Energiespeicher puffern kurze Ausfälle.

Arbeitsannahme M1/M2: Versorgung und Betrieb erfordern die Verbindung zum Kommandozentrum. Drei Prioritätsstufen und Ein-/Ausschalten steuern die Zuteilung; Hydroponik beginnt mit hoher Priorität. Das Kommandozentrum speichert bis zu 20, eine Solaranlage bis zu 10 Energieeinheiten. Wirtschaft und Transport verwenden dieselben gespeicherten Zustände auch bei geschlossener Oberflächenansicht.

**SURFACE-009** Eigene und ausdrücklich berechtigte verbündete Einheiten dürfen automatische Tore passieren. Sonstige Durchfahrt und Gebäudenutzung richten sich nach den diplomatischen Rechten.

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
