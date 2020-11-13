# Berekenen van de as van het spoor adhv de opgemeten rails

Berekenen van de spooras aan de hand van twee opgemeten rails (as-rail, binnenkant-rail).

## De verkregen bestanden

Als voorbeeld hebben we een AutoCAD bestand gekregen van de lijn 60 (Hier -> [OW17 L60](link.com "TODO: inser link"))

### Wat vinden we in dat bestand?

In de laag `binnenkant rail` vinden we vier samengestelde lijnen *(aka enkele 3D Polylines)* met op elke vertex een xyz-waarde. De lijnen zijn niet samengevoegd, dus bestaan uit meerdere delen. Dit moeten we als eerste zien op te lossen.

## Workflow

### JOIN (AutoCAD commando)

Omdat alle polylijnen mooi aaneensluitend zijn, kunnen we gebruik maken van het AutoCAD commando `join`. Was dit niet het geval dan zou dit met dat commando niet lukken. Om de polylijnen in één enkelvoudige beweging te kunnen joinen, zorg ik ervoor dat enkel de laag van de lijnen `Unlocked` staat.

#### Richting

Na het samenvoegen van de lijnen controleren we of dat de richting van de polylijnen aan elkaar gelijk zijn.
Ook zorgen we ervoor dat de algemene richting de kilometrering van de hoofdlijn volgt.

#### Vertexen

We controleren ook of per spoor beide rail-lijnen **evenveel vertexen hebben**. Hier in ons voorbeeld is dit niet het geval. Na een visuele controle merken we op dat dit komt doordat er vertexen zijn die door de opmeter zijn toegevoegd. **Deze vallen niet op effectief opgemeten punten. Indien we deze verwijderen zullen we eenzelfde aantal punten bekomen voor elke polyijn van de rail.**

### IAMTopo\_CleanNonSurveyVertexFromPolyline

Dit commando zal elk punt dat zich niet op een echt meetpunt bevindt, verwijderen uit de lijst met punten, en een nieuwe lijn creëren. We selecteren eerst een opmeetpunt, en daarna de polylijn die we wensen op te kuisen.
Na een controle merken we dat de 4-lijnen van de rails evenveel vertexen hebben.
We zijn nu klaar om over te gaan naar het berekenen van het center.
Om de origineel aangeleverde lijnen niet te wijzigen, heb ik de nieuwe lijnen verplaats naar een nieuwe laag. Laagnaam: `Binnenkant Rail Verwijderde Punten`

#### IAMTopo\_Rails2RailwayCenterLine

Met het commando `IAMTopo_Rails2RailwayCenterLine` kunnen we nu het center tussen de twee lijnen berekenen.

Ook dient er een correctie te gebeuren om de scheefstand van de bolprismas te corigeren. Het algoritm houdt enkel rekening met *de verplaatsing in xy richting*, de verplaatsing van de z-waarde werd reeds door de opperator van de waarde afgetrokken. Om er voor te zorgen dat er een correctie wordt berekend moeten we de omgevingsvariabel `Rails2RailwayCenterLine_Use_CalculateSurveyCorrection`op de waarde `True`staat in het instellingen paneel.

Het instellingen paneel kunnen we oproepen aan de hand van het commando `IAMTopo_Settings`. Terwijl we in dit venster zijn kunnen we ook instellen waar we de bestanden wensen heen te schrijven a.d.h.v. `Rails2RailwayCenterLine_PathToCSVFile`en `CalculateSurveyCorrection_PathToCsvFile`. Deze bestanden zullen het resultaat bevatten van de twee berekeningen. Deze kunnen dan op hen beurt worden gebruikt om te controleren ofdat alles goed werd berekend.

In normale omstandigheden hoeven deze bestanden niet te worden aangemaakt. We kunnen deze dan ook uitschakkelen in het `SettingsPanel.`

### Controle

Hoe controleren we nu of dit correct is? We kunnen vergelijken met het resultaat van de opmeter. In een [ander AutoCAD bestand](L60_OW17_As_rails.dwg), vinden we de berekende centerlijnen van beide sporen terug. Deze werden aangemaakt door Nicolas Rogge. Deze polylijnen zijn echter nog niet samengevoegd tot een enkele samengestelde lijn. Dit doen we met [het commando](https://bitbucket.org/cadsmurfs/topohelper/wiki/commands/IAMTopo_JoinPolyline) `IAMTOPO_JOINPOLYLINE`. Wanneer deze polylijnen zijn samengevoegd, kunnen we de lijn kopiëren naar onze vorige tekening.
De controle kunnen we nu uitvoeren adhv [het commando](https://bitbucket.org/cadsmurfs/topohelper/wiki/commands/IAMTopo_DistanceBetween2Polylines) `IAMTopo_DistanceBetween2Polylines`.

### IAMTopo\_DistanceBetween2Polylines
