# FlowRide — Entidades, reglas, casos de uso y forma de construcción

Backend en C# .NET 10, arquitectura hexagonal, monolito modular. Un despliegue por operador (silo): cada operador tiene su proceso y su base de datos.

Este documento describe lo que **ya está construido** en `feature/proyecto-completo`. Lo que falta aparece en la sección 7.

---

## 1. Módulos y dependencias

Un proyecto por módulo; cada uno con las capas `Dominio`, `Aplicacion` e `Infraestructura` como carpetas (ver ADR-0018).

```
src/
├── Compartido/   Nucleo · Contratos · Persistencia
├── Modulos/      Plataforma · Clientes · Identidad · Flota · Solicitudes
│                 Viajes · Asignaciones · Comunicacion · Operacion
└── FlowRide.Inicializador/
```

| Proyecto | Qué contiene |
|---|---|
| `Nucleo` | Excepciones, helpers, `Entidad`, `RaizAgregado`, rol y contexto del actor, `IUnitOfWork`, `IManejadorEvento` |
| `Contratos` | Los eventos de dominio que cruzan módulos |
| `Persistencia` | `DbContext` único, unidad de trabajo, outbox, convenciones del modelo |
| `Plataforma` | Parámetros del operador, auditoría, datos del operador |
| `Clientes` | Clientes empresariales e itinerarios |
| `Identidad` | Usuarios y pasajeros |
| `Flota` | Conductores, vehículos, documentos y turnos |
| `Solicitudes` | Solicitudes de transporte |
| `Viajes` | Viajes, seguimiento GPS y calificaciones |
| `Asignaciones` | Motor de asignación y su configuración |
| `Comunicacion` | Notificaciones, alertas, bot de WhatsApp |
| `Operacion` | Reportes y tareas programadas |
| `Inicializador` | Composición, seguridad web, migraciones, sembrado de desarrollo |

**Reglas de dependencia**
- Los módulos dependen solo de los que tienen debajo (grafo fijo, vigilado por pruebas de arquitectura).
- Entre módulos se habla por servicios, puertos de consulta (`IConsultaX`) o eventos; los repositorios son `internal`.
- Cuando una operación debe ser atómica entre módulos (asignar), se documenta como excepción.

Convenciones: los conceptos de negocio van en español; los nombres técnicos y de patrones en inglés (`IUnitOfWork`, `IWhatsAppGateway`, `AssignmentEngine`, `IAssignmentRule`).

Tiempo: los instantes son `DateTimeOffset` en UTC. Las fechas de negocio (`DateOnly`, `TimeOnly`) se interpretan en la zona IANA del operador. Nada usa `DateTime.Now`; el reloj es `TimeProvider`.

---

## 2. Entidades, atributos y reglas

Notación: **RA** = raíz de agregado (acumula eventos de dominio). **E** = entidad simple. **VO** = objeto de valor. Todas tienen `Id` (GUID v7, se genera si no se pasa).

### 2.1 Operador (E)
Datos del operador de este despliegue. Una sola fila.

| Atributo | Tipo |
|---|---|
| RazonSocial, Nit, CorreoContacto | texto |
| ZonaHoraria | zona IANA |
| Activo, CreadoEn | bool, instante |

Reglas
- Razón social y NIT obligatorios.
- La zona horaria debe existir (se valida contra IANA).
- El correo se normaliza.

### 2.2 Usuario (E)
Cualquier persona con acceso: pasajero, conductor, administrador u operador logístico.

| Atributo | Tipo |
|---|---|
| Nombre, CodigoInterno | texto obligatorio |
| Correo, Telefono | texto opcional, normalizado |
| Rol | Pasajero, Conductor, AdministradorOperador, OperadorLogistico |
| IdentidadExterna | id del proveedor de identidad (único) |
| Activo, CreadoEn, DesactivadoEn | |

Reglas
- Pasajero y conductor necesitan teléfono (WhatsApp, formato internacional `+57…`).
- Administrador y operador necesitan correo.
- Código interno, correo y teléfono son únicos.
- La identidad externa se vincula una sola vez; no se cambia a otra.
- Un usuario desactivado pierde el acceso aunque su token siga vigente.
- El rol de pasajero y conductor no se cambia: se crea un usuario nuevo.

### 2.3 ClienteEmpresarial (E)
Empresa que contrata el transporte para sus empleados. No es el límite de aislamiento (ese es el operador).

| Atributo | Tipo |
|---|---|
| Nombre, Nit?, Activo | |

Reglas: nombre obligatorio. Un cliente inactivo no admite pasajeros nuevos.

### 2.4 Pasajero (E)
Perfil del usuario con rol pasajero.

| Atributo | Tipo |
|---|---|
| UsuarioId, ClienteEmpresarialId | referencias por Id |

Reglas: el usuario debe tener rol Pasajero. Un usuario tiene un solo perfil de pasajero.

### 2.5 Vehiculo (E)

| Atributo | Tipo |
|---|---|
| Placa (única), Capacidad, Habilitado | |
| Documentos | lista de `DocumentoVigencia` |

Reglas
- Placa y capacidad validadas al crear.
- Documentos requeridos: SOAT, revisión técnico-mecánica y tarjeta de operación.
- Registrar un documento del mismo tipo reemplaza al anterior.
- Es asignable en una fecha si está habilitado y los tres documentos están vigentes ese día.

### 2.6 Conductor (E)

| Atributo | Tipo |
|---|---|
| UsuarioId | referencia |
| Licencia | `DocumentoVigencia` |
| VehiculoId? | referencia |
| Activo, Disponible, MotivoNoDisponible | |
| Turnos | lista de `Turno` |

Reglas
- Marcar no disponible exige un motivo.
- Asignar un vehículo exige que el vehículo exista y esté habilitado.
- Los turnos de un mismo día no se solapan.
- **Asignabilidad** en una fecha y hora. No es asignable si ocurre alguno de estos motivos:
  - conductor inactivo
  - conductor no disponible
  - licencia no vigente
  - sin vehículo
  - vehículo no habilitado
  - documentos del vehículo no vigentes
  - fuera de turno

### 2.7 DocumentoVigencia (VO)
`Tipo` (Licencia, SOAT, RTM, TarjetaOperacion), `Numero`, `VenceEl` (fecha).

Reglas: número obligatorio. Estado según los días de aviso del operador: `Vigente`, `PorVencer`, `Vencido`. Vigente hasta el día de vencimiento inclusive. `Renovar` devuelve un documento nuevo.

### 2.8 Turno (VO)
`Dia`, `Inicio`, `Fin`.

Reglas: puede cruzar la medianoche (fin menor que inicio). Se detecta el solape con otro turno.

### 2.9 Itinerario (E)
Servicio planificado de un cliente.

| Atributo | Tipo |
|---|---|
| CodigoServicio, ClienteEmpresarialId? | |
| Origen, Destino | texto |
| Fecha | `DateOnly` |
| HoraSalida, HoraLlegada | `TimeOnly` |

Reglas: origen y destino obligatorios y distintos; código obligatorio.

### 2.10 SolicitudTransporte (RA)
Pedido de un pasajero. Su estado es la fuente de verdad del avance del servicio.

| Atributo | Tipo |
|---|---|
| PasajeroId, ItinerarioId? | |
| Origen, Destino | texto |
| DestinoLatitud?, DestinoLongitud? | para estimar la llegada |
| FechaHoraProgramada | instante UTC |
| Urgente, EsPersonal | bool |
| Estado | Pendiente, EnRevision, Asignada, EnCurso, Finalizada, Cancelada |
| AsignacionId?, Intentos, RecordatorioEnviado | |
| MotivoCancelacion?, CreadaEn, CanceladaEn? | |

Reglas
- Fecha futura; origen y destino obligatorios y distintos.
- Las coordenadas van las dos o ninguna, dentro de rango.
- **Modificar** solo mientras no tenga conductor (Pendiente o EnRevision).
- **Cancelar**: no si está EnCurso, Finalizada o ya Cancelada; el motivo es obligatorio. El pasajero debe cancelar con la antelación mínima (parámetro, 60 min por defecto); el operador puede hacerlo siempre que no haya empezado.
- Si es **personal**, administración y operación logística no ven origen ni destino.
- Sin recurso disponible: pasa a EnRevision y emite `SinRecursoDisponible`.
- Tras agotar los intentos de asignación (parámetro) pasa a EnRevision.
- El recordatorio se emite una sola vez.

Eventos: `SolicitudRegistrada`, `SolicitudCancelada`, `SinRecursoDisponible`, `RecordatorioViajeDebido`.

### 2.11 Asignacion (RA)
Oferta de una solicitud a un conductor con un vehículo.

| Atributo | Tipo |
|---|---|
| SolicitudId, ConductorId, VehiculoId | |
| Estado | PendienteAceptacion, Aceptada, EnCurso, Finalizada, Rechazada, Reasignada, Cancelada |
| VersionConfiguracion | versión de reglas usada |
| Explicacion | `DecisionAsignacion` (reglas, descartados, ranking, desempate) |
| EsManual, Motivo?, CreadaEn, RespondidaEn? | |

Reglas
- Una solicitud tiene a lo sumo una asignación viva (índice único parcial en la base).
- La asignación manual exige un motivo.
- Solo se reasigna una asignación pendiente o aceptada; la anterior queda en el histórico.
- Un conductor que rechazó o fue reemplazado en esa solicitud no vuelve a recibir la oferta.

Eventos: `AsignacionCreada`, `AsignacionAceptada`, `AsignacionRechazada`, `AsignacionReasignada`.

### 2.12 ConfiguracionAsignacion (E)
Reglas del motor con su orden y peso, versionadas.

| Atributo | Tipo |
|---|---|
| Version | entero, único |
| Reglas | lista de (ReglaId, Activa, Orden, Peso) |
| CreadaPor?, CreadaEn | |

Reglas: las eliminatorias obligatorias no se pueden desactivar; los pesos son válidos; cada cambio crea una versión nueva (nunca se edita una existente).

### 2.13 Motor de asignación (servicio de dominio)
`AssignmentEngine` con reglas `IAssignmentRule`.

| Regla | Tipo |
|---|---|
| disponibilidad, turno, regulatoria | Eliminatoria obligatoria |
| capacidad | Eliminatoria opcional |
| urgente | Prioridad |
| balance_carga, recientes | Puntuación |

Es determinista: misma entrada, mismo resultado. Desempata por el Id del conductor. Devuelve la explicación completa de la decisión.

### 2.14 Viaje (RA)
Ejecución real del servicio asignado.

| Atributo | Tipo |
|---|---|
| SolicitudId, AsignacionId, PasajeroId, ConductorId, VehiculoId | |
| Estado | Programado, EnCurso, Finalizado, Cancelado |
| ProgramadoPara, EtaProgramada | instantes |
| DestinoLatitud?, DestinoLongitud? | |
| IniciadoEn?, FinalizadoEn? | |
| AlertaEtaActiva | evita repetir la alerta |
| Hitos | trazabilidad (instante, tipo, detalle) |

Reglas
- La llegada prevista es posterior a la salida.
- Iniciar solo desde Programado; finalizar solo desde EnCurso.
- Reportar incidente solo en curso y con descripción.
- Cancelar solo antes de iniciar.
- Un vehículo no tiene dos viajes en curso (índice único parcial).
- Desviación de ETA: se alerta una sola vez al cruzar el umbral y se rearma cuando la ETA vuelve a ser normal.

Eventos: `ViajeIniciado`, `ViajeFinalizado`, `IncidenteReportado`, `DesviacionEtaDetectada`.

### 2.15 Calificacion (E)
`ViajeId`, `PasajeroId`, `ConductorId`, `Puntaje` (1 a 5), `Comentario?` (máx. 500), `CreadaEn`.

Reglas: solo de un viaje finalizado, por el mismo pasajero del viaje, una por viaje.

### 2.16 RegistroUbicacion (E)
Evento GPS: `VehiculoId`, `ConductorId?`, `ViajeId?`, `TimestampOrigen`, `TimestampRecepcion`, `Latitud`, `Longitud`, `Calidad` (Continua o Puntual), `Fuente`.

Reglas
- Coordenadas en rango.
- No se acepta un evento del futuro (tolerancia de 2 min) ni más antiguo que la ventana (parámetro, 15 min).
- Un duplicado (vehículo, instante, fuente) se acepta sin guardarlo de nuevo.
- `EstimadorEta` calcula la llegada con la velocidad observada, acotada entre 5 y 120 km/h. No se reasigna por cercanía GPS.

### 2.17 ConversacionBot (E)
Máquina de estados del bot de WhatsApp para pedir un servicio.

| Atributo | Tipo |
|---|---|
| UsuarioId, Estado | Inicio → origen → destino → fecha y hora → urgencia → confirmación → completa |
| Origen?, Destino?, FechaHoraUtc?, Urgente? | datos recogidos |
| ActualizadaEn, ExpiraEn | vigencia 30 min |

Reglas: comandos `solicitar`, `estado`, `cancelar`. Fechas en `yyyy-MM-dd HH:mm` o `dd/MM/yyyy HH:mm`, interpretadas en la zona del operador.

### 2.18 ParametroOperador (E)
`Clave`, `Valor`, `Version`, `ModificadoPor?`, `ModificadoEn`. Cada clave tiene un rango válido en un catálogo.

| Clave | Defecto |
|---|---|
| eta.umbral_minutos | 5 |
| recordatorio.anticipacion_minutos | 30 |
| solicitud.ventana_cancelacion_minutos | 60 |
| alerta.sin_conductor_minutos | 10 |
| documentos.dias_aviso | 30 |
| telemetria.ventana_evento_minutos | 15 |
| asignacion.ventana_reciente_horas | 24 |
| asignacion.maximo_intentos | 3 |
| viaje.duracion_estimada_minutos | 45 |

### 2.19 AlertaOperativa (E)
Aviso para el panel: `Tipo`, `Severidad` (Informativa, Atencion, Critica), `Titulo`, `Detalle`, `Referencia`, `ClaveDeduplicacion` (única), `CreadaEn`, `LeidaEn?`. La misma clave no genera dos alertas.

### 2.20 RegistroAuditoria (E)
`ActorId?`, `ActorNombre`, `Accion`, `Recurso`, `Resultado` (Aceptada o Rechazada), `Motivo?`, `Antes?`, `Despues?`, `Instante`. Solo se agrega, nunca se edita. Retención objetivo: 5 años.

### 2.21 Tablas de soporte (infraestructura)
- `outbox`: eventos pendientes de publicar.
- `mensajes_entrantes`: ids de WhatsApp ya procesados.
- `notificaciones_enviadas`: evita enviar dos veces el mismo mensaje.

---

## 3. Relaciones

```
Operador (1 por despliegue)
ClienteEmpresarial 1 ── * Pasajero * ── 1 Usuario
                   1 ── * Itinerario
Usuario 1 ── 1 Conductor * ── 0..1 Vehiculo
Pasajero 1 ── * SolicitudTransporte * ── 0..1 Itinerario
SolicitudTransporte 1 ── * Asignacion (una viva) * ── 1 Conductor
Asignacion 1 ── 0..1 Viaje 1 ── 0..1 Calificacion
Vehiculo 1 ── * RegistroUbicacion
```

Las referencias entre agregados son siempre por Id, nunca por objeto.

---

## 4. Flujo principal

```
Pasajero registra solicitud (app o WhatsApp)
  → evento SolicitudRegistrada (outbox)
  → aviso al pasajero + asignación automática
      ├─ hay candidato → Asignacion PendienteAceptacion → aviso al conductor
      │     ├─ ACEPTAR → Viaje Programado → aviso al pasajero
      │     │     → INICIO → FIN → el pasajero califica
      │     └─ RECHAZAR → vuelve a Pendiente → otro conductor
      └─ sin candidato / intentos agotados → EnRevision + alerta al operador
Operador logístico puede reasignar a mano en cualquier momento previo al inicio.
```

---

## 5. Casos de uso

Actor: **P** pasajero, **C** conductor, **OL** operador logístico, **AD** administrador, **S** sistema.

### 5.1 Usuarios y flota
| Caso de uso | Actor | Servicio |
|---|---|---|
| Crear usuario (con perfil de pasajero si aplica) | AD | `ServicioUsuarios` |
| Consultar y listar usuarios | AD, OL | |
| Actualizar datos, cambiar rol, activar y desactivar | AD | |
| Crear conductor con licencia | AD | `ServicioFlota` |
| Renovar licencia | AD | |
| Definir turnos | AD, OL | |
| Marcar disponible o no disponible (con motivo) | AD, OL | |
| Asignar o quitar vehículo | AD, OL | |
| Crear vehículo, registrar documentos, habilitar y deshabilitar | AD | |
| Crear, actualizar y activar clientes empresariales | AD | `ServicioClientes` |
| Crear y listar itinerarios | AD | |

### 5.2 Solicitudes y asignación
| Caso de uso | Actor | Servicio |
|---|---|---|
| Registrar solicitud | P, OL | `ServicioSolicitudes` |
| Consultar, listar (el pasajero solo ve las suyas) | P, OL, AD | |
| Modificar solicitud sin conductor | P, OL | |
| Cancelar solicitud (con ventana mínima para el pasajero) | P, OL | |
| Asignar automáticamente | S, OL | `ServicioAsignaciones` |
| Simular una asignación sin guardar | OL | |
| Reasignar manualmente con motivo | OL | |
| Aceptar o rechazar asignación | C | |
| Ver y cambiar la configuración del motor (versionada) | OL ver, AD cambiar | |

### 5.3 Ejecución y seguimiento
| Caso de uso | Actor | Servicio |
|---|---|---|
| Iniciar viaje | C | `ServicioViajes` |
| Finalizar viaje | C | |
| Reportar incidente | C | |
| Calificar viaje | P | |
| Reportar ubicación GPS y evaluar la desviación de ETA | S | `ServicioSeguimiento` |

### 5.4 Canal WhatsApp
| Caso de uso | Actor | Servicio |
|---|---|---|
| Pedir un servicio conversando con el bot | P | `ServicioConversaciones` |
| Consultar el estado de la última solicitud | P | |
| Calificar con `CALIFICAR n` | P | |
| `ACEPTAR`, `RECHAZAR`, `INICIO`, `FIN`, `INCIDENTE <texto>` | C | |

El mensaje se identifica por el teléfono; un mensaje repetido se procesa una sola vez.

### 5.5 Reacciones a eventos (sin intervención de un usuario)
| Evento | Reacción |
|---|---|
| SolicitudRegistrada | Avisa al pasajero y asigna |
| AsignacionCreada | Avisa al conductor |
| AsignacionAceptada | Avisa al pasajero con conductor y placa |
| AsignacionRechazada | Busca otro conductor |
| AsignacionReasignada | Avisa al conductor anterior |
| SinRecursoDisponible | Avisa al pasajero y crea alerta crítica |
| SolicitudCancelada | Avisa al conductor asignado |
| ViajeIniciado, ViajeFinalizado | Avisa al pasajero |
| RecordatorioViajeDebido | Recuerda el servicio al pasajero |
| DesviacionEtaDetectada | Avisa al pasajero y crea alerta |
| IncidenteReportado | Crea alerta crítica |

### 5.6 Tareas programadas
Cada minuto (`ServicioPlanificacion`): emite los recordatorios por anticipación, alerta las solicitudes que llevan demasiado esperando conductor y alerta documentos por vencer o vencidos.

### 5.7 Administración y consulta
| Caso de uso | Actor |
|---|---|
| Ver y ajustar parámetros del operador (con rango) | OL ver, AD ajustar |
| Ver y actualizar datos del operador | OL ver, AD actualizar |
| Consultar auditoría | AD |
| Ver alertas, contar no leídas, marcar leída | OL, AD |
| Reportes: viajes realizados, solicitudes por estado, conductores, operación por cliente, indicadores | OL, AD |

Los indicadores son: solicitudes, finalizadas, canceladas, sin conductor, % de asignación automática, % de puntualidad y calificación promedio.

---

## 6. Historias de usuario (formato para backlog)

Cada historia lleva criterios de aceptación; los de estas ya los cubren pruebas.

**HU-01 — Pedir un servicio.** Como pasajero quiero registrar una solicitud con origen, destino y hora para que me asignen conductor.
- Debe quedar Pendiente y asignarse sola en segundos.
- Con hora pasada o origen igual a destino se rechaza.

**HU-02 — Privacidad del servicio personal.** Como pasajero quiero marcar un servicio como personal para que la administración no vea mis lugares.
- Administración y operación ven origen y destino vacíos.

**HU-03 — Cancelar.** Como pasajero quiero cancelar mi solicitud para no pagar un servicio que no usaré.
- Con menos de 60 min (parámetro) se rechaza y debo contactar al operador.

**HU-04 — Responder una oferta.** Como conductor quiero aceptar o rechazar un servicio para gestionar mi jornada.
- Solo yo respondo mi asignación.
- Un rechazo busca otro conductor y no me vuelve a ofrecer ese servicio.

**HU-05 — Ejecutar el viaje.** Como conductor quiero iniciar y finalizar el viaje y reportar incidentes para dejar trazabilidad.
- No puedo tener dos viajes en curso con el mismo vehículo.

**HU-06 — Calificar.** Como pasajero quiero calificar de 1 a 5 el viaje terminado.
- Una sola calificación por viaje.

**HU-07 — Asignación explicable.** Como operador logístico quiero ver por qué se eligió a un conductor para poder auditar y corregir.
- La asignación guarda reglas aplicadas, descartados con motivo y ranking.
- Puedo simular sin guardar.

**HU-08 — Reasignar a mano.** Como operador logístico quiero reemplazar al conductor con un motivo para resolver imprevistos.
- Sin motivo se rechaza; el conductor debe ser asignable.
- Dos reasignaciones simultáneas no dejan dos asignaciones vivas.

**HU-09 — Ajustar el motor.** Como administrador quiero cambiar reglas y pesos sin desplegar para adaptarme al negocio.
- Cada cambio crea una versión; las asignaciones guardan la versión usada.

**HU-10 — Flota al día.** Como administrador quiero que el sistema me avise de documentos por vencer para no operar con vehículos o licencias vencidos.
- Un conductor o vehículo con documento vencido no recibe asignaciones.
- Se alerta una vez por documento y estado.

**HU-11 — Seguimiento.** Como pasajero quiero que me avisen si mi servicio llegará tarde.
- Se avisa una sola vez al superar el umbral de ETA y se rearma al normalizarse.

**HU-12 — Operar por WhatsApp.** Como pasajero o conductor quiero hacerlo todo por WhatsApp.
- Número desconocido: respuesta de no registrado.
- Mensaje repetido: se ignora.

**HU-13 — Seguridad por rol.** Como operador del sistema quiero que cada rol vea solo lo suyo.
- Sin token: 401. Rol insuficiente: 403.
- Usuario desactivado: 401 aunque su token siga vigente.
- Token de otro operador: 403.

**HU-14 — Auditoría.** Como administrador quiero ver quién hizo qué y cuándo.
- Las acciones sensibles y los rechazos quedan registrados.

---

## 7. Cómo lo construimos y qué falta

### Orden de construcción (ya recorrido)
1. **Dominio**: entidades, reglas, motor y eventos, con pruebas unitarias primero (145 en verde).
2. **Servicios**: puertos de salida, DTOs, casos de uso y manejadores de eventos.
3. **Infraestructura**: mapeo EF, migración inicial, repositorios, unidad de trabajo, outbox, planificador.
4. **API**: controladores, seguridad por rol, manejo de errores (Dominio→422, NotFound→404, Conflict→409, Forbidden→403), salud, telemetría.
5. **Verificación**: 145 pruebas de dominio, 44 reglas de arquitectura por módulo y 12 pruebas de integración sobre PostgreSQL real, todas en verde.

### Reglas de trabajo
- Una regla de negocio vive en el dominio y tiene su prueba antes de exponerse en la API.
- Todo cambio de estado que otros módulos necesiten sale como evento por el outbox; los manejadores son idempotentes.
- Los parámetros operativos se leen del catálogo, nunca se escriben fijos.
- Sin secretos en el repositorio.

### Pendiente
- **Adaptadores externos**: proveedor real de WhatsApp (firma del webhook), proveedores GPS, proveedor de identidad (Keycloak), Azure Blob (documentos), Key Vault, correo SMTP, proveedor de rutas. Hoy WhatsApp es un sandbox que solo escribe en el log con el teléfono enmascarado.
- **Pruebas**: escribir las pruebas de los casos de uso de cada módulo.
- **Operación**: caché de parámetros, retención y partición de la tabla de ubicaciones.
- **Decisión abierta**: alinear el estilo con `nota-service-api` (entidad y dominio separados, un caso de uso por operación). Hoy los agregados son ricos y los casos de uso se agrupan por módulo.
