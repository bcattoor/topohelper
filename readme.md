# TopoHelper

Een plugin voor AutoCAD, die hulpmiddelen aanbied op bepaalde zaken te berekenen.

## Beschrijving van het probleem

Wanneer een operator een spoorlijn gaat opmeten, dan vraagt de dienst sporen om de volgende zaken op te meten:  

- De as van het spoor volgens RTVB B1.1
- De linker rail (in bocht, of bij een verkanting)
- De rechter rail (in bocht, of bij een verkanting)

Er moeten dus drie punten opgemeten worden, en dit terwijl de spoor-as kan worden berekend aan de hand van de opgemeten rails.

Meer info i.v.m. deze methode: spoor-definitie beschreven in de RTVB B1.1: Design van het spoor (Bericht 24 I-AM/2014).

Momenteel beschikken we binnen AutoCAD niet over een routine die deze berekening maakt.

## Flow

De opmeter levert ons een AutoCAD bestand aan die de volgende zaken bevat

- 3d polylijn van beide assen van de rail, opgebouwd door het verbinden van alle opgemeten punten
  - Hoogte Z, X en Y
- Alle opgemeten punten
  - Hoogte Z, X en Y
  - Naam van de punten

## Gebruikershandleiding

<u>System variables:</u>

| Name                   | Description                                                                                                                                                                                                                                                                                        |
| ---------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DV_LRTRR_TOLERANCE     | We use this value to determine when a measured track gauge is too small. The formula we use is as follows: `gauge < (1.435 - IAM_DV_LRTRR_TOLERANCE)`. When expression is `true` we throw an exception.<br />`DV` Stands for *data validation*.<br />`LRTRR` Stands for *left-rail-to-right-rail*. |
| DV_LRTRR_MAX_VALUE     | We use this value to determine when a measured track gauge is too big. The formula we use is as follows: `gauge > IAM_DV_LRTRR_MAX_VALUE`. When expression is `true` we throw an exception.                                                                                                        |
| LAY_NAME_PREFIX_2D     | All 2D entities created by this plugin will have the layer name prefixed by this value.<br />`LAY` Stands for *layer*.                                                                                                                                                                             |
| LAY_NAME_PREFIX_3D     | All 3D entities created by this plugin will have the layer name prefixed by this value.                                                                                                                                                                                                            |
| APP_EPSILON            | This value is used internally to compare doubles, it represents the precision of the calculations. It can not be altered at runtime, if changes are needed, they should be implemented by the developer.<br />`APP` Stands for *application setting*.                                              |
| DRAW_3D_R2R_RAILS_PL   | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 3D polyline for the calculated rails.                                                                                                                                                                             |
| DRAW_3D_R2R_CL_PL      | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 3D polyline for the calculated track axis.                                                                                                                                                                        |
| DRAW_2D_R2R_RAILS_PL   | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 2D polyline for the calculated rails.                                                                                                                                                                             |
| DRAW_3D_R2R_CL_PL      | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 2D polyline for the calculated track axis.                                                                                                                                                                        |
| DRAW_3D_R2R_CL_PNTS    | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 3D points for the calculated track axis.                                                                                                                                                                          |
| DRAW_3D_R2R_RAILS_PNTS | Used in the **IAMTopo_Rails2RailwayCenterLine**<br />When true, we will draw the 3D points for the calculated rails.                                                                                                                                                                               |
| IO_FILE_R2R_CSV        | This file is used to log our calculation. Can be used when the calculations need to be exported for verification.                                                                                                                                                                                  |
| LAY_NAME_CSD_RAILS     |                                                                                                                                                                                                                                                                                                    |

Commands in TopoHelper

| Command name                    | Description |
| ------------------------------- | ----------- |
| IAMTopo_Rails2RailwayCenterLine |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |
|                                 |             |

## Installatie

## Gebruik in AutoCAD

## Ontwikkel Software

Er wordt gekozen om met het C# .NET Framework 4.6 een oplossing aan te bieden in de vorm van een AutoCAD plug-in. Als ontwerpsoftware wordt gebruik gemaakt van [Visual Studio 2019](https://visualstudio.microsoft.com/vs/).

## Versie beheer van de code

De uitgewerkte oplossing wordt beheerd op een BitBucket GIT-versiebeheersysteem.

## Verdeling van de gecompileerde bestanden

Deze kan dan onder de tekenaars worden verdeeld aan de hand van een downloadlink. De downloadlink is beschikbaar op de BitBucket server.

## Definities gebruikt in de tekst

Tabel met de gebruikte definities