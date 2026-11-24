# M5: Raumgefechte, Landung und örtliche Eroberung

Stand: 24. November 2026, Produktversion **1.0.5**, Speicherformat **5**. P11–P13 sind implementiert und lokal automatisiert geprüft. Der Git-Abschluss ist bis zu einem erfolgreichen verpflichtenden GitHub-Unity-Build offen. Eine vollständige manuelle Maus-/Tastaturabnahme wird nicht behauptet.

## Umfang und Bedienung

Die Systemübersicht bietet **M5 Invasion prüfen**. Diese getrennte, pausierte Prüfkarte enthält ausdrücklich vorbereitete, endliche Testbestände: zwei besiedelte Welten und einen neutralen Mond, eine eigene Orbitalwerft, Eskortfregatte, Torpedoschiff und einen Landungstransporter mit zwei Infanterieeinheiten, einem Pionier und einer Artillerie. Der Transporter trägt zwölf Nahrung und zwölf Bauteile. Nereid besitzt eine gegnerische Korvette, Infanterie, Panzer, Flak, Geschützstellung, Mauerabschnitte und Depot. Die vier transportierten Einheiten werden über die normalen Ladebefehle aus der Ausgangsoberfläche entfernt. Dies ist keine regulär finanzierte Startpartie und keine Fraktions-KI.

Der normale Spielstart erhält keine zusätzlichen Flotten, Technologien oder Vorräte. Militärschiffe entstehen regulär in einer Orbitalwerft, die zunächst ein Raumhafen mit Material, Zeit und Besatzung baut. Die Werft folgt ihrem Planeten. Ihr Schiffsauftrag fordert Material vom Hafen an; örtliche Kuriere beliefern den Hafen, der je Übertragungsschritt eine Ware an die Werft weitergibt. Die Werft verbraucht die tatsächlich vorhandenen Waren und Besatzung und baut genau ein Schiff nach Ablauf seiner Produktionszeit.

Im Bereich **Raumfahrt und Forschung → Krieg**:

1. Schiff und Zielwelt auswählen. **Anflug / Sammlung am Ziel** führt zu einer beweglichen Welt. Im Flottenreiter können mehrere Schiffe dem zuerst ausgewählten Schiff Eskorte geben.
2. Aufgeklärte Feinde angreifen, zur Heimatwelt zurückziehen oder eine eigene Werft für Reparatur und Nachmunitionierung wählen. Diese benötigen Zeit und Werftbauteile. Nahbereiche um Welten sind als Ringe dargestellt; ein ferner Gegner blockiert keine ganze Operation.
3. Träger zuerst zum nahen Zielorbit bringen. Bodenkoordinaten eingeben und **Abstieg** wählen. Für die Prüfkarte ist **4 / 9** ein alternativer Landeort abseits des Flakbereichs. Eine freie 2×2-Fläche ist erforderlich. Frieden ohne Stationierungsrecht und ein aktiver örtlicher Schild verhindern die feindliche Direktlandung.
4. Nach dem acht Sekunden dauernden Abstieg einzelne Truppen entladen. Die Bodentruppen behalten ID, Schaden, Munition und Vorräte. Eine volle Fläche muss zunächst freigemacht werden. Flak trifft Träger während Auf-/Abstieg; Schiffe im freien Raum sind keine Flakziele. Gelandete Träger können durch Bodenwaffen angegriffen werden.
5. **Zieloberfläche öffnen**, dort **Militär und Einheiten** nutzen. Ein Pionier errichtet mit vier mitgeführten Bauteilen, einer Nahrung und sechs Sekunden vor Ort einen Landekopf. Ein naher gelandeter Träger liefert seine tatsächlichen Nahrung-/Bauteilbestände. Der Landekopf versorgt nahe Verbände und bleibt angreifbar; ohne weitere Lieferungen sind seine Vorräte endlich.
6. Infanterie unmittelbar an eine feindliche Anlage bewegen und **Sichern** wählen. Acht Sekunden ununterbrochene örtliche Sicherung sind erforderlich. Feindliche Bodentruppen im Sicherungsradius setzen den Fortschritt zurück. Die Verwaltung überträgt den strategischen Planetenbesitz erst mit einem erreichbaren gesicherten eigenen Landekopf oder nutzbaren eroberten Hafen mit Nahrung.
7. Andere Anlagen behalten zunächst ihren bisherigen örtlichen Eigentümer und müssen separat gesichert werden. Überlebende Verteidiger bleiben bestehen. Lagerbestände werden übernommen, nicht neu erzeugt. Grenzüberschreitende laufende örtliche Lieferungen werden abgebrochen beziehungsweise mit realer Ladung zurückgeführt. Fremde Anlagen können nicht allein aufgrund strategischen Planetenbesitzes ausgeschaltet oder abgerissen werden.
8. Für Evakuierung Truppen tatsächlich neben einen gelandeten Träger bewegen und **Auswahl … verladen** verwenden. Anschließend **Aufstieg / Evakuierung** und Raumrückzug. Einheiten ohne erreichbaren Abholpunkt bleiben zurück.

Torpedoschiffe können im nahen Orbit nach aktueller Bodenaufklärung einen angekündigten Feuerauftrag vorbereiten. Sie bleiben während sechs Sekunden an ihrer angreifbaren Position gebunden; der Schuss kostet zwei Munition. Schilde verhindern Wirkung in ihrem aktiven lokalen Radius. Planetare Abwehrbatterien bekämpfen Schiffe ausschließlich im angezeigten nahen Orbit und verbrauchen Munition aus realen Bauteilen. Beschuss überträgt kein Eigentum. Eine zerstörte Verwaltung bleibt als Standort bestehen und kann durch einen Pionier mit vier Bauteilen und sechs Sekunden Reparaturzeit wiederhergestellt werden.

## Vorläufige Daten

Zahlen stehen in `Definitions`, `SpaceState` und `InvasionState`, nicht in der Darstellung. Vier Truppenplätze pro Landungstransporter; 24 Warenplätze; acht Sekunden Auf-/Abstieg; acht Sekunden Sicherung bei 2,5 Feldern Gegenwehrbereich; drei Raumeinheiten naher Orbit; drei Felder Schildradius. Direkte Raumwaffen besitzen je Rolle Schaden, Reichweite, Nachladezeit, begrenzte Munition und Hülle. Torpedoschiffe sind gegen leichte Raumziele schwächer. Die Grundwerft und ersten Kriegsschiffe benötigen keine importierten Kristalle.

## Ausgeführte Prüfungen

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
```

**74 Core-Tests bestanden**, davon 16 neue M5-Verhaltensprüfungen. Sie prüfen bezahlte Werftproduktion, Hafentransfer, Kriegs-/Sichtgrenzen, Escort-Reaktion, Rückzug, Reparaturverbrauch, reale Ladungsverluste, vier Truppenidentitäten mit Teilentladung und anschließendem Trägerverlust, Landeberechtigung, Schildgrenze, Flakwirkung, Versorgung als Eroberungsvoraussetzung, fortbestehenden Widerstand, angekündigtes Orbitalfeuer, Save/Load während Abstieg und Wiederaufbau, Ablehnung doppelter Einheiten-IDs, V4-Migration, vollständige Invasion, Evakuierungsnähe sowie unterbrochene Gebäudesicherung.

Der Windows-Release-Build mit Unity **6000.3.29f1** ist erfolgreich. Der automatisierte grafische Ablauf in `PrototypeSmokeRunner` prüft zusätzlich den tatsächlichen Player und die Eingabeadapter: ursprüngliche Wirtschaft, M3, M4 und der vollständige M5-Ablauf von Anflug über Raumgefecht und angreifbaren Abstieg bis versorgter Eroberung und Speicherfortsetzung.

Letzter vollständiger Grafiklauf: **103 Einzelprüfungen**, 14 erzeugte Renderbilder, `TestResults/Smoke/20261124-091107`. Die M5-Bilder wurden als Dateien visuell kontrolliert. Zwei dabei gefundene Lesbarkeitsprobleme wurden behoben: abgeschnittener Reitername und überlappende Bodentruppenbeschriftungen. Der Test verwendet die regulären Controllerbefehle, keine vorgetäuschten UI-Erfolge.

Szenariozuordnung: TEST-004/005 (Raumkampf und Verlust), TEST-006 (kristallfreie Grundgegenwehr), TEST-008 (Transportidentität), TEST-009 (Landung/Flak/Abholung), TEST-010 (Besetzung/Bestand/Widerstand), TEST-020 (Speicherfortsetzung). Fachregeln insbesondere COMBAT-001–009, INV-001–011 und DEF-003–007. Gemeinsame diplomatische Operationsrechte bleiben P16/P17.

## Testumgebung und Grenzen

Der Nutzer verlangt Tests ohne Screenreader und anschließend ausdrücklich ohne Computer-Use-Skill sowie ohne Störung des laufenden PCs. Die weitere Abnahme erfolgt deshalb durch Core-Tests, fensterlose Batch-Builds, automatisierte Spielabläufe und Prüfung vorhandener Bilddateien. Ein zuvor begonnener direkter UI-Test wurde beendet; er ist kein vollständiger Bedienungsnachweis. Nach der letzten Vorgabe werden keine sichtbaren Playerfenster oder Eingaben auf dem Arbeitsdesktop gestartet. Die Prozessprüfung fand keinen verbliebenen PlanetOne-Testplayer.

Der bisherige Grafiklauf wurde vor dieser letzten Einschränkung mit `tools/Test-Prototype.ps1 -Visible` ausgeführt. Dieser Befehl darf künftig nur in einer isolierten Testsitzung laufen, beispielsweise in einer dedizierten, headless gestarteten VirtualBox-VM. VirtualBox ist vorhanden; eine VM wurde für diesen Abschluss nicht angelegt, da die nötigen Rendernachweise bereits vorlagen. Keine Gastinstallation, Grafikbeschleunigung oder VM-Abnahme wird als ausgeführt behauptet.

Offen bleiben die vollständige manuelle Bedienungsabnahme, weitere Plattformen und der Remote-Unity-Nachweis. Die CI-Einrichtung aus PR #4 ist auf diesem abhängigen Versionsbranch enthalten. Solange dessen erforderliche Unity-Secrets fehlen, verhindert **PR validation** den Merge auch für M5; der Branchschutz wird nicht umgangen. M6-KI, M7-Diplomatie und eine vollständige Partie sind nicht Teil dieser Lieferung.
