# Catálogo Galáctico API

API REST desarrollada con .NET 10 utilizando Minimal API para administrar personajes, cartas y eventos de un catálogo galáctico.

## Tecnologías

- .NET 10
- C#
- ASP.NET Core Minimal API
- Swagger / OpenAPI
- Git y GitHub

## Estructura

El proyecto está organizado en:

- Models: modelos principales y DTOs.
- Data: almacenamiento de datos en memoria.
- Services: lógica auxiliar y consultas.
- Endpoints: rutas de la API.
- Program.cs: configuración principal.

## Funcionalidades

La API permite administrar personajes, cartas y eventos.

También incluye:

- Filtros de personajes por facción y sensibilidad a la Fuerza.
- Relación entre personajes y eventos.
- Una carta por personaje.
- Ranking de personajes por poder.
- MVP de cada evento.
- Simulación de eventos según el poder de las facciones.
- Control de fallecimiento de personajes.
- Validaciones para mantener la coherencia entre personajes, cartas y eventos.

## Endpoints principales

### Personajes

GET /personajes  
GET /personajes/{id}  
POST /personajes  
PUT /personajes/{id}  
DELETE /personajes/{id}  
GET /personajes/{id}/eventos  
GET /personajes/ranking?por=poder  

### Cartas

GET /cartas  
GET /cartas/{id}  
POST /cartas  
PUT /cartas/{id}  

### Eventos

GET /eventos  
GET /eventos/{id}  
POST /eventos  
PUT /eventos/{id}  
GET /eventos/{id}/mvp  
POST /eventos/{id}/simular  

## Fechas de los eventos

Las fechas utilizan números enteros:

- Valores negativos: BBY
- 0: Batalla de Yavin
- Valores positivos: ABY

## Simulación

Para simular un evento se suma el poder de las cartas de los personajes de cada facción y se aplica un factor aleatorio entre 0.90 y 1.10.

La facción con mayor poder final gana la simulación.

