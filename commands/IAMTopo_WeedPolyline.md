# IAMTopo\_WeedPolyline

Deze functie leest een lijst van punten in komende van een selectie van twee samengestelde lijnen (3D-POLYLINE). De lijst wordt opnieuw opgebouwd, maar houdt rekening met een minimum afstand tussen elk van de definiërende punten. Deze minimum afstand is door de gebruiker zelf in te stellen. (WeedPolyline\_MinDistance)

Het zorgt er ook voor dat wanneer een nieuw punt wordt gemaakt, dit punt steeds loodrecht wordt gekozen vergeleken met de eerst geselecteerde samengestelde lijn.
Voordat we deze functie kunnen gebruiken dienen aan enkele voorwaarden te worden voldaan.

## In te stellen variabelen

Om deze variabel aan te passen maak je gebruik van het commando `IAMTopo_Settings`.

### WeedPolyline\_MinDistance

| Name | Description |
| ---- | ----------- |
| default value | 10.00 |
| value type | double |