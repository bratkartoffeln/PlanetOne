# Grafikquellen

## SurfaceAtlas.png

Pfad: `Assets/PlanetOne/Runtime/Resources/SurfaceAtlas.png`. Erzeugt am 24. November 2026 mit dem eingebauten Imagegen-Werkzeug; kein CLI-Fallback, keine übernommenen Referenzspielbilder. Die vier Felder werden über den Shader ausgewählt. Spiegelnde Wiederholung und ein kleiner UV-Rand reduzieren Naht- und Atlasüberläufe. Die im Prompt gewünschte Auflösung ist eine Vorgabe an den Generator, keine Zusicherung über die gelieferte Dateigröße.

Vollständiger Generierungsprompt:

> Use case: stylized-concept. Production texture asset for original sci-fi strategy game PlanetOne, not a mockup. Square 2048x2048 texture atlas, EXACT 2 columns by 2 rows equally sized touching quadrants with NO GAPS, NO BORDERS, NO LETTERS, NO LABELS, NO objects, NO perspective, NO drop shadows. Each quadrant is a flat top-down material albedo sample at consistent neutral diffuse lighting suitable for tiling at runtime. TOP LEFT: natural warm gray ochre rocky dust regolith, fine gravel and restrained cracks, not huge boulders. TOP RIGHT: dark gunmetal spaceship hull plates, restrained seams, tiny rivets, brushed steel grain, no text no rust. BOTTOM LEFT: cool light gray industrial ceramic and concrete pavement plates with fine wear, mild panel seams. BOTTOM RIGHT: deep blue photovoltaic solar cells, fine silver regular conductor lines and dark navy crystalline glass microstructure. All four materials moderately bright neutral albedo, not baked dark lighting, high-quality stylized physically plausible strategy-game materials with readable low frequency structure and detailed fine texture. Each quadrant independently seamless at edges as much as possible. Entire image completely filled by the four flat materials.

## Projektgenerierte Grafik

`PrototypeArtwork.cs` erstellt Katalogbilder aus derselben Modellroutine wie die Spielwelt. `PrototypePresentation.cs` erzeugt die GUI-Mikrotexturen, `PrototypeHudIcons.cs` die einheitlichen Liniensymbole. Seit 1.0.11 verwendet die Systemansicht `CelestialSurface.shader` für die Planetentexturen, `AtmosphereGlow.shader` für Atmosphäre/Korona und `SpaceBackdrop.shader` für den Nebelhintergrund. Die prozeduralen Stern- und Kristallmaterialien bleiben in `PlanetOneSurface.shader`. Sicht- und Besitzanzeigen bleiben kontrastreiche Markierungen.

Die Layoutstudie im Chat ist ein Vorschauartefakt und wird nicht als Laufzeitgrafik ausgeliefert. Recherche und konkrete Umsetzung: [GUI_GRAFIK_NACHWEIS](GUI_GRAFIK_NACHWEIS.md).

## Modellreferenzen für 1.0.10

- `GUI_REFERENCE.png`: ursprüngliche, im Chat erzeugte Layout-/Grafiktafel mit den drei Ansichten; verbindliche visuelle Zielreferenz nach D-029.
- `ColonyModelReference.png`: mit Imagegen am 24. November 2026 erzeugte transparente Gebäudestudie, vier mal vier Ansichten. Inhaltliche Vorgaben: weiß-graue Keramik, Metall, cyanfarbene Fenster, industrielle orange Akzente, Zentrale, Farmkuppeln, Solarzellen, Industrie, Häfen und militärische Anlagen entsprechend der ursprünglichen Tafel.
- `SpaceModelReference.png`: mit Imagegen am selben Tag erzeugte transparente Raumfahrtstudie, vier mal zwei Ansichten. Inhaltliche Vorgaben: eigene PlanetOne-Schiffe, detaillierte Rümpfe, Brücken, Frachtmodule und blaue Triebwerke, ergänzt um Himmelskörper in der Materialsprache der Zieltafel.

Diese beiden zusätzlichen Tafeln sind Modellreferenzen, keine im Player verwendeten Sprites. Die vollständigen ursprünglichen Generierungsprompts dieser beiden Studien sind im Repository nicht archiviert; die obige Beschreibung ist eine Zusammenfassung. Laufzeitmodelle entstehen als echte Meshes in `PrototypeModels3D.cs` und `PrototypeModelGeometry.cs`; Glas nutzt `HabitatGlass.shader`, der unbeleuchtete Weltraumhintergrund `SpaceBackdrop.shader`. Keine Grafikdateien der genannten Referenzspiele wurden übernommen.

## Laufzeitgrafiken für 1.0.11

Beide PNGs wurden am 24. November 2026 mit dem eingebauten Imagegen-Werkzeug erzeugt, anschließend visuell geprüft und in den Unity-Player eingebunden. Kein CLI-Fallback, keine übernommenen Grafikdateien der Referenzspiele. Die Auflösungen im Prompt sind Generatorvorgaben; Unity importiert mit maximal 2048 Pixeln. Die Planeten bleiben dreidimensionale Kugeln, auf die der Atlas als Oberflächentextur gelegt wird. Der Nebel enthält keine vorgerenderten Spielobjekte.

### CelestialAtlas.png

Gespeichert unter [Assets/PlanetOne/Runtime/Resources/CelestialAtlas.png](../Assets/PlanetOne/Runtime/Resources/CelestialAtlas.png). Vier Kartenstreifen: Gesteinsplanet, Eisplanet, Gasplanet und Kratermond. Vollständiger Generierungsprompt:

> Use case: stylized-concept. Production albedo texture atlas for a real 3D space strategy game, PlanetOne. Exactly four equally tall horizontal strips stacked vertically with NO GAPS and NO BORDER. Each strip is an independent seamless-in-longitude 2:1 equirectangular planet surface map. Canvas overall ratio 1:2, target 2048x4096. TOP strip: warm ochre rocky alien terrestrial planet, large rust sandstone continents, charcoal basalt mountain ranges, scattered small deep teal seas, wispy white clouds, highly detailed fine geographical texture; restrained realistic colors. SECOND strip: blue ice planet, pale arctic ice, navy fissures, soft blue clouds, geological detail. THIRD strip: elegant gas giant surface map with flowing horizontal tan, cream, rust and plum bands and storm vortices. BOTTOM strip: cratered warm grey rocky moon with mare basins and overlapping impact craters. All strips completely filled with flat map data. Neutral uniform diffuse illumination, NO baked planetary sphere, NO light direction, NO terminator, NO black space, NO stars, NO perspective, NO UI, NO text, NO labels. Designed to wrap onto real rotating 3D spheres. Detailed science fiction realism similar to polished strategy game concept art.

### NebulaBackdrop.png

Gespeichert unter [Assets/PlanetOne/Runtime/Resources/NebulaBackdrop.png](../Assets/PlanetOne/Runtime/Resources/NebulaBackdrop.png). Vollständiger Generierungsprompt:

> Use case: stylized-concept. Production full-frame background texture for PlanetOne, an elegant realistic science-fiction space strategy game. Wide 16:9, target 2560x1440. Deep almost-black navy outer space with thousands of delicate pinprick stars at varied brightness and very fine dusty blue-violet nebula wisps. Dramatic but restrained luminous cobalt and violet molecular cloud filaments concentrated in the upper right quarter and lower left corner, winding faintly diagonally. Most of the center and lower middle remains very dark clear negative space for readable 3D planets and ships to be rendered on top. Fine astrophotographic detail, no single bright focal object. NO planets, NO moons, NO sun, NO ships, NO orbits, NO UI, NO words, NO borders, NO watermarks, NO large star lens flares. Original game asset, atmospheric sophisticated space opera, not colorful fantasy smoke, not uniform flat gradient.
