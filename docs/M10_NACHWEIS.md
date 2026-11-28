# M10 · Kolonielogistik und Referenzpartie

Produktversion 1.0.19, Paket P23, Regel LOG-018, Abnahme TEST-043.

## Verhalten im Spiel

Eine Solaranlage auf einer freien, erreichbaren Wabe beauftragen. Die Kolonie reserviert die Baukosten im erreichbaren Lager. Ein Kurier holt die Ware dort ab. Unterwegs lässt sich die Lieferung in der Logistikansicht (L) verfolgen. Nach Ankunft des Materials arbeitet ein Baufahrzeug an der Baustelle. Regelmäßig genutzte Strecken hinterlassen kräftigere Spuren. Auf beiden Seiten einer Wabengrenze zählt der jeweils dort gefahrene Weganteil. Liegt ausreichend Material in einem abgeschnittenen Lager, nennt die Bauplatzrückmeldung die fehlende Menge und den Grund „Lager nicht erreichbar“.

## Ergebnisse unter Windows

- Alle 225 Core- und Plattformprüfungen bestanden. Sie prüfen unter anderem abgeschnittene Lager, Unterbrechung einer beladenen Fahrt, Übergabe der Güter, exakte Fortsetzung nach dem Laden und das Verblassen der Spuren.
- Zusätzlich wurde Slot 1 bei Tick 1039 in eine separate Simulation geladen. Das Fahrzeugwerk hatte keinen offenen Auftrag. Ein regulär bestellter Panzer erhöhte den Bestand innerhalb von 270 Ticks von drei auf vier. Der Originalspielstand blieb erhalten.
- Im regulären Referenzszenario mit Seed 8026 gewann der Einzelspieler bei Tick 81737. Er bezahlte zuerst die Eskorte und erklärte danach den Krieg. Die Koalition 1 und 3 gewann bei Tick 44555 nach physischer Lieferung von acht Einheiten Handelsware gegen zwei Einheiten Bezahlung. Die fünf industriellen Rohstoffbilanzen wurden in beiden Partien abgeglichen.
- Die Invasions-KI berücksichtigt beim Anflug Abstand zum Koloniezentrum und bekannte Verteidigung. Auf der vorbereiteten Prüfkarte landete ihr Verband und eroberte das Ziel über die gemeinsamen Spielbefehle.
- Unity 6000.3.29f1 baute den Windows-x64-Player. Im separaten Testdesktop bei 1440 × 900 waren die Bodenspuren, die Logistikansicht, der beladene Kurier und die fertiggestellte Baustelle nachweisbar. Der Renderlauf verwendet einen eigenen Speicherpfad.
- Der vollständige Lauf mit dem F05-Siegspielstand bestand 1138 Einzelprüfungen und erzeugte 207 Renderbilder. In Layout B prüft der Lauf die Flottenbefehle in der unteren Leiste. Eine im Testgelände unterbrochene Fahrt behielt beim Speichern und Laden ihre Fracht und konnte sie nach Öffnung der Passage abliefern.

## Wiederholen

Im Repository-Stamm unter Windows PowerShell ausführen:

```powershell
.\tools\Version.ps1 -Action Check
.\tools\Test-Core.ps1
.\tools\Unity.ps1 -Action Build
.\tools\Test-Presentation.ps1 -PlayerPath Builds/Windows/PlanetOne.exe -SupplyNetworkOnly
.\tools\Test-Presentation.ps1 -PlayerPath Builds/Windows/PlanetOne.exe -ReferenceSave TestResults/Reference/victory.json
```

Das Core-Protokoll steht unter `TestResults/Core/results.txt`. Jeder Playerlauf legt Bilder, Log und Prüfergebnis in einem eigenen Verzeichnis unter `TestResults/Presentation` ab. Die gezielte Prüfung bedient die Spieloberfläche über deren Controller. Physische Mausbedienung und weitere Zielplattformen waren nicht Teil dieses Laufs.
