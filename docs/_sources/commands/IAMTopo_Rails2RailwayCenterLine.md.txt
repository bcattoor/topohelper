# Rails to Railway Center Line

## Gebruik

1. Start het commando `IAMTopo_Rails2RailwayCenterLine.`
2. Selecteer de linkerrail
3. Selecteer de rechter rail
4. Een controle wordt uitgevoerd om te kijken of de functie kan worden verder gezet:
    * zijn de geselecteerde polylijnen uniek t.o.v. elkaar
    * zijn het aantal definieërende(*in AutoCAD noemen we dit Vertexten*) punten gelijk
    * zijn er genoeg punten op de ploylijnen, lees meer dan 2 punten per polylijn
    * is de richting t.o.v. elkaar gelijk
    * voldoen de spoorwijdtes aan de minimum opgegeven waarde
        * zie [DataValidation\_LeftrailToRightRail\_Tolerance](https://bitbucket.org/cadsmurfs/topohelper/wiki/Home) in de tabel van de variabelen.
    * voldoen de spoorwijdtes aan de maximum opgegeven waarde
        * zie [DataValidation\_LeftrailToRightRail\_Maximum](https://bitbucket.org/cadsmurfs/topohelper/wiki/Home) in de tabel van de variabelen.
    * Wanneer alle controles in orde zijn kan er verder worden gegaan.
5. Wanneer de waarde van `Rails2RailwayCenterLine_Use_CalculateSurveyCorrection` is ingesteld als waar(True), dan wordt de correctie berekend voor het rekening houden met de scheefstand van de bolprisma’s.
    * TODO: Lees hier hoe dit in zijn werk gaat.
6. Aan de hand van de sporen berekenen we nu de centerlijn van het spoor.
    * TODO: Lees hier hoe dit in zijn werk gaat
7. Alle lijnen/punten worden uitgetekend in de DWG en dat volgens de ingestelde standaarden.
    * Om deze instellingen aan te passen: zie [alle instellignen hier.](https://bitbucket.org/cadsmurfs/topohelper/wiki/Home)
8. Indien zo ingesteld worden de CSV-bestanden weggeschreven naar de in de [instellingen](https://bitbucket.org/cadsmurfs/topohelper/wiki/Home) opgegeven locaties.
    * TODO: Lees hier hoe je deze CSV bestanden makkelijk zelf gebruikt.

## Waarom

Wanneer een operator een spoorlijn gaat opmeten, dan vraagt de dienst sporen om de volgende zaken op te meten:

* De as van het spoor volgens RTVB B1.1
* De linker rail (in bocht, of bij een verkanting)
* De rechter rail (in bocht, of bij een verkanting)

Er moeten dus drie punten worden opgemeten.

De spoor-as kan worden berekend aan de hand van de opgemeten rails.

Meer info i.v.m. deze methode: spoor-definitie beschreven in de RTVB B1.1: Design van het spoor (Bericht 24 I-AM/2014).

Momenteel beschikken we binnen AutoCAD niet over een routine die deze berekening maakt.

## Hoe worden de gegevens aangeleverd

De opmeter levert ons een AutoCAD bestand aan die de volgende zaken bevat

* 3d polylijn van beide assen van de rail, opgebouwd door het verbinden van alle opgemeten punten
    * Hoogte Z, X en Y
* Alle opgemeten punten
    * Hoogte Z, X en Y
    * Naam van de punten
* De punten zijn in eenzelfde sectie opgemeten, d.w.z. de punten staan loodrecht tegenover elkaar t.o.v. de spooras.