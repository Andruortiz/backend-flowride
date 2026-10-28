# ADR-0018: estructura del backend por módulos (proyecto por módulo con capas internas)

## Metadatos

| Campo | Valor |
|---|---|
| ID | ADR-0018 (propuesto) |
| Estado | **Proposed** — pendiente de validación del equipo |
| Relacionados | ADR-0001 (monolito modular), ADR-0007 (outbox y concurrencia), ADR-0017 (.NET y capas) |
| Reemplaza parcialmente | La organización "un proyecto por capa" descrita en ADR-0017 |

## Contexto

El ADR-0017 fija la organización interna en cuatro capas (`Domain`, `Application`, `Infrastructure`, `API`). El ADR-0001 exige además **límites de módulo explícitos** y que los módulos no se acoplen por acceso directo a implementaciones internas de otros módulos.

Con un proyecto por capa, esos límites existían solo como carpetas: un servicio podía usar el repositorio de otro módulo y ninguna regla de compilación lo impedía. Se verificó en el código: tres servicios (solicitudes, viajes, asignaciones) usaban repositorios de otros módulos.

## Decisión

Un proyecto por módulo de negocio, con las cuatro capas del ADR-0017 como carpetas internas (`Dominio`, `Aplicacion`, `Infraestructura`). La capa `API` queda como proyecto de arranque (`Inicializador`) que solo compone.

```
src/
├── Compartido/
│   ├── FlowRide.Nucleo/        excepciones, helpers, Entidad, RaizAgregado, contratos de seguridad
│   ├── FlowRide.Contratos/     eventos de dominio que cruzan módulos
│   └── FlowRide.Persistencia/  DbContext único, unidad de trabajo, outbox
├── Modulos/                    Plataforma, Clientes, Identidad, Flota, Solicitudes,
│                               Viajes, Asignaciones, Comunicacion, Operacion
└── FlowRide.Inicializador/     composición, seguridad web, migraciones
```

### Reglas

1. Las dependencias entre módulos siguen un grafo acíclico fijo (de `Plataforma` hacia `Operacion`). Una prueba de arquitectura vigila que ningún módulo referencie a uno no permitido.
2. Un módulo expone a los demás solo: sus **servicios** (puertos de entrada), sus **puertos de consulta** (`IConsultaX`), sus **DTOs** y sus **tipos de dominio**. Los repositorios y las implementaciones son `internal`: el compilador impide usarlos desde fuera.
3. Los cambios que otro módulo debe reflejar viajan como **eventos** (`FlowRide.Contratos`) por el outbox. Quien reacciona lo hace de forma idempotente.
4. **Excepción documentada:** cuando una operación debe ser atómica entre módulos (asignar: crear la asignación y marcar la solicitud), el módulo que opera puede modificar entidades que obtuvo por un puerto de consulta; se guardan en la misma transacción.
5. Hay un solo `DbContext` y una sola base por operador (ADR-0002, ADR-0007). Cada módulo aporta su configuración de tablas (`IConfiguradorModelo`); `Persistencia` no conoce a ningún módulo. Las migraciones viven en el proyecto de arranque.

## Consecuencias

**Positivas**
- Los límites de módulo los hace cumplir el compilador y 44 reglas de arquitectura.
- Cancelar una solicitud, iniciar y finalizar un viaje dejaron de ser transacciones entre módulos: son eventos.
- Un módulo se puede extraer a un servicio en el futuro con poco cambio (ADR-0001, notas de implementación).

**Negativas**
- Consistencia eventual en esas transiciones: la solicitud pasa a *Finalizada* unos instantes después del viaje.
- Dentro de un módulo ya no hay un proyecto distinto por capa; que `Dominio` no use EF Core o ASP.NET lo garantizan las pruebas de arquitectura, no el compilador.
- Más proyectos (13 en `src`, frente a 5).

## Alternativas descartadas

- **Un proyecto por capa (estado anterior):** no hace cumplir los límites de módulo.
- **Tres proyectos por módulo (27 proyectos):** protege más, pero el costo de mantenimiento no se justifica para el volumen del proyecto.
- **Un `DbContext` por módulo:** rompería la transacción única con el outbox (ADR-0007).
