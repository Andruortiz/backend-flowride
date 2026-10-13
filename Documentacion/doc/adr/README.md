# ADRs — FlowRide

Este directorio contiene los Architecture Decision Records vigentes de FlowRide.

| ADR | Decisión |
|---|---|
| ADR-0001 | Monolito modular con límites internos explícitos |
| ADR-0002 | Multi-tenancy Silo por operador |
| ADR-0003 | Identidad desacoplada mediante OIDC/OAuth2 |
| ADR-0004 | PostgreSQL + Npgsql como persistencia transaccional |
| ADR-0005 | Telemetría GPS con adapter + PostgreSQL particionado |
| ADR-0006 | Cache-Aside local para baja volatilidad |
| ADR-0007 | Consistencia con concurrencia optimista + Outbox |
| ADR-0008 | Integrar WhatsApp mediante Sandbox (curso) y evolucionar a Meta Cloud API (producción) |
| ADR-0009 | Motor de asignación con reglas configurables |
| ADR-0010 | Despliegue Silo contenedorizado y escalado horizontal |
| ADR-0011 | OpenTelemetry + auditoría persistente por tenant |

Todos los ADRs usan la misma estructura de referencia (Metadatos, Contexto, Decisión, Alternativas, Consecuencias, Tácticas de Bass, Escenarios/requisitos relacionados, ADRs relacionados, Notas de implementación, Referencias).
