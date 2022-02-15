============================
Rails to Railway Center Line
============================

Gebruik
-------

Stap 1: Commando
^^^^^^^^^^^^^^^^
In autocad start het commando ``IAMTopo_Rails2RailwayCenterLine.``

Stap 2: Selecteer de rails
^^^^^^^^^^^^^^^^^^^^^^^^^^
Selecteer de linkerrail en dan selecteer je de rechter rail.

Stap 3: Controle van de invoer
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Een controle wordt uitgevoerd op de door u aangeleverde invoer. Dit om te kijken of de functie kan worden verder gezet.

Deze controles zijn:
====================

- zijn de geselecteerde polylijnen uniek t.o.v. elkaar
- zijn het aantal definieërende (*in AutoCAD noemen we dit Vertexten*) punten gelijk
- zijn er genoeg punten op de ploylijnen, lees meer dan 2 punten per polylijn
- is de richting t.o.v. elkaar gelijk
- voldoen de spoorwijdtes aan de minimum opgegeven waarde
    .. seealso:: zie TODO:  in de tabel van de variabelen.
- voldoen de spoorwijdtes aan de maximum opgegeven waarde
    .. seealso:: zie TODO: in de tabel van de variabelen.

Wanneer **alle controles in orde zijn** kan er verder worden gegaan.

Stap 5: Corigeren waarde i.v.m. de scheefstand van de bolprismas
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Wanneer de waarde van de variabel ``Rails2RailwayCenterLine_Use_CalculateSurveyCorrection`` is ingeschakeld (waarde ``= True``), dan wordt de correctie berekend voor het rekening houden met de scheefstand van de bolprisma’s.

.. seealso:: TODO: Lees hier hoe dit in zijn werk gaat.

Stap 6: Berekenen van de aslijn van het spoor
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Aan de hand van de rails berekenen we nu de centerlijn van het spoor.

.. seealso:: TODO: Lees hier hoe dit in zijn werk gaat

STAP 7: Uitteken van de resultaten naar de tekening
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Alle lijnen/punten worden uitgetekend in de DWG en dat **volgens de ingestelde standaarden**.

.. seealso:: TODO: Om deze instellingen aan te passen: zie alle instellingen hier.

STAP 8: Uitsturen van het resultaat naar een CSV bestand
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Indien zo ingesteld worden er CSV-bestanden weggeschreven naar de in de instellingen opgegeven locaties. Deze bestanden bevatten de berekende waarden van de uitgevoerde functies.

.. Note:: Momenteel wordt een bestand berekend met de gegevens i.v.m. de uitgevoerde correcties in STAP 5. En i.v.m. de gegevens berekend in STAP 6.

.. seealso:: TODO: Lees hier hoe je deze CSV bestanden makkelijk zelf gebruikt.

Waarom is deze functie nodig
----------------------------

Wanneer een operator een spoorlijn gaat opmeten, dan vraagt de dienst sporen om de volgende zaken op te meten:

* De as van het spoor volgens RTVB B1.1
* De linker rail (in bocht, of bij een verkanting)
* De rechter rail (in bocht, of bij een verkanting)

Er moeten dus drie punten worden opgemeten.

De spoor-as kan worden berekend aan de hand van de opgemeten rails.

Meer info i.v.m. deze methode: spoor-definitie beschreven in de RTVB B1.1: Design van het spoor (Bericht 24 I-AM/2014).

Momenteel beschikken we binnen AutoCAD niet over een routine die deze berekening maakt.

Hoe moeten de gegevens worden aangeleverd
-----------------------------------------

De opmeter levert ons een AutoCAD bestand aan die de volgende zaken bevat:

- 3d polylijn van beide assen van de rail, opgebouwd door het verbinden van alle opgemeten punten (X,Y,Z [#TAW]_)
    .. warning:: Alle vertexen die deel uitmaken van de polylijn worden als opgemeten punten beschouwd! Voeg dus geen punten toe die niet werkelijk zijn opgemeten!

- Alle opgemeten punten, in hun eigen laag. 
    .. note::
    
        Deze punten worden voorgesteld door gebruik te maken van AutoCAD blokken. 
        We hebben het liefst blokken met als attribuut de naam (NAME), en als insertiepunt: Z [#TAW]_ X en Y.
        Er mag steeds meer informatie beteffede de opgemeten punten worden toegevoegd, *bovenstaand is een minimum.*

- De punten zijn in eenzelfde sectie opgemeten, d.w.z. de punten staan loodrecht tegenover elkaar t.o.v. de spooras.
- De aangeleverde opmeting wordt opgemeten in Lambert72 [#EPGS-31370]_

.. rubric:: Voetnoot

.. [#TAW] Tweede algemene waterpassing: https://www.ngi.be/website/tweede-algemene-waterpassing/
.. [#EPGS-31370] Belge_1972_Belgian_Lambert_72: http://epsg.io/31370