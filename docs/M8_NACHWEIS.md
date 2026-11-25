# M8 – Gesamtszenario, Speichern und Windows-Export

Produktversion **1.0.15**, 25. November 2026. Betrifft P19–P21, ARCH-014 bis ARCH-016, TEST-020 bis TEST-022 und TEST-031; Entscheidungen D-035 und T-052 bis T-054.

## Umsetzung

**Spielmenü → Referenzpartie · Drei Kolonien** startet F05: Aster, Nereid und Helios, die Kristallmonde Khepri und Selene, ein Spieler und zwei wirtschaftlich unabhängige KI. Jede Startkolonie besitzt dasselbe endliche Budget. Forschung, Flotten, Truppen und Waren entstehen ausschließlich über normale Kosten, Produktion und Transporte. F05 verwendet das bestehende Speicherformat 11. Alte Partien behalten ihre Welten, Kennungen und Vorräte.

**Sicherung laden** stellt ausdrücklich die letzte gültige `.bak` wieder her. Der neue Unity-unabhängige Dateiadapter validiert vor dem Schreiben, synchronisiert eine temporäre Datei im gleichen Verzeichnis und ersetzt den Hauptstand atomar. Beschädigte Hauptstände überschreiben die gültige Sicherung nicht. Eine fehlgeschlagene oder nicht unterstützte Ersetzung lässt den alten Stand unangetastet. Es gibt keine automatische Rückkehr zu einem unbemerkt älteren Spielstand. Leere, übergroße und unbekannte Formate werden abgelehnt.

Beim Laden wird eine zuvor geöffnete Oberfläche nur beibehalten, wenn sie im neuen Stand bekannt ist. Andernfalls öffnet sich das Sonnensystem; die alte Oberfläche bleibt nicht unter einer neuen Simulation sichtbar. F5, F9, Leertaste, Escape und Tab verwenden einen gemeinsamen Tastaturverteiler mit denselben Aktionen wie die GUI. Modale Einstellungen blockieren Spielaktionen.

Nach dem Torerfolg bleibt das Ergebnis mit den Namen der tatsächlichen Sieger in der Statusanzeige sichtbar, auch nach dem Laden und in beiden Ansichten. Kurzzeitige Bedienmeldungen dürfen es vorübergehend ersetzen; anschließend erscheint es wieder. Der Koalitionssieg nennt beide Fraktionen.

Der vollständige Durchlauf fand und korrigierte weitere Integrationsfehler:

- Bauvorschau und Ausführung berücksichtigen ausschließlich Lager mit einem tatsächlichen Weg zur Baustelle. Ein erreichbares Kommandozentrum genügt nicht, wenn das reservierte Material in einem abgetrennten Lager liegt.
- Bei einer offenen Handelsverpflichtung stellt die KI einen eigenen Frachter nach dessen laufender Auslieferung frei. Automatische Versorgung darf ihn nicht vor der zugesagten Lieferung erneut belegen. Vorhandene Ware wird nicht gelöscht und laufende Transporte werden nicht abgekürzt.
- Ein bereits entschiedenes Szenario nimmt keine neue Toraktivierung an, die anschließend ohne Fortschritt stehen bliebe.
- Die KI bestimmt erreichbare Bauplätze mit zwei Flutungen der Wegenetze statt wiederholter vollständiger Pfadsuchen je Kandidat. Gewählter Bauplatz, Sichtregeln und tatsächliche Wegeprüfung bleiben erhalten. Die Optimierung folgte einer gemessenen Spitze im KI-Anteil.

## Reproduzierbare Prüfung

```powershell
powershell -ExecutionPolicy Bypass -File tools/Test-Core.ps1
powershell -ExecutionPolicy Bypass -File tools/Version.ps1 -Action Check
powershell -ExecutionPolicy Bypass -File tools/Unity.ps1 -Action Build
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -ReferenceSave TestResults/Reference/victory.json
powershell -ExecutionPolicy Bypass -File tools/Test-Presentation.ps1 -Width 1280 -Height 720 -ReferenceSave TestResults/Reference/cooperative-victory.json
```

`Test-Core.ps1 -Suite Reference` wiederholt nur F05. `-Suite Combat` setzt für die gezielte Fehlersuche den zuvor regulär erzeugten Stand `TestResults/Reference/military-ready.json` voraus; dieser Teillauf ersetzt nicht den vollständigen Nachweis. Der Gesamtaufruf erzeugt seine Ausgangszustände neu. Protokolle, JSON-Zwischenstände und Builds liegen unter ignorierten lokalen Verzeichnissen.

Dateiprüfungen vergleichen nicht nur JSON im Speicher: Sie schreiben und lesen echte Dateien. Geprüft werden Frachtbeladung und Flug, Truppenabstieg, laufende Gebäudebesetzung, Vertragskündigungsfrist und Toraktivierung. Vollständiger Zustand und Fortsetzung müssen bitgenau dieselbe JSON-Repräsentation ergeben, einschließlich Sichtwissen, ausstehenden Befehlen und exklusiven KI-Zuweisungen. Zusätzliche Fehlerfälle: gesperrtes Ziel, ungültiger neuer Zustand, beschädigter Hauptstand bei gültiger Sicherung und fehlende Wege zu einem Materiallager.

Die isolierten Invasions-Übergangstests verwenden ausdrücklich benannte, endliche Prüf-Fixtures. Sie werden nicht als wirtschaftlich regulärer F05-Durchlauf ausgegeben.

## Zwei reguläre F05-Verläufe

Seed **8026**; Anfang je Fraktion: 16 Einwohner, 60 Nahrung, 100 Metall, 40 Bauteile, ein Kommandozentrum. Zwei KI verwenden weiterhin ausschließlich ihre erlaubten Beobachtungen und die gemeinsame Befehlsprüfung.

Der erste Verlauf baut Erzgewinnung, drei Schmelzen, Bauteilproduktion, Energie, Wohnraum, Labor und Raumhafen auf. Ein priorisierter, bezahlter Kolonietransport besiedelt Khepri. Vereinbarte zivile Transitrechte erlauben die Frachtroute durch fremde geschützte Orbits. Ein automatischer Frachter bringt die wirklich geförderten Kristalle nach Aster; Elektronik und Sensoren werden mit angelieferten Forschungsproben freigeschaltet.

Anschließend folgen ein erklärter Krieg mit Helios, bezahlte Werft und Eskorte sowie Depot, Geschütz- und Artilleriestellung auf Aster. Die KI beschafft ihren Transporter, lädt reale Truppen und landet nach einem Raumgefecht auf Aster. Der Spieler verliert seine Flotte einschließlich des Frachters; die Kolonie bleibt wirtschaftlich handlungsfähig. Nach einem ausgehandelten Waffenstillstand und erneuten Transitrechten baut sie einen neuen Frachter, der den Kristalltransport wieder aufnimmt. Regulär errichtetes und versorgtes Sprungtor, Sicherungsfrist und Alleinsieg schließen den Verlauf ab.

Der zweite Verlauf verzweigt aus demselben regulär aufgebauten Stand vor dem Krieg. Fraktionen 1 und 3 schließen eine Koalition. Acht Metall und zwei Bauteile als Gegenleistung werden mit realen Frachtern tatsächlich geliefert. Der automatische Kristalltransport läuft danach wieder an. Das Tor wird mit festgelegten Teilnehmern aktiviert; beide Koalitionsmitglieder gewinnen, Fraktion 2 erhält keinen Sieg. KI–KI-Verhandlungen sind zusätzlich im Vertragshistoriennachweis enthalten.

Globale Bilanzen für Erz, Metall, Bauteile, Kristalle und Elektronik entsprechen nach beiden Verläufen den endlichen Startvorräten plus tatsächlicher Produktion, Verbrauch, Rückgewinnung und Verlust. Unterwegs befindliche Ware wird einmal gezählt; noch nicht abgeholte Reservierungen werden nicht als zusätzliche Ladung gewertet. Nahrung und ausgegebene Truppenrationen bleiben in ihren vorhandenen Verbrauchsregeln und werden bei Save/Load vollständig verglichen.

## Leistung und Windows-Paket

Das vorab festgelegte Profil ist in T-052 dokumentiert. Der Windows-Player misst die ersten 5.000 Simulationstakte und weitere 5.000 Takte des aufgebauten Referenzstands. CSV-Zeiten trennen Wirtschaft, Logistik, Bodeneinheiten, Raumfahrt, Invasion, KI und Zielauswertung. Die Messung ist optional und verändert keine Spielstände. Nach 60 Aufwärmbildern werden pro Ansicht 360 Bildintervalle gemessen, während die Simulation für diesen getrennten Darstellungsanteil pausiert. Bildratenziel: 60; VSync: 1. Dies ersetzt keine Messung beliebig großer Schlachten.

Hardware: Intel Core i7-13700, 32.530 MiB gemeldeter Arbeitsspeicher, NVIDIA RTX 3060 Ti mit 8.024 MiB Grafikspeicher, Windows 11 10.0.26300, Direct3D 11. Der tatsächliche Prozess-Arbeitssatz wird über `GetProcessMemoryInfo` gemessen; der in Unity/Mono beobachtete Nullwert von `Process.WorkingSet64` wurde nicht als Speichernachweis akzeptiert.

Für lokale Pakete wird derselbe Inno-Setup-Compiler **6.4.3** wie in CI verwendet. Das signierte Original wurde [aus dem offiziellen Release](https://github.com/jrsoftware/issrc/releases/tag/is-6_4_3) bezogen, Authenticode geprüft und mit dem [offiziellen portablen Modus](https://jrsoftware.org/inno-setup/documentation/topic_technotes.htm) ausschließlich unter `TestResults/Toolchains/InnoSetup-6.4.3` eingerichtet. Download-SHA256: `f3c42116542c4cc57263c5ba6c4feabfc49fe771f2f98a79d2f7628b8762723b`.

```powershell
powershell -ExecutionPolicy Bypass -File tools/Package-Windows.ps1 -CompilerPath TestResults/Toolchains/InnoSetup-6.4.3/ISCC.exe
powershell -ExecutionPolicy Bypass -File tools/Test-Installer.ps1 -RunPlayer -ReferenceSave TestResults/Reference/victory.json
```

Der Installertest verwendet ein eindeutiges Projekt-Unterverzeichnis und `/TESTINSTALL=1`: keine Verknüpfungen, keine Registrierung als installierte Anwendung, keine Änderung einer vorhandenen Nutzerinstallation. ZIP und installierte Dateien werden über Hashwerte verglichen. Der installierte Player läuft auf einem eigenen, niemals aktivierten Windows-Desktop, danach wird die Testinstallation wieder deinstalliert. Keine VM oder Bildschirmumschaltung ist erforderlich.

## Prüfstatus und Grenzen

Die abschließenden Ergebnisse und Belegbilder werden nach den laufenden Gesamtprüfungen ergänzt. Eine vollständig physische Maus-/Tastaturprüfung, subjektive Spielspaßbewertung sowie weitere Plattformen sind nicht nachgewiesen. Entsprechend der Nutzeranweisung wurden weder Screenreader noch Computer-Use verwendet.

Die Git-Integration bleibt vom erforderlichen GitHub-Unity-Check abhängig. Vor M8 fehlten die Unity-Aktivierungssecrets. Während der Arbeit wurde `origin/main` extern bis 1.0.12 (`ece80b4`) weitergeführt; der Versionsbranch wird vor dem PR regulär damit zusammengeführt. Kein Admin-Merge und kein Überspringen des Branchschutzes. Der lokale Implementierungs- und Prüfstand ist von dieser Integrationsgrenze zu unterscheiden.
