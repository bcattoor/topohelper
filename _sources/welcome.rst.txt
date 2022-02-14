======
Welkom
======

Op deze website gaan we dieper in op enkele onderwerpen in verband met het gebruik van de TopoHelper tool/plugin en de beshikbare commando's. Voor meer informatie over het project zelf: bijvoorbeeld: voor het `melden van een probleem`_, of om zelf even de broncode in te kijken dan kan je `hier op onze github pagina`_ terecht.

.. _melden van een probleem: https://github.com/bcattoor/topohelper/issues/new/choose

.. _hier op onze github pagina: https://github.com/bcattoor/topohelper/

Waarom deze plugin
-------------------
Deze plugin en zijn commando's zijn ontwikkeld voor bepaald specifiek gebruik op (opgemeten) samengestelde lijnen (3D POLYLINES in AutoCAD). Tijdens het werken met deze specifieke lijnen zijn noden ontstaan die met een AutoCAD software niet standaard wordt opgelost. Deze plugin biedt daar zoveel mogelijk een antwoord op.

In deze documentatie ga ik in op: het waarom een bepaalde functie nodig is, en hoe je deze kan gebruiken.

Instalatie
----------
Alles i.v.m. de applicatie instaleren lees je hier in :ref:`Install`.

Gebruik
--------
Door gebruik te maken van de **COMMAND LINE** in **AutoCAD** kan je de functies van deze plugin oproepen. 

Indien een er een opfrissing, *van hoe deze commandoregel te gebruiken*, vereist is lees dan even hier verder: :ref:`WorkingWithTheCommandline`.

Aanpassen van de instellingen
--------------------------------

De instellingen kunnen via een instellingenpaneel worden aangepast. Hoe je dit doet lees je in hier: :ref:`IAMTopo_Settings`. Ook kan via het instellingen paneel, de de functies van deze plugin aanroepen via een uitklapmenu.

Verdeling van de gecompileerde bestanden
----------------------------------------

Deze kan dan onder de tekenaars worden verdeeld door een downloadlink. De downloadlink is beschikbaar op de github server. `Download hier de laatste versie`_. Als bepaalde collega's niet over internetoegang beschikken, dan kan de software ook worden verdeeld door het gedownloade bestand te verspreiden via de interne file servers. Dit wordt echter afgeraden.

.. _Download hier de laatste versie: https://github.com/bcattoor/topohelper/releases/latest

Gebruikte technologie voor het ontwikkelen van de software
----------------------------------------------------------
Er werd gekozen om met het C# .NET Framework 4.6 een oplossing aan te bieden in de vorm van een AutoCAD plug-in. Als ontwerpsoftware wordt gebruik gemaakt van Visual Studio 2019.

Versie beheer van de code
-------------------------
De uitgewerkte oplossing wordt beheerd op github door middel van een GIT-versiebeheersysteem. Een overzicht van alle commits_ kan je hier vinden. 

De volledig broncode_ is ook beschikbaar. Dit kan nodig zijn wanneer er in de toekomst aanpassingen moeten worden gedaan.

.. _commits: https://github.com/bcattoor/topohelper/commits/master
.. _broncode: https://github.com/bcattoor/topohelper/tree/master

