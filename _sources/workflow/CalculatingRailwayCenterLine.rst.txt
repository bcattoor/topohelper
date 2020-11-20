==========================
As van het spoor berekenen
==========================

Berekenen van de as van het spoor a.d.h.v. de opgemeten rails
-------------------------------------------------------------

Om een werkbare workflow voor te leggen kies ik er om om met een werkelijke situatie te starten. Op die manier kan je de bestanden zelf downloaden en openen in AutoCAD, en dus de workflow volgen. We zullen de spooras berekenen aan de hand van twee opgemeten rails (as-rail, binnenkant-rail).

.. note:: In dit voorbeeld heeft de opmeter er voor gekozen om binnenkant rail op te meten. De applicatie kan overweg met beide methodes.

De verkregen bestanden
----------------------

Als voorbeeld hebben we een AutoCAD bestand gekregen van de lijn 60 (Download hier -> `OW17 L60`_)

.. figure:: ./../_static/images/wf_crcl_dwg.*

    Screenshot van de modelspace van de DWG.

.. _OW17 L60: https://bcattoor.github.io/topohelper/files.survey/OW17%20L60.dwg


Wat vinden we in dat bestand
^^^^^^^^^^^^^^^^^^^^^^^^^^^^
In de laag **binnenkant rail** vinden we vier samengestelde lijnen *aka enkele 3D Polylines* met op elke vertex een xyz-waarde. De lijnen zijn niet samengevoegd en bestaan dus uit meerdere delen. Dit moeten we als eerste zien op te lossen.


.. figure:: ./../_static/images/wf_crcl_binnenkant_rail.*

    Screenshot van de vier rails.

Workflow
-----------
Hier een voorstelling van hoe de workflow er standaard uiziet, uiteraard kan hier worden van afgeweken indien nodig.

.. figure:: ./../_static/diagrams/CalculateCenterlineTrack.svg

    Basis flow: schematische voorstelling

Voorbereiding
^^^^^^^^^^^^^
Bruikbaar maken van de aangeleverde gegevens uit de dwg `OW17 L60`_.

Samenvoegen
~~~~~~~~~~~
Het is belangrijk dat de polylijnen aaneensluitend zijn over de volledige lengte. Op die manier is het mogelijk om de spooras in één keer door te rekenen.

In ons voorbeeld:
Omdat alle polylijnen mooi aaneensluitend zijn, kunnen we gebruik maken van het AutoCAD commando ``join``. Om het overzicht in de tekening te bewaren, zorg ik ervoor dat enkel de laag *Binnenkant Rail* ``Unlocked`` staat. 

.. image:: ./../_static/images/wf_joinpolyline_001.gif

Wanneer de lijnen **niet mooi aaneensluitend zijn**, dan maken we gebruik van het commando :ref:`IAMTopo_JoinPolyline` om de lijnen samen te voegen.

Richting
~~~~~~~~
Na het samenvoegen van de lijnen controleren we of dat de richting van de polylijnen aan elkaar gelijk zijn.
Ook zorgen we ervoor dat de richting de algemene righting van de kilometrering van de hoofdlijn volgt.

Vertexen
~~~~~~~~
We controleren ook of de samengestelde lijnen van de rails *evenveel vertexen hebben*. Hier in ons voorbeeld is dit niet het geval. Na een visuele controle merken we op dat dit komt doordat er vertexen zijn die door de opmeter of zijn software zijn toegevoegd. **Deze vallen niet op effectief opgemeten punten. Indien we deze verwijderen zullen we eenzelfde aantal punten bekomen voor elke polyijn van de rail.**

Opkuisen van niet opgemeten punten
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

Om deze punten te verwijderen maken we gebruik van: :ref:`IAMTopo_CleanNonSurveyVertexFromPolyline` Dit commando zal elk punt dat zich niet op een echt meetpunt bevindt, verwijderen uit de lijst met punten en een nieuwe lijn creëren. 

We selecteren eerst een opmeetpunt, en daarna de polylijn die we wensen op te kuisen.
Na een controle merken we dat de 4-lijnen van de rails evenveel vertexen hebben.
We zijn nu klaar om over te gaan naar het berekenen van het center.
Om de origineel aangeleverde lijnen niet te wijzigen, heb ik de nieuwe lijnen verplaats naar een nieuwe laag. Laagnaam: ``Binnenkant Rail Verwijderde Punten``

.. image:: ./../_static/images/wf_CleanNonSurveyVertexFromPolyline_001.gif

Berekenen van de as van het spoor
^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
Met het commando ``IAMTopo_Rails2RailwayCenterLine`` kunnen we nu het center tussen de twee lijnen berekenen.

Ook dient er een correctie te gebeuren om de scheefstand van de bolprismas te corigeren. Het algoritm houdt enkel rekening met *de verplaatsing in xy richting*, de verplaatsing van de z-waarde [#TAW]_ (hoogte van toestel) werd reeds door de opperator van de waarde afgetrokken. Om er voor te zorgen dat er een correctie wordt berekend moet de omgevingsvariabel ``Rails2RailwayCenterLine_Use_CalculateSurveyCorrection`` op de waarde *True* staan.

Het :ref:`IAMTopo_Settings` paneel kunnen we oproepen aan de hand van het commando ``IAMTopo_Settings``. 

Uittekenen van de oplossing in AutoCAD
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
Na het berekenen wordt het resultaat uitgetekend aan de hand van se standaard ingesteld in het :ref:`IAMTopo_Settings` paneel.


CSV bestanden
~~~~~~~~~~~~~
Ter controle kunnen ook CSV bestanden aangemaakt worden. Deze bestanden bevatten de ruwe basisgegevens die gebruikt zijn voor de berekening alsook de berekende oplossing. Deze kunnen dan op hen beurt worden gebruikt om te controleren ofdat alles goed werd berekend.
Terwijl we in :ref:`IAMTopo_Settings` zijn kunnen we ook instellen waar we de bestanden wensen heen te schrijven a.d.h.v. ``Rails2RailwayCenterLine_PathToCSVFile`` en ``CalculateSurveyCorrection_PathToCsvFile``.

In normale omstandigheden hoeven deze bestanden niet te worden aangemaakt. We kunnen deze dan ook uitschakkelen in het :ref:`IAMTopo_Settings` paneel.

Controle
^^^^^^^^

Hoe controleren we nu of dit correct is? We kunnen vergelijken met het resultaat van de opmeter. In een [ander AutoCAD bestand](https://bcattoor.github.io/topohelper/files.survey/L60_OW17_As_rails.dwg), vinden we de berekende centerlijnen van beide sporen terug. Deze werden aangemaakt door Nicolas Rogge. Deze polylijnen zijn echter nog niet samengevoegd tot een enkele samengestelde lijn. Dit doen we met [het commando](https://bitbucket.org/cadsmurfs/topohelper/wiki/commands/IAMTopo_JoinPolyline) ``IAMTOPO_JOINPOLYLINE``. Wanneer deze polylijnen zijn samengevoegd, kunnen we de lijn kopiëren naar onze vorige tekening.
De controle kunnen we nu uitvoeren adhv [het commando](https://bitbucket.org/cadsmurfs/topohelper/wiki/commands/IAMTopo_DistanceBetween2Polylines) ``IAMTopo_DistanceBetween2Polylines``.

IAMTopo_DistanceBetween2Polylines
~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

.. This directive creates a paragraph heading that is not used to create a table of contents node.

.. rubric:: Voetnoot

.. [#TAW] Tweede algemene waterpassing: https://www.ngi.be/website/tweede-algemene-waterpassing/
.. [#EPGS-31370] Belge_1972_Belgian_Lambert_72: http://epsg.io/31370
