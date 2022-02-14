.. _IAMTopo_CleanNonSurveyVertexFromPolyline:

=======================================
Clean Non Survey Verteces From Polyline
=======================================

Waarom
------
Soms verkrijgen we een samengestelde lijn die meer puntjes bevat ofdat er initieel werden opgemeten. 
Dit kan een probleem vormen voor bepaalde functies.

Wat
---
Deze functie zal elke vertex [#vertex]_ dat zich niet op een echt meetpunt bevindt, verwijderen uit de lijst met punten, en een nieuwe samengestelde lijn creëren.
De lijst met meetpunten wordt opgebouwd aan de hand van alle punten die in de laag voorkomen van een door de gebruiker geselecteerd punt.

Gebruik
-------
#. Op de commandoregel type je het commando ``IAMTopo_CleanNonSurveyVertexFromPolyline`` in.
#. Selecteer een punt of block die bij de desbetreffende samengestelde lijn behoort. 
    .. note:: Filteren gebeurt op basis van de laagnaam waarin dit object zich bevind.
#. Selecteer de polylijn die u wenst op te kuisen.
#. Een nieuw polylijn is gemaakt.

.. image:: ./../_static/images/wf_CleanNonSurveyVertexFromPolyline_001.gif

.. rubric:: Voetnoot

.. [#vertex] Een vertex is een definiërend punt van een samengestelde lijn.