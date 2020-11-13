# Weed Polyline

## Waarschuwwig

TODO: This function has not yet been properly implemented. It lacks definition and a well described purpose.

## Beschrijving

Deze functie leest een lijst van punten in komende van een selectie van twee samengestelde lijnen (*of twee 3D-POLYLINE*). De lijst met punten wordt opnieuw opgebouwd, maar houdt rekening met een minimum afstand tussen elk van de definiërende punten op de als eerste geselecteerde lijn. Deze minimum afstand is door de gebruiker zelf in te stellen. (WeedPolyline\_MinDistance). Wanneer een punt niet voldoet aan deze minimum afstand wordt ze niet toegevoegd aan de nieuwe lijst.

Aan de hand van deze nieuwe verworven lijst, bouwen we de lijst voor de tweede samengestelde lijst op.

Elk punt die aan deze nieuw lijst wordt toegevoegd wordt loodrecht gekozen t.o.v de puntjen op de eerst geselecteerde samengestelde lijn.

## Voorwaarden

Voordat we deze functie kunnen gebruiken dienen aan enkele voorwaarden te worden voldaan.

## In te stellen variabelen

Om deze variabel aan te passen maak je gebruik van het commando `IAMTopo_Settings`.

### WeedPolyline\_MinDistance

| Name | Description |
| ---- | ----------- |
| default value | 10.00 |
| value type | double |
