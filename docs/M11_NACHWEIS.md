# M11 · Atmosphärische Rohstoffgewinnung

Version 1.0.20, Paket P24, ECO-009 und TEST-044.

## Selbst spielen

In „Gegen einen Computer“ Zephyr erkunden und mit einem ausgerüsteten Kolonieschiff besiedeln. Für einen kurzen Einstieg lässt sich unter **Prüfkarten → Planetentypen und Luftfracht** eine bestehende Kolonie öffnen. Auf Zephyr unter **Bauen → Industrie → Gasförderer** die Station platzieren. Der Bau kostet 16 Metall und 4 Bauteile. Für den Betrieb werden 3 Energie und 2 Arbeitskräfte benötigt. Der Stationsinspektor zeigt die im Gasband verbleibende förderbare Menge.

Am Raumhafen unter **Versorgung → Wasserstoff** den lokalen Zielbestand und die Exportreserve einstellen. Neue Gasriesenhäfen fordern acht Einheiten an. Einen Frachter beauftragen, um die eingelagerte Ware zu einem zweiten Hafen zu liefern. Der Bestand in der oberen Leiste berücksichtigt die für ausgehende Transporte reservierte Menge.

## Abnahme

Alle 227 Core- und Plattformtests bestanden. Die reguläre F05-Einzelpartie erreichte den Sieg bei Tick 81737, die Koalitionspartie bei Tick 44555. Der bestehende Spielstand aus Slot 1 bei Tick 1039 ließ sich separat als Schema 14 laden und mit identischem Zustand erneut einlesen.

- In einer regulär gegründeten Gasriesenkolonie wurden Solaranlage, Förderer, Lager und Raumhafen bezahlt und gebaut. Eine gespeicherte beladene Drohne erreichte nach dem Laden denselben vollständigen Simulationszustand. Die Förderbilanz umfasst das verbleibende Gasband, die laufende Charge, Gebäudeinventare und bewegte Fracht.
- Der Raumhafen erhielt acht Einheiten. Bei Exportreserve drei lieferte der Frachter genau fünf an den Zielhafen. Bei vollem Stationsspeicher blieben Vorrat und Produktion konstant. Nach Freigabe zweier Plätze ließ sich die letzte Charge genau einmal fördern.
- Der isolierte Windows-Player auf Unity 6000.3.29f1 baute eine sichtbare Station auf Zephyr und prüfte die acht gelieferten Einheiten im Raumhafen. Das Bild `gas-hydrogen-extraction.png` im jeweiligen Testlauf zeigt die ausgewählte Station und die Ressourcenleiste.
- Speicherformat 14 ergänzt die siebte Warenposition. Beim Laden älterer Formate erhalten vorhandene Inventare und Frachträume für Wasserstoff den Anfangsbestand null. Das natürliche Gasband entsteht auch auf bestehenden Gasriesen. Die Migrationstests vergleichen die übrigen Waren und laufenden Aufträge.

## Reproduzieren

```powershell
.\tools\Test-Core.ps1
.\tools\Unity.ps1 -Action Build
.\tools\Test-Presentation.ps1 -ReferenceSave TestResults/Reference/victory.json
```

Die Core-Ergebnisse stehen unter `TestResults/Core/results.txt`, Bilder und Playerprotokoll unter `TestResults/Presentation`. Die Playerprüfung verwendet einen eigenen Testdesktop und einen eigenen Speicherpfad.
