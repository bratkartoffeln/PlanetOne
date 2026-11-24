# Grafikquellen

## SurfaceAtlas.png

Pfad: `Assets/PlanetOne/Runtime/Resources/SurfaceAtlas.png`. Erzeugt am 24. November 2026 mit dem eingebauten Imagegen-Werkzeug; kein CLI-Fallback, keine übernommenen Referenzspielbilder. Die vier Felder werden über den Shader ausgewählt. Spiegelnde Wiederholung und ein kleiner UV-Rand reduzieren Naht- und Atlasüberläufe. Die im Prompt gewünschte Auflösung ist eine Vorgabe an den Generator, keine Zusicherung über die gelieferte Dateigröße.

Vollständiger Generierungsprompt:

> Use case: stylized-concept. Production texture asset for original sci-fi strategy game PlanetOne, not a mockup. Square 2048x2048 texture atlas, EXACT 2 columns by 2 rows equally sized touching quadrants with NO GAPS, NO BORDERS, NO LETTERS, NO LABELS, NO objects, NO perspective, NO drop shadows. Each quadrant is a flat top-down material albedo sample at consistent neutral diffuse lighting suitable for tiling at runtime. TOP LEFT: natural warm gray ochre rocky dust regolith, fine gravel and restrained cracks, not huge boulders. TOP RIGHT: dark gunmetal spaceship hull plates, restrained seams, tiny rivets, brushed steel grain, no text no rust. BOTTOM LEFT: cool light gray industrial ceramic and concrete pavement plates with fine wear, mild panel seams. BOTTOM RIGHT: deep blue photovoltaic solar cells, fine silver regular conductor lines and dark navy crystalline glass microstructure. All four materials moderately bright neutral albedo, not baked dark lighting, high-quality stylized physically plausible strategy-game materials with readable low frequency structure and detailed fine texture. Each quadrant independently seamless at edges as much as possible. Entire image completely filled by the four flat materials.

## Projektgenerierte Grafik

`PrototypeArtwork.cs` erstellt Katalogbilder aus derselben Modellroutine wie die Spielwelt. `PrototypePresentation.cs` erzeugt die GUI-Mikrotexturen. `PlanetOneSurface.shader` enthält die prozeduralen Planeten-, Mond-, Stern-, Kristall- und Nebeloberflächen. Sicht- und Besitzanzeigen bleiben bewusst einfache kontrastreiche Markierungen.

Die Layoutstudie im Chat ist ein Vorschauartefakt und wird nicht als Laufzeitgrafik ausgeliefert. Recherche und konkrete Umsetzung: [GUI_GRAFIK_NACHWEIS](GUI_GRAFIK_NACHWEIS.md).

## Modellreferenzen für 1.0.10

- `GUI_REFERENCE.png`: ursprüngliche, im Chat erzeugte Layout-/Grafiktafel mit den drei Ansichten; verbindliche visuelle Zielreferenz nach D-029.
- `ColonyModelReference.png`: mit Imagegen am 24. November 2026 erzeugte transparente Gebäudestudie, vier mal vier Ansichten. Inhaltliche Vorgaben: weiß-graue Keramik, Metall, cyanfarbene Fenster, industrielle orange Akzente, Zentrale, Farmkuppeln, Solarzellen, Industrie, Häfen und militärische Anlagen entsprechend der ursprünglichen Tafel.
- `SpaceModelReference.png`: mit Imagegen am selben Tag erzeugte transparente Raumfahrtstudie, vier mal zwei Ansichten. Inhaltliche Vorgaben: eigene PlanetOne-Schiffe, detaillierte Rümpfe, Brücken, Frachtmodule und blaue Triebwerke, ergänzt um Himmelskörper in der Materialsprache der Zieltafel.

Diese beiden zusätzlichen Tafeln sind Modellreferenzen, keine im Player verwendeten Sprites. Die vollständigen ursprünglichen Generierungsprompts dieser beiden Studien sind im Repository nicht archiviert; die obige Beschreibung ist eine Zusammenfassung. Laufzeitmodelle entstehen als echte Meshes in `PrototypeModels3D.cs` und `PrototypeModelGeometry.cs`; Glas nutzt `HabitatGlass.shader`, der unbeleuchtete Weltraumhintergrund `SpaceBackdrop.shader`. Keine Grafikdateien der genannten Referenzspiele wurden übernommen.
