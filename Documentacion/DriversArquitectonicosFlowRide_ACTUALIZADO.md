**UNIVERSIDAD CATÓLICA DE ORIENTE**

**Facultad de Ingenierías**

**Programa de Ingeniería de Sistemas**

**INGENIERÍA DEL SOFTWARE 2**

**Semestre 2-2026**

**PROYECTO EXTRACLASE --- FLOWRIDE**

Plataforma SaaS de Gestión de Transporte Corporativo Interno

**DRIVERS ARQUITECTÓNICOS**

**AUTORES:**

**Sebastián Zapata Naranjo**\
**Andrés David Ortiz Mejía**\
**Jhonny Estiven Benítez Caro**

**DOCENTE:**

**Wider Farid Sánchez Garzón**

**Rionegro, Antioquia**

Septiembre de 2026

# Drivers arquitectónicos de FlowRide

Los drivers arquitectónicos son los elementos que condicionan de manera
significativa la arquitectura de FlowRide. Para el proyecto se
consideran cuatro grupos: atributos de calidad priorizados,
funcionalidades arquitectónicamente significativas, restricciones de
negocio y restricciones técnicas.

Este documento utiliza como fuente de verdad para los atributos de
calidad las hojas **Tradeoff QA** y **Priorización QA** del análisis
arquitectónico. Por esta razón se trabaja con un conjunto cerrado de
**11 atributos de calidad**, evitando incorporar atributos adicionales
que no formen parte del análisis vigente.

# Atributos de calidad priorizados

La prioridad arquitectónica global adoptada para FlowRide utiliza una
escala de **11 a 1**, donde **11 representa la mayor prioridad y 1 la
menor prioridad relativa**.

  --------------------------------------------------------------------------
                     Prioridad Atributo de calidad     Aplicación en
                                                       FlowRide
  ---------------------------- ----------------------- ---------------------
                        **11** **Disponibilidad**      La operación depende
                                                       de la continuidad del
                                                       canal WhatsApp, del
                                                       motor de asignación y
                                                       del Panel Web. Se
                                                       manejan objetivos
                                                       diferenciados por
                                                       criticidad: WhatsApp
                                                       99,9 %, motor y
                                                       operaciones críticas
                                                       99,5 % y Panel Web 99
                                                       %.

                        **10** **Seguridad**           Protege identidad,
                                                       autorización, roles,
                                                       sesiones y acceso a
                                                       información. La
                                                       revocación de acceso
                                                       debe surtir efecto en
                                                       ≤ 60 s y los accesos
                                                       no autorizados deben
                                                       ser rechazados.

                         **9** **Rendimiento**         Las operaciones
                                                       críticas deben
                                                       responder bajo carga
                                                       sin degradar la
                                                       operación. Para
                                                       operaciones CRUD se
                                                       adopta P95 \< 500 ms
                                                       y P99 \< 1,5 s con
                                                       hasta 200 usuarios
                                                       concurrentes por
                                                       instancia.

                         **8** **Confiabilidad**       La plataforma debe
                                                       evitar asignaciones
                                                       duplicadas, pérdida
                                                       de eventos y estados
                                                       incoherentes,
                                                       manteniendo una
                                                       fuente de verdad
                                                       transaccional.

                         **7** **Aislamiento**         Cada operador de
                                                       transporte se maneja
                                                       como tenant con
                                                       aislamiento físico
                                                       mediante modelo Silo,
                                                       evitando acceso
                                                       cruzado entre
                                                       persistencias.

                         **6** **Interoperabilidad**   FlowRide debe
                                                       integrarse con
                                                       servicios externos
                                                       como WhatsApp y
                                                       telemetría GPS
                                                       mediante contratos y
                                                       adaptadores que
                                                       eviten acoplar el
                                                       dominio al proveedor.

                         **5** **Capacidad**           Una instancia debe
                                                       soportar el volumen
                                                       operativo definido,
                                                       incluidos hasta 200
                                                       usuarios concurrentes
                                                       y un dimensionamiento
                                                       base de 100 eventos
                                                       GPS/min por
                                                       instancia.

                         **4** **Capacidad para ser    Las acciones
                               auditado**              sensibles y eventos
                                                       relevantes deben
                                                       poder reconstruirse
                                                       mediante evidencia
                                                       verificable,
                                                       minimizando datos
                                                       personales
                                                       innecesarios.

                         **3** **Usabilidad - UX**     Pasajero y conductor
                                                       interactúan
                                                       principalmente
                                                       mediante WhatsApp y
                                                       los roles
                                                       administrativos
                                                       mediante Panel Web.
                                                       Los controles de
                                                       seguridad no deben
                                                       introducir fricción
                                                       innecesaria.

                         **2** **Escalabilidad**       El crecimiento de la
                                                       plataforma se
                                                       resuelve incorporando
                                                       nuevas instancias
                                                       Silo por operador,
                                                       evitando concentrar
                                                       la carga de todos los
                                                       tenants en una única
                                                       instancia.

                         **1** **Mantenibilidad**      El monolito modular,
                                                       la separación de
                                                       responsabilidades y
                                                       los adaptadores deben
                                                       permitir cambios
                                                       localizados sin
                                                       propagar
                                                       modificaciones
                                                       innecesarias por el
                                                       sistema.
  --------------------------------------------------------------------------

Los atributos **Accesibilidad** y **Portabilidad** no forman parte del
conjunto priorizado vigente. Aspectos como despliegue, testabilidad,
observabilidad, modificabilidad y reusabilidad pueden seguir siendo
prácticas o propiedades deseables de la solución, pero no se manejan
como drivers de calidad independientes dentro del Tradeoff vigente.

# Priorización por stakeholder

La hoja **Priorización QA** conserva los mismos 11 atributos y el mismo
orden físico. Cada stakeholder utiliza los valores del **11 al 1
exactamente una vez**, de acuerdo con sus necesidades. Cada columna suma
66 puntos.

  --------------------------------------------------------------------------------
  Atributo de calidad   Administrador      Conductor       Pasajero       Operador
                         del operador                                    logístico
  ------------------- --------------- -------------- -------------- --------------
  Disponibilidad                    9             11             11             11

  Seguridad                        11              8              9              7

  Rendimiento                       8             10             10             10

  Confiabilidad                    10              9              8              9

  Aislamiento                       7              3              4              4

  Interoperabilidad                 3              6              5              6

  Capacidad                         4              5              3              5

  Capacidad para ser                6              2              2              8
  auditado                                                          

  Usabilidad - UX                   5              7              7              3

  Escalabilidad                     2              4              6              2

  Mantenibilidad                    1              1              1              1

  **Total**                    **66**         **66**         **66**         **66**
  --------------------------------------------------------------------------------

La priorización por stakeholder no sustituye la prioridad arquitectónica
global de la hoja Tradeoff QA. Representa la importancia relativa que
cada actor asigna a cada atributo desde su interacción con FlowRide.

# Tradeoffs de calidad vigentes

  -----------------------------------------------------------------------------------------------------------------------------------
  ID       Atributo            Tensión            Criterio vigente /   Impacto aceptado   Escenarios          Decisión      Prioridad
                               arquitectónica     resolución                              relacionados        asociada    
  -------- ------------------- ------------------ -------------------- ------------------ ------------------- ----------- -----------
  T-01     Disponibilidad      Aumentar           Se aplican niveles   Se acepta mayor    COND-DISP-01,       ADR-0010 /           11
                               redundancia y      diferenciados:       complejidad y      PAS-DISP-01,        ADR-0012    
                               capacidad de       WhatsApp 99,9 %,     costo en           ADM-DISP-01,                    
                               recuperación       motor y operaciones  componentes        OPLOG-DISP-02                   
                               mejora             críticas 99,5 % y    críticos sin                                       
                               continuidad, pero  Panel Web 99 %. La   sobredimensionar                                   
                               incrementa         recuperación se      componentes                                        
                               infraestructura,   concentra en         administrativos.                                   
                               complejidad        componentes                                                             
                               operativa y costo  críticos.                                                               
                               por tenant.                                                                                

  T-02     Seguridad           Validar identidad, Se prioriza          Se acepta costo de ADM-SEG-01,         ADR-0002 /           10
                               rol, sesión y      protección de        validación siempre OPLOG-SEG-02,       ADR-0003    
                               límites de acceso  acceso,              que CRUD mantenga  ADM-SEG-02                      
                               agrega             autenticación        P95 \< 500 ms y                                    
                               procesamiento y    desacoplada,         P99 \< 1,5 s.                                      
                               complejidad a cada autorización por                                                        
                               petición.          roles y revocación ≤                                                    
                                                  60 s.                                                                   

  T-03     Rendimiento         La caché reduce    Solo se cachean      Se acepta menor    ADM-REND-01,        ADR-0006              9
                               latencia, pero     datos de baja        hit-rate y más     ADM-CAP-02                      
                               puede servir       volatilidad.         lecturas a                                         
                               información        Solicitudes,         persistencia en                                    
                               obsoleta.          asignaciones, viajes datos críticos                                     
                                                  en curso y           para conservar                                     
                                                  telemetría no        consistencia                                       
                                                  dependen de caché    operacional.                                       
                                                  como fuente de                                                          
                                                  verdad.                                                                 

  T-04     Confiabilidad       Garantizar         La fuente de verdad  Se acepta mayor    OPLOG-REND-01,      ADR-0007              8
                               consistencia añade permanece            complejidad        COND-CONF-02,                   
                               transacciones,     transaccional; se    transaccional      PAS-CONF-02                     
                               control de         aplican concurrencia antes que duplicar                                 
                               concurrencia,      optimista,           asignaciones,                                      
                               idempotencia y     restricciones de     perder eventos o                                   
                               reintentos.        unicidad y           generar estados                                    
                                                  mecanismos           incoherentes.                                      
                                                  confiables de                                                           
                                                  procesamiento de                                                        
                                                  eventos.                                                                

  T-05     Aislamiento         El modelo Silo     Se mantiene Silo     Se acepta mayor    OPLOG-SEG-02,       ADR-0002 /            7
                               mejora             como aislamiento     complejidad        ADM-SEG-02          ADR-0010    
                               confidencialidad y físico por operador  operativa a cambio                                 
                               contención de      y se automatizan     de aislamiento                                     
                               fallos, pero       aprovisionamiento,   físico                                             
                               multiplica         migraciones y        verificable.                                       
                               recursos y tareas  backups.                                                                
                               por tenant.                                                                                

  T-06     Interoperabilidad   WhatsApp,          Las integraciones se Se acepta          COND-DISP-01,       ADR-0008 /            6
                               telemetría e       encapsulan mediante  dependencia        PAS-DISP-03,        ADR-0012    
                               identidad dependen adaptadores; se      externa a cambio   COND-ACC-01,                    
                               de contratos y     aplican validación,  de integración y   OPLOG-REND-01                   
                               proveedores        idempotencia,        capacidad de                                       
                               externos fuera del reintentos y         sustitución del                                    
                               control directo de recuperación según   proveedor sin                                      
                               FlowRide.          el canal.            acoplar el                                         
                                                                       dominio.                                           

  T-07     Capacidad           Mayor frecuencia   Dimensionamiento     Se limita          OPLOG-INTEROP-04,   ADR-0005              5
                               de GPS mejora      base de 100          frecuencia y       PAS-CONF-01                     
                               frescura del ETA,  eventos/min por      retención en lugar                                 
                               pero aumenta       instancia y          de almacenar                                       
                               throughput,        retención GPS de 6   ubicación                                          
                               almacenamiento y   meses. La ubicación  indefinidamente o                                  
                               exposición de      se usa para          a frecuencia                                       
                               datos.             monitoreo y          extrema.                                           
                                                  desviación de ETA,                                                      
                                                  no para reasignación                                                    
                                                  dinámica.                                                               

  T-08     Capacidad para ser  Una auditoría más  Se conserva la       Se acepta mayor    OPLOG-AUD-01,       ADR-0011 /            4
           auditado            rica mejora        evidencia necesaria  almacenamiento a   PAS-AUD-02,         ADR-0013    
                               trazabilidad, pero para reconstruir     cambio de          OPLOG-AUD-02,                   
                               aumenta            acciones sensibles,  evidencia          PAS-AUD-03                      
                               almacenamiento,    evitando duplicar    verificable,                                       
                               costo y exposición datos personales     aplicando                                          
                               de información.    innecesarios.        minimización de                                    
                                                                       datos.                                             

  T-09     Usabilidad - UX     WhatsApp reduce    WhatsApp se mantiene Se acepta una      PAS-UX-01,          ADR-0003 /            3
                               fricción, pero las como canal de        interacción        COND-SEG-01,        ADR-0008    
                               acciones críticas  pasajero y           adicional cuando   PAS-SEG-01                      
                               y datos sensibles  conductor, con       protege datos o                                    
                               requieren          validación de        evita                                              
                               controles          identidad, permisos  suplantación.                                      
                               adicionales.       y exposición mínima.                                                    

  T-10     Escalabilidad       Crecer mediante    La plataforma        Se acepta          Pendiente de        Pendiente             2
                               instancias Silo    incorpora nuevas     crecimiento de     vincular con el     de validar  
                               evita concentrar   instancias Silo por  infraestructura    escenario           al revisar  
                               tenants, pero      operador y conserva  por tenant a       seleccionado de     ADR.        
                               aumenta            el dimensionamiento  cambio de          escalabilidad.                  
                               infraestructura y  de cada instancia.   aislamiento y de                                   
                               operación conforme                      evitar degradación                                 
                               crece la                                entre operadores.                                  
                               plataforma.                                                                                

  T-11     Mantenibilidad      Separación         Se mantiene un       Se acepta mayor    Pendiente de        Pendiente             1
                               modular,           monolito modular con disciplina y       vincular con el     de validar  
                               interfaces y       responsabilidades    estructura interna escenario           al revisar  
                               adaptadores        separadas y          a cambio de menor  seleccionado de     ADR.        
                               agregan estructura contratos explícitos acoplamiento y     mantenibilidad.                 
                               inicial, pero      para infraestructura cambios                                            
                               reducen el impacto e integraciones.     localizados.                                       
                               de cambios                                                                                 
                               futuros.                                                                                   
  -----------------------------------------------------------------------------------------------------------------------------------

Las asociaciones con ADR se conservan como referencias de trabajo y se
validarán en la revisión específica de ADR. No se crean códigos nuevos
de escenario ni decisiones no verificadas para Escalabilidad y
Mantenibilidad.

# Escenarios de calidad

Cada escenario utiliza seis elementos: **fuente, estímulo, artefacto,
entorno, respuesta y medida de respuesta**. Las medidas deben ser
verificables y cuantificables.

## QA-01 --- Disponibilidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Falla no planeada de un componente
                                      de ejecución

  Estímulo                            Una instancia deja de responder
                                      durante horario operativo

  Artefacto                           Canal WhatsApp, motor/operaciones
                                      críticas o Panel Web

  Entorno                             Producción

  Respuesta                           FlowRide aplica la estrategia de
                                      continuidad correspondiente al
                                      componente afectado preservando la
                                      operación y consistencia de datos

  Medida                              Disponibilidad mensual objetivo:
                                      WhatsApp ≥ 99,9 %; motor y
                                      operaciones críticas ≥ 99,5 %;
                                      Panel Web ≥ 99 %
  -----------------------------------------------------------------------

## QA-02 --- Seguridad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Usuario o actor no autorizado

  Estímulo                            Intenta acceder a una operación
                                      protegida con credenciales
                                      inválidas, expiradas, revocadas o
                                      sin el rol requerido

  Artefacto                           Autenticación y autorización de la
                                      API

  Entorno                             Producción

  Respuesta                           El sistema rechaza el acceso y no
                                      expone información protegida

  Medida                              100 % de solicitudes no autorizadas
                                      de las pruebas de seguridad son
                                      rechazadas; la revocación de acceso
                                      surte efecto en ≤ 60 s
  -----------------------------------------------------------------------

## QA-03 --- Rendimiento

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Usuario autenticado desde Panel Web
                                      o flujo operacional

  Estímulo                            Ejecuta una operación CRUD sobre la
                                      API

  Artefacto                           API REST y persistencia
                                      transaccional

  Entorno                             Pico de operación con hasta 200
                                      usuarios concurrentes por instancia

  Respuesta                           El sistema procesa y retorna la
                                      operación solicitada

  Medida                              P95 \< 500 ms y P99 \< 1,5 s para
                                      operaciones CRUD bajo el volumen
                                      definido
  -----------------------------------------------------------------------

## QA-04 --- Confiabilidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Dos procesos o solicitudes
                                      concurrentes

  Estímulo                            Intentan producir una asignación o
                                      transición de estado incompatible o
                                      duplicada

  Artefacto                           Motor de asignación y persistencia
                                      transaccional

  Entorno                             Operación normal o reintento
                                      después de una falla transitoria

  Respuesta                           El sistema conserva una única
                                      transición válida y evita
                                      duplicados

  Medida                              0 asignaciones duplicadas y 0
                                      transiciones inválidas en las
                                      pruebas de concurrencia e
                                      idempotencia definidas
  -----------------------------------------------------------------------

## QA-05 --- Aislamiento

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Usuario autenticado perteneciente
                                      al operador A

  Estímulo                            Intenta acceder a información
                                      persistida en el tenant del
                                      operador B

  Artefacto                           Resolución de tenant y
                                      persistencias Silo

  Entorno                             Producción multi-tenant

  Respuesta                           La petición no puede acceder ni
                                      enrutar operaciones hacia la
                                      persistencia de otro operador

  Medida                              0 fugas de datos entre tenants y
                                      100 % de pruebas automatizadas de
                                      acceso cruzado bloqueadas
  -----------------------------------------------------------------------

## QA-06 --- Interoperabilidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Servicio externo integrado

  Estímulo                            Envía un webhook o evento válido
                                      hacia FlowRide

  Artefacto                           Adaptador de integración
                                      correspondiente

  Entorno                             Operación normal

  Respuesta                           El adaptador valida el contrato y
                                      procesa o encola el evento sin
                                      acoplar el dominio al proveedor

  Medida                              100 % de casos válidos del contrato
                                      de integración superan las pruebas
                                      de contrato; para webhook de
                                      WhatsApp, reconocimiento o encolado
                                      ≤ 500 ms
  -----------------------------------------------------------------------

## QA-07 --- Capacidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Fuente de telemetría GPS y usuarios
                                      concurrentes

  Estímulo                            La instancia recibe el volumen
                                      operativo dimensionado

  Artefacto                           API, ingesta de telemetría y
                                      persistencia

  Entorno                             Producción en carga nominal

  Respuesta                           La instancia procesa el volumen sin
                                      superar los objetivos de
                                      rendimiento definidos

  Medida                              Hasta 200 usuarios concurrentes y
                                      100 eventos GPS/min por instancia
                                      manteniendo los umbrales de
                                      rendimiento aplicables
  -----------------------------------------------------------------------

## QA-08 --- Capacidad para ser auditado

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Administrador, operador logístico o
                                      proceso sensible

  Estímulo                            Ejecuta una acción que requiere
                                      trazabilidad

  Artefacto                           Mecanismo de auditoría

  Entorno                             Producción

  Respuesta                           Se registra evidencia suficiente
                                      para reconstruir actor, acción,
                                      instante y resultado sin duplicar
                                      datos personales innecesarios

  Medida                              100 % de las acciones catalogadas
                                      como auditables generan un registro
                                      verificable; la retención legal
                                      definida para auditoría es de 5
                                      años
  -----------------------------------------------------------------------

## QA-09 --- Usabilidad - UX

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Pasajero o conductor

  Estímulo                            Ejecuta un flujo habitual mediante
                                      WhatsApp

  Artefacto                           Canal conversacional de FlowRide

  Entorno                             Operación normal

  Respuesta                           El sistema guía el flujo y solicita
                                      controles adicionales únicamente
                                      cuando la operación sea sensible

  Medida                              100 % de los flujos críticos
                                      definidos para WhatsApp pueden
                                      completarse sin instalar una
                                      aplicación adicional; los pasos
                                      extra de validación se limitan a
                                      operaciones sensibles
  -----------------------------------------------------------------------

## QA-10 --- Escalabilidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Crecimiento de la plataforma

  Estímulo                            Se incorpora un nuevo operador de
                                      transporte

  Artefacto                           Plataforma FlowRide y
                                      aprovisionamiento Silo

  Entorno                             Producción

  Respuesta                           Se crea una nueva instancia Silo
                                      sin aumentar la carga operacional
                                      de las instancias existentes

  Medida                              El nuevo tenant conserva el techo
                                      de hasta 200 usuarios concurrentes
                                      por instancia y los objetivos de
                                      rendimiento vigentes sin degradar
                                      las instancias existentes
  -----------------------------------------------------------------------

## QA-11 --- Mantenibilidad

  -----------------------------------------------------------------------
  Parte                               Contenido
  ----------------------------------- -----------------------------------
  Fuente                              Equipo de desarrollo

  Estímulo                            Se modifica una regla de asignación
                                      o se sustituye una integración
                                      externa

  Artefacto                           Monolito modular, motor de reglas y
                                      adaptadores

  Entorno                             Desarrollo e integración

  Respuesta                           El cambio permanece localizado en
                                      el módulo o adaptador
                                      correspondiente y no obliga a
                                      modificar módulos no relacionados

  Medida                              0 modificaciones requeridas en
                                      módulos de dominio no relacionados
                                      y 100 % de las pruebas existentes
                                      del área afectada continúan
                                      aprobando después del cambio
  -----------------------------------------------------------------------

# Criterio de coherencia

A partir de esta versión, cualquier escenario, Building Block, táctica,
PoC/Spike o ADR que se incorpore al análisis debe poder relacionarse con
al menos uno de estos **11 atributos de calidad**. No se deben crear
nuevos atributos en hojas posteriores sin actualizar previamente
Tradeoff QA y Priorización QA.

# Especificación de Requisitos Funcionales

Esta sección consolida la especificación funcional vigente de FlowRide.
El catálogo queda cerrado en 56 requisitos funcionales, alineados con el
alcance actual del producto, el modelo B2B2B, el aislamiento Silo por
operador, WhatsApp como canal de pasajero y conductor, el motor de
asignación programado por reglas, la calificación del servicio y la
telemetría GPS para monitoreo y detección de desviación de ETA.

Los requisitos arquitectónicamente significativos se identifican por su
valor de negocio (VN) o por representar un reto tecnológico (RT). El
catálogo evita duplicidades funcionales y excluye capacidades que no
pertenecen al alcance actual del producto, de modo que la numeración
RF-01 a RF-56 constituye la línea base funcional de este documento.

## **Módulo 1 --- Autenticación y Autorización**

RF-01 --- Autenticación mediante mecanismo de identidad basado en token

  -------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -------------------------------------------------------------------
  Actor             Todos los usuarios del sistema (Pasajero, Conductor, Administrador
                    del operador, Operador logístico)

  Descripción       El sistema deberá autenticar y validar la identidad de los usuarios
                    mediante un mecanismo basado en tokens y un proveedor de identidad
                    desacoplado. El sistema usa un proveedor de identidad desacoplado y
                    tokens de acceso estándar; la comparación y selección del proveedor
                    se documenta en ADR-03.

  Entradas          Correo electrónico y contraseña del usuario

  Proceso           Valida credenciales y estado del usuario mediante el proveedor de
                    identidad configurado y obtiene/emite un token de acceso con los
                    atributos mínimos necesarios para autorización, sin usar empresaId
                    como mecanismo de aislamiento entre tenants.

  Salidas           Token de acceso válido + información básica del usuario y rol

  Precondiciones    El usuario debe estar registrado y activo en el sistema

  Postcondiciones   El usuario puede realizar peticiones autenticadas mientras su
                    sesión/token sea válido según la política de sesión y expiración
                    configurada

  Prioridad         Alta
  -------------------------------------------------------------------------------------

**RF-02 --- Autorización basada en roles**

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Todos los usuarios autenticados

  Descripción       El sistema deberá validar en cada petición a endpoints
                    protegidos que el usuario posea el rol requerido para ejecutar
                    la acción, rechazando la petición si no cumple.

  Entradas          Token de acceso válido en el encabezado Authorization

  Proceso           Valida el rol y permisos requeridos para la operación mediante
                    el mecanismo de autorización implementado por la aplicación,
                    manteniendo desacople respecto al proveedor de identidad

  Salidas           Respuesta HTTP 200 (autorizado) o HTTP 403 (no autorizado)

  Precondiciones    El usuario debe estar autenticado con un token válido y no
                    expirado

  Postcondiciones   Solo los usuarios con el rol adecuado pueden ejecutar cada
                    acción del sistema

  Prioridad         Alta
  ----------------------------------------------------------------------------------

## Módulo 2 --- Gestión de Operadores / Tenants

RF-03 --- Registro de operador de transporte como tenant

  ------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ------------------------------------------------------------------
  Actor             Administrador técnico / proceso de aprovisionamiento SaaS

  Descripción       El sistema deberá permitir registrar un nuevo operador de
                    transporte como tenant de FlowRide y aprovisionar su entorno de
                    datos aislado físicamente mediante el modelo Silo.

  Entradas          Nombre o razón social del operador, NIT, correo de contacto y
                    datos mínimos de aprovisionamiento

  Proceso           Valida la identidad del operador, crea el identificador del tenant
                    y dispara el aprovisionamiento de su persistencia aislada. El
                    aprovisionamiento crea la persistencia aislada del tenant;
                    migraciones y respaldos siguen la estrategia operativa documentada
                    en ADR-02.

  Salidas           Tenant del operador registrado y entorno Silo preparado para
                    configuración

  Precondiciones    El operador no debe existir previamente como tenant

  Postcondiciones   El operador queda habilitado para configurar usuarios,
                    conductores, vehículos, reglas y clientes empresariales dentro de
                    su propia instancia

  Prioridad         Alta
  ------------------------------------------------------------------------------------

RF-04 --- Configuración inicial del tenant

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Administrador del operador

  Descripción       El sistema deberá permitir configurar los parámetros base de
                    operación del tenant: usuarios administrativos, recursos
                    operativos y parámetros necesarios para iniciar la operación.

  Entradas          Datos de usuarios administrativos, recursos y parámetros
                    operativos del tenant

  Proceso           Persiste la configuración dentro de la instancia Silo del
                    operador

  Salidas           Confirmación de configuración aplicada

  Precondiciones    La empresa debe estar previamente registrada (RF-03)

  Postcondiciones   El tenant puede operar con sus reglas y recursos sin afectar
                    otros operadores

  Prioridad         Alta
  ----------------------------------------------------------------------------------

## **Módulo 3 --- Gestión de Usuarios**

RF-05 --- Creación de usuarios por tenant

  ------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ------------------------------------------------------------------
  Actor             Administrador del operador

  Descripción       El sistema deberá permitir al administrador crear usuarios del
                    tenant (pasajeros, conductores, operadores logísticos y
                    administradores) con el rol y asociación operacional
                    correspondiente.

  Entradas          Nombre, correo, teléfono, rol, código interno del usuario

  Proceso           Valida la unicidad aplicable dentro del tenant y persiste el
                    usuario en la instancia del operador. La gestión de credenciales
                    se delega al proveedor de identidad; la aplicación no almacena
                    contraseñas en texto plano ni acopla el dominio a un emisor
                    específico de tokens.

  Salidas           Usuario creado con su identificador único y credenciales para
                    primer login

  Precondiciones    El administrador debe estar autenticado y autorizado dentro del
                    tenant

  Postcondiciones   El usuario puede autenticarse y operar según su rol dentro del
                    tenant

  Prioridad         Alta
  ------------------------------------------------------------------------------------

**RF-06 --- Asignación de roles a usuarios**

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Administrador del operador

  Descripción       El sistema deberá permitir asignar o modificar el rol de un
                    usuario existente dentro de la empresa (Pasajero, Conductor,
                    Operador Logístico, Administrador).

  Entradas          Identificador del usuario, nuevo rol

  Proceso           Valida que el rol exista en el catálogo, actualiza el rolId del
                    usuario y registra la acción en auditoría

  Salidas           Usuario con rol actualizado

  Precondiciones    El usuario y el rol deben existir dentro del mismo tenant; el
                    administrador debe estar autorizado

  Postcondiciones   Los permisos del usuario se actualizan de acuerdo con la
                    política de sesión, roles y revocación configurada

  Prioridad         Alta
  ----------------------------------------------------------------------------------

## **Módulo 4 --- Gestión de Conductores**

**RF-07 --- Consulta de disponibilidad de conductores**

  ------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ------------------------------------------------------------------
  Actor             Motor de Asignación (proceso interno) + Operador Logístico

  Descripción       El sistema deberá permitir consultar los conductores disponibles
                    para una franja o servicio dentro del tenant, filtrando por estado
                    y criterios operativos programados. La ubicación GPS no se utiliza
                    para reasignación dinámica por cercanía.

  Entradas          Identificador del tenant y filtros operativos (estado, turno,
                    vehículo asignado u otros definidos por reglas)

  Proceso           Consulta conductores activos y disponibles dentro de la instancia
                    del operador, respetando las reglas programadas de disponibilidad

  Salidas           Listado de conductores disponibles con sus datos operativos

  Precondiciones    Debe existir al menos un conductor registrado y activo en el
                    tenant

  Postcondiciones   La consulta no modifica el estado del sistema

  Prioridad         Media
  ------------------------------------------------------------------------------------

## **Módulo 5 --- Gestión de Itinerarios y Solicitudes**

**RF-08 --- Registro de itinerarios**

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Operador Logístico

  Descripción       El sistema deberá permitir registrar itinerarios de transporte
                    que definen los servicios recurrentes o programados de la
                    empresa (origen, destino, horarios, código de servicio).

  Entradas          Código de servicio, fecha, origen, destino, hora de salida, hora
                    de llegada

  Proceso           Valida los datos y persiste el itinerario dentro del tenant.
                    Cuando corresponda, empresaId identifica al cliente empresarial
                    atendido por el operador, no al tenant.

  Salidas           Itinerario registrado con identificador único

  Precondiciones    El operador debe estar autenticado y tener rol autorizado

  Postcondiciones   El itinerario queda disponible como referencia para solicitudes
                    de transporte

  Prioridad         Alta
  ----------------------------------------------------------------------------------

**RF-09 --- Recepción de solicitudes de transporte**

  -----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -----------------------------------------------------------------
  Actor             Pasajero (vía WhatsApp)

  Descripción       El sistema deberá recibir solicitudes de transporte de los
                    pasajeros, validarlas y registrarlas en la cola de asignación.

  Entradas          Identificador del pasajero, itinerario asociado (opcional),
                    dirección origen, dirección destino, fecha y hora, indicador de
                    urgencia y atributo esPersonal cuando aplique

  Proceso           Valida datos mínimos, crea la solicitud con estado Pendiente
                    dentro del tenant del operador y registra empresaId solo como
                    referencia al cliente empresarial atendido cuando corresponda

  Salidas           Solicitud registrada con identificador único y confirmación al
                    pasajero

  Precondiciones    El pasajero debe estar identificado dentro de un tenant activo

  Postcondiciones   La solicitud queda encolada para asignación automática

  Prioridad         Alta
  -----------------------------------------------------------------------------------

## **Módulo 6 --- Motor de Asignación**

**RF-10 --- Asignación automática de conductores**

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Motor de Asignación (proceso interno disparado por nueva
                    solicitud)

  Descripción       El sistema deberá asignar automáticamente un conductor y un
                    vehículo a cada solicitud pendiente, aplicando las reglas de
                    negocio programadas y configurables del tenant.

  Entradas          Solicitud de transporte en estado Pendiente

  Proceso           Recupera conductores disponibles, aplica las reglas de
                    asignación configuradas por el tenant, selecciona el mejor
                    candidato y crea la asignación asociada

  Salidas           Asignación creada con conductor y vehículo; solicitud
                    actualizada a estado Asignada

  Precondiciones    Debe existir al menos un conductor disponible y las reglas del
                    tenant deben estar configuradas

  Postcondiciones   Se notifica al conductor asignado (RF-17); la solicitud queda en
                    flujo operativo

  Prioridad         Alta
  ----------------------------------------------------------------------------------

**RF-11 --- Aplicación de reglas de negocio para la asignación**

  -------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -------------------------------------------------------------------
  Actor             Motor de Asignación

  Descripción       El sistema deberá aplicar reglas de negocio configurables por
                    tenant, tales como disponibilidad programada, minimización de
                    cambios de turno, balance de carga, distancia del servicio
                    origen-destino y restricciones regulatorias. La telemetría GPS se
                    usa para monitoreo y desviación de ETA, no para reasignación
                    dinámica por cercanía.

  Entradas          Solicitud de transporte, configuración de reglas del tenant y
                    conductores disponibles

  Proceso           Ejecuta las reglas activas, filtra y pondera candidatos según la
                    configuración del operador y registra las razones de la decisión
                    para trazabilidad

  Salidas           Conductor seleccionado o notificación de imposibilidad de
                    asignación

  Precondiciones    Las reglas deben estar configuradas por el Administrador del
                    operador

  Postcondiciones   La decisión y las reglas aplicadas quedan disponibles para
                    auditoría y trazabilidad

  Prioridad         Alta
  -------------------------------------------------------------------------------------

**RF-12 --- Reasignación manual de conductores**

  -----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -----------------------------------------------------------------
  Actor             Operador Logístico

  Descripción       El sistema deberá permitir al Operador logístico reasignar
                    manualmente una asignación cuando exista una contingencia
                    operativa, por ejemplo, cuando un conductor abandone su turno o
                    quede indisponible.

  Entradas          Identificador de la asignación, nuevo conductor y vehículo

  Proceso           Valida disponibilidad del nuevo conductor, actualiza la
                    asignación, notifica a ambos conductores (el liberado y el nuevo)
                    y registra la acción en auditoría

  Salidas           Asignación actualizada; notificaciones enviadas

  Precondiciones    La asignación debe existir y el operador debe pertenecer al mismo
                    tenant

  Postcondiciones   La nueva asignación queda vigente; la anterior queda registrada
                    en histórico

  Prioridad         Alta
  -----------------------------------------------------------------------------------

**RF-13 --- Priorización de solicitudes urgentes**

  -----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -----------------------------------------------------------------
  Actor             Motor de Asignación

  Descripción       El sistema deberá priorizar solicitudes marcadas como urgentes o
                    críticas de acuerdo con reglas configuradas por el tenant, sin
                    convertir el motor en un esquema de competencia dinámica por
                    proximidad.

  Entradas          Solicitud con marca urgente = true

  Proceso           Prioriza la solicitud dentro de la lógica programada del motor y,
                    cuando no exista capacidad suficiente, genera una situación de
                    excepción para intervención del Operador logístico conforme a las
                    reglas vigentes.

  Salidas           Asignación priorizada o alerta de imposibilidad de asignación

  Precondiciones    La solicitud debe estar marcada como urgente y el tenant debe
                    tener reglas de priorización activas

  Postcondiciones   La decisión queda registrada para trazabilidad y, si requiere
                    intervención, se notifica al Operador logístico

  Prioridad         Media
  -----------------------------------------------------------------------------------

## **Módulo 7 --- Integración con WhatsApp**

**RF-14 --- Recepción de mensajes desde WhatsApp**

  ------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ------------------------------------------------------------------
  Actor             Plataforma WhatsApp Business / proveedor seleccionado en ADR-0008

  Descripción       El sistema deberá recibir mensajes entrantes de pasajero y
                    conductor vía webhook HTTP desde la plataforma WhatsApp Business.
                    La integración se implementa mediante un adaptador desacoplado del
                    proveedor, preservando el contrato funcional del webhook y del
                    envío de mensajes.

  Entradas          Payload HTTP POST del proveedor seleccionado con contenido del
                    mensaje, número de origen y metadatos

  Proceso           Valida autenticidad del webhook según el proveedor seleccionado,
                    identifica al usuario y despacha el mensaje al flujo
                    conversacional correspondiente

  Salidas           Respuesta HTTP conforme al contrato del proveedor; mensaje
                    disponible para procesamiento

  Precondiciones    El usuario debe tener número de WhatsApp asociado a su cuenta

  Postcondiciones   El mensaje queda disponible para procesamiento por el bot
                    conversacional

  Prioridad         Alta
  ------------------------------------------------------------------------------------

**RF-15 --- Procesamiento de solicitudes enviadas por WhatsApp**

  ----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ----------------------------------------------------------------
  Actor             Bot conversacional interno

  Descripción       El sistema deberá interpretar los mensajes conversacionales del
                    pasajero, mantener el estado de la conversación y convertirlos
                    en solicitudes de transporte estructuradas.

  Entradas          Mensaje entrante + estado de conversación previo

  Proceso           Aplica la máquina de estados conversacional, extrae datos
                    (origen, destino, fecha, urgencia), y cuando la solicitud está
                    completa la persiste vía RF-09

  Salidas           Solicitud de transporte creada; respuesta al pasajero
                    solicitando el siguiente dato o confirmando

  Precondiciones    El mensaje debe provenir de un usuario identificado

  Postcondiciones   La solicitud queda registrada o la conversación continúa
                    esperando más datos

  Prioridad         Alta
  ----------------------------------------------------------------------------------

**RF-16 --- Envío de confirmaciones al usuario por WhatsApp**

  ---------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ---------------------------------------------------------------
  Actor             Servicio de Notificaciones

  Descripción       El sistema deberá enviar al pasajero una confirmación por
                    WhatsApp cuando su solicitud haya sido registrada y cuando haya
                    sido asignada.

  Entradas          Evento de cambio de estado de solicitud (Pendiente → Asignada)

  Proceso           Genera el mensaje de confirmación con los datos operativos
                    pertinentes y lo envía mediante el adaptador de WhatsApp
                    configurado para FlowRide

  Salidas           Mensaje entregado al pasajero

  Precondiciones    La solicitud debe estar en estado Asignada

  Postcondiciones   El pasajero conoce el estado y detalles de su solicitud

  Prioridad         Alta
  ---------------------------------------------------------------------------------

**RF-17 --- Envío de notificaciones al conductor asignado**

  ---------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ---------------------------------------------------------------
  Actor             Servicio de Notificaciones

  Descripción       El sistema deberá notificar al conductor por WhatsApp cuando se
                    le asigne un nuevo servicio, incluyendo los datos del pasajero,
                    origen, destino y hora.

  Entradas          Evento de creación de asignación

  Proceso           Genera el mensaje con los detalles del servicio y lo envía al
                    número WhatsApp del conductor

  Salidas           Notificación entregada al conductor

  Precondiciones    El conductor debe tener número WhatsApp registrado

  Postcondiciones   El conductor puede aceptar o rechazar la asignación desde
                    WhatsApp

  Prioridad         Alta
  ---------------------------------------------------------------------------------

**RF-18 --- Actualización de estado del viaje desde WhatsApp**

  -----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -----------------------------------------------------------------
  Actor             Conductor (vía WhatsApp)

  Descripción       El sistema deberá permitir al conductor actualizar el estado del
                    viaje (inicio, finalización, incidente) enviando mensajes por
                    WhatsApp que se sincronicen con el estado interno del sistema.

  Entradas          Mensaje del conductor con palabra clave (INICIO, FIN,
                    INCIDENTE) + viajeId implícito por conversación

  Proceso           Interpreta el comando, valida que el conductor esté asignado al
                    viaje, actualiza el estado y notifica al pasajero y al operador
                    si corresponde

  Salidas           Estado del viaje actualizado; notificación de confirmación al
                    conductor

  Precondiciones    El conductor debe tener una asignación activa

  Postcondiciones   El estado del viaje refleja la realidad operativa; se disparan
                    las notificaciones correspondientes

  Prioridad         Alta
  -----------------------------------------------------------------------------------

## Módulo 8 --- Multi-tenancy (modelo Silo)

RF-19 --- Aislamiento físico entre tenants (modelo Silo)

  ------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- ------------------------------------------------------------------
  Actor             Sistema (transversal a todas las operaciones)

  Descripción       El sistema deberá garantizar aislamiento físico entre operadores
                    de transporte (tenants) mediante el modelo Silo, evitando que una
                    sesión o proceso de un operador acceda a la persistencia de otro.

  Entradas          Petición autenticada con contexto de tenant/operador resuelto por
                    la plataforma

  Proceso           Resuelve el tenant de la petición y enruta el acceso únicamente
                    hacia la instancia y persistencia Silo del operador autorizado.
                    empresaId no actúa como filtro de aislamiento entre tenants;
                    identifica al cliente empresarial dentro de la BD del operador.

  Salidas           Respuesta obtenida exclusivamente desde el entorno de datos del
                    tenant autorizado

  Precondiciones    El usuario debe estar autenticado y asociado a un tenant válido

  Postcondiciones   No existe acceso cruzado entre persistencias de tenants; el
                    aislamiento se verifica con pruebas automatizadas y controles de
                    aprovisionamiento

  Prioridad         Alta
  ------------------------------------------------------------------------------------

RF-20 --- Configuración separada de usuarios y recursos por tenant

  -----------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -----------------------------------------------------------------
  Actor             Administrador del operador

  Descripción       El sistema deberá permitir que cada tenant tenga su propia
                    configuración de usuarios, conductores, vehículos, reglas de
                    asignación y parámetros operativos dentro de su instancia Silo.

  Entradas          Datos de configuración específicos del tenant

  Proceso           Persiste la configuración dentro del entorno del tenant,
                    garantizando que los cambios no afecten instancias de otros
                    operadores

  Salidas           Configuración aplicada exclusivamente al tenant del operador

  Precondiciones    El administrador debe estar autenticado y autorizado en el tenant

  Postcondiciones   Cada tenant opera con su configuración independiente

  Prioridad         Alta
  -----------------------------------------------------------------------------------

## **Módulo 9 --- Configuración Avanzada**

**RF-21 --- Configuración avanzada de reglas de asignación**

  -------------------------------------------------------------------------------------
  **Campo**         **Contenido**
  ----------------- -------------------------------------------------------------------
  Actor             Administrador del operador

  Descripción       El sistema deberá permitir definir, activar, desactivar y ordenar
                    reglas de asignación aplicables al tenant (por ejemplo:
                    disponibilidad programada, minimización de cambios de turno,
                    balance de carga, distancia del servicio y restricciones
                    regulatorias). No se usa cercanía GPS para reasignación dinámica.

  Entradas          Definición de reglas con parámetros, orden de aplicación y
                    condiciones de activación

  Proceso           Valida la coherencia de las reglas, las persiste dentro del tenant
                    y las pone a disposición del motor para solicitudes posteriores

  Salidas           Reglas configuradas y activas para el tenant

  Precondiciones    El administrador debe pertenecer al tenant que configura

  Postcondiciones   El motor de asignación aplica las nuevas reglas a partir de la
                    siguiente solicitud

  Prioridad         Media
  -------------------------------------------------------------------------------------

## Capacidades V1 complementarias

Las siguientes capacidades forman parte del alcance V1 y completan la
línea base funcional: RF-54 Calificación pasajero → conductor, RF-55
Ingesta de telemetría GPS y RF-56 Detección de desviación de ETA.

### RF-54 --- Calificación del servicio

  --------------------------------------------------------------------------------
  Campo             Contenido
  ----------------- --------------------------------------------------------------
  Actor             Pasajero vía WhatsApp

  Descripción       El sistema deberá permitir que el pasajero califique al
                    conductor al finalizar un Viaje, mediante un puntaje y
                    comentario opcional.

  Entradas          viajeId, puntaje, comentario opcional

  Proceso           Valida que el viaje haya finalizado y que el pasajero esté
                    asociado al servicio; registra la Calificacion vinculada al
                    Viaje.

  Salidas           Confirmación de calificación registrada

  Precondiciones    Viaje finalizado y pasajero autorizado para calificarlo

  Postcondiciones   La calificación queda disponible para reportes operacionales
                    agregados por conductor, cliente empresarial u operador

  Prioridad         Alta
  --------------------------------------------------------------------------------

### RF-55 --- Ingesta de telemetría GPS

  ----------------------------------------------------------------------------------
  Campo             Contenido
  ----------------- ----------------------------------------------------------------
  Actor             Fuente de telemetría GPS configurada para el tenant

  Descripción       El sistema deberá recibir y almacenar eventos de ubicación de
                    los vehículos/conductores para viajes en curso.

  Entradas          vehiculoId, viajeId cuando aplique, timestamp, latitud, longitud
                    y metadatos de la fuente

  Proceso           Valida el evento, identifica el tenant y el viaje, y persiste
                    RegistroUbicacion según la estrategia de persistencia de
                    telemetría documentada para FlowRide.

  Salidas           Evento de ubicación aceptado o rechazado con trazabilidad

  Precondiciones    Fuente de telemetría autenticada/autorizada y vehículo
                    identificado

  Postcondiciones   La ubicación queda disponible para monitoreo y análisis de
                    desviación de ETA, no para reasignación dinámica por cercanía

  Prioridad         Alta
  ----------------------------------------------------------------------------------

### RF-56 --- Detección de desviación de ETA

  ---------------------------------------------------------------------------------
  Campo             Contenido
  ----------------- ---------------------------------------------------------------
  Actor             Módulo de Telemetría / sistema

  Descripción       El sistema deberá detectar desviaciones relevantes de ETA
                    durante viajes en curso a partir de la telemetría GPS y generar
                    una alerta operativa.

  Entradas          Secuencia de RegistroUbicacion + datos programados del Viaje

  Proceso           Evalúa la ubicación y el progreso del viaje contra la ETA
                    programada y los umbrales configurados para el servicio.

  Salidas           Estado/alerta de desviación para Administrador del operador y
                    Operador logístico; notificación proactiva cuando corresponda

  Precondiciones    Viaje en curso con telemetría disponible

  Postcondiciones   La desviación queda registrada para monitoreo y trazabilidad

  Prioridad         Alta
  ---------------------------------------------------------------------------------

## **Catálogo abreviado --- Requisitos no significativos para la arquitectura**

Los siguientes requisitos completan la línea base funcional de FlowRide.
Corresponden a operaciones necesarias del producto que no condicionan
por sí solas la arquitectura, porque se resuelven mediante patrones
conocidos, reutilizan decisiones transversales o tienen menor impacto
arquitectónico. Los reportes operacionales básicos y la observabilidad
técnica permanecen dentro del alcance.

**Autenticación y acceso (2):**

-   RF-22 Cierre de sesión --- permite al usuario cerrar su sesión
    activa e invalidar el contexto local.

-   RF-23 Recuperación o restablecimiento de credenciales --- permite al
    usuario recuperar el acceso mediante correo electrónico.

**Gestión de usuarios (2):**

-   RF-24 Actualización de información de usuarios --- permite modificar
    datos personales de los usuarios existentes.

-   RF-25 Activación e inactivación de usuarios --- permite al
    Administrador del operador habilitar o deshabilitar el acceso de un
    usuario.

**Gestión de conductores (3):**

-   RF-26 Registro de conductores --- permite dar de alta un nuevo
    conductor con sus datos operativos y licencia.

-   RF-27 Actualización de datos de conductores --- permite modificar
    información del conductor.

-   RF-28 Activación e inactivación de conductores --- permite habilitar
    o deshabilitar operativamente a un conductor.

**Gestión de itinerarios y solicitudes (4):**

-   RF-29 Validación de datos mínimos de solicitud --- verifica que la
    solicitud contenga los campos obligatorios.

-   RF-30 Consulta del estado de solicitudes --- permite al pasajero u
    operador ver el estado actual de una solicitud.

-   RF-31 Historial de solicitudes y viajes --- permite consultar el
    histórico de solicitudes y viajes por usuario o cliente empresarial.

-   RF-32 Modificación o cancelación de solicitudes --- permite cancelar
    o ajustar una solicitud conforme a las reglas del flujo operativo.

**Asignación de transporte (2):**

-   RF-33 Visualización de asignaciones generadas --- permite al
    operador ver las asignaciones creadas por el motor.

-   RF-34 Actualización del estado de asignación --- permite marcar una
    asignación como aceptada, rechazada, en curso o finalizada.

**Integración con WhatsApp (1):**

-   RF-35 Envío de recordatorios automáticos --- envía recordatorios
    programados de viajes próximos al pasajero y al conductor.

**Panel web administrativo (7):**

-   RF-36 Visualización centralizada de solicitudes y viajes --- muestra
    una vista unificada de la operación.

-   RF-37 Monitoreo operativo de servicios en curso --- permite ver en
    tiempo real los servicios activos.

-   RF-38 Consulta de usuarios, conductores y asignaciones --- permite
    consultar el catálogo operativo del tenant.

-   RF-39 Intervención manual en la operación --- permite al Operador
    logístico tomar control manual de un servicio cuando exista una
    contingencia.

-   RF-40 Consulta de trazabilidad de cada viaje --- muestra el
    histórico completo de eventos de un viaje.

-   RF-41 Búsqueda y filtrado de información --- permite filtrar y
    buscar en los listados operativos.

-   RF-42 Visualización de indicadores básicos --- muestra métricas
    operacionales simples de la operación diaria.

**Notificaciones (3):**

-   RF-43 Notificación automática de cambios de estado --- notifica al
    usuario cuando cambia el estado de su servicio.

-   RF-44 Notificación de reasignación o novedades --- informa al
    pasajero y conductor sobre cambios operativos relevantes.

-   RF-45 Alertas operativas para administradores u operadores ---
    notifica eventos que requieren atención.

**Reportes (4):**

-   RF-46 Reporte de viajes realizados --- genera reporte de viajes
    completados en un período.

-   RF-47 Reporte de solicitudes atendidas y pendientes --- muestra el
    estado agregado de solicitudes.

-   RF-48 Reporte de conductores asignados --- muestra actividad y carga
    de trabajo por conductor.

-   RF-49 Reporte histórico de operación por cliente empresarial ---
    genera un reporte agregado histórico dentro del tenant.

**Logging y monitoreo (4):**

-   RF-50 Registro de eventos del sistema --- registra eventos
    operativos para trazabilidad técnica.

-   RF-51 Registro de errores y excepciones --- registra errores
    técnicos con contexto para diagnóstico.

-   RF-52 Seguimiento de acciones de usuarios administrativos ---
    registra en auditoría las acciones sensibles.

-   RF-53 Monitoreo técnico de disponibilidad del sistema --- expone
    métricas de disponibilidad y salud del sistema.

Este filtro se aplica sobre la línea base funcional RF-01 a RF-56
definida en este documento. Una funcionalidad se considera
arquitectónicamente significativa cuando aporta valor de negocio
diferencial o introduce un reto tecnológico que condiciona decisiones de
diseño, integración, persistencia, seguridad, disponibilidad o
despliegue.

# Filtro de significancia arquitectónica

(recordatorio):

Una funcionalidad es significativa arquitectónicamente si cumple al
menos una condición:

Valor de negocio (VN): es lo que diferencia a FlowRide; el negocio no
existe sin esto.

Reto tecnológico (RT): hay que investigar o probar cómo hacerlo; no es
un patrón conocido de escritorio.

Todo lo demás son dependencias: cosas necesarias, pero que no marcan la
arquitectura porque son patrones CRUD estándar o features secundarias.

## Tabla A --- Requisitos funcionales SIGNIFICATIVOS para la arquitectura

  ------------------------------------------------------------------------------------------------------
  **\#**   **Requisito      **Módulo**       **Tipo**   **Justificación**
           funcional**                                  
  -------- ---------------- ---------------- ---------- ------------------------------------------------
  RF-01    Autenticación    Autenticación    RT         Reto técnico vigente por selección de proveedor,
           mediante                                     estrategia de token, revocación y desacople. La
           mecanismo de                                 autenticación se mantiene desacoplada del
           identidad basado                             proveedor concreto; ADR-03 documenta la decisión
           en token                                     tecnológica sin alterar este requisito
                                                        funcional.

  RF-02    Autorización     Autenticación    VN         Los cuatro roles operativos (Pasajero,
           basada en roles                              Conductor, Administrador del operador, Operador
                                                        logístico) son parte del modelo específico de
                                                        transporte corporativo interno.

  RF-03    Registro de      Gestión de       VN + RT    Es el punto de entrada del SaaS multi-tenant.
           operador de      tenants                     Cada alta implica aprovisionar un tenant con
           transporte como                              aislamiento físico Silo, incluyendo su
           tenant                                       persistencia, configuración operativa y ciclo
                                                        indesujeto a migraciones y respaldo.

  RF-04    Configuración    Gestión de       VN         Habilita que cada tenant configure sus propios
           inicial del      tenants                     recursos y reglas dentro de su instancia
           operador                                     aislada.

  RF-05    Creación de      Gestion de       VN         Alta de usuarios dentro del tenant. La
           usuarios por     usuarios                    asociación al operador sustituye el uso de
           tenant                                       empresaId como mecanismo de aislamiento entre
                                                        tenants.

  RF-06    Asignación de    Gestión de       VN         Habilita los cuatro roles operativos que son
           roles a usuarios usuarios                    diferenciador (ver RF-02).

  RF-07    Consulta de      Gestión de       VN         Es el insumo crítico del motor de asignación.
           disponibilidad   conductores                 Sin esto, no hay asignación automática.
           de conductores                               

  RF-08    Registro de      Itinerarios y    VN         El itinerario es el objeto central del dominio.
           itinerarios      solicitudes                 Sin itinerario no hay servicio que asignar.

  RF-09    Recepción de     Itinerarios y    VN         Core del negocio. Es lo que dispara todo el
           solicitudes de   solicitudes                 flujo operativo.
           transporte                                   

  RF-10    Asignación       Motor de         VN + RT    Core del negocio: externaliza la lógica de
           automática de    asignación                  asignación que hoy vive en la cabeza del
           conductores                                  administrador y la vuelve configurable por
                                                        tenant.

  RF-11    Aplicación de    Motor de         VN + RT    Motor de reglas configurable por operador,
           reglas de        asignación                  orientado a turnos y programación. La telemetría
           negocio para la                              no se usa para competencia dinámica por
           asignación                                   cercanía.

  RF-12    Reasignación     Motor de         VN         Es necesaria para contingencias reales del caso
           manual de        asignación                  ancla, como un conductor que abandona su turno y
           conductores                                  obliga a redistribuir servicios.

  RF-13    Priorización de  Motor de         VN + RT    Permite priorización operativa según reglas del
           solicitudes      asignación                  tenant y gestión de excepciones sin convertir el
           urgentes                                     motor en ride-hailing por proximidad.

  RF-14    Recepción de     Integración      VN + RT    Canal principal para pasajero y conductor. Reto
           mensajes desde   WhatsApp                    técnico: webhooks, contrato de integración,
           WhatsApp                                     idempotencia, seguridad y desacople del
                                                        proveedor mediante un adaptador específico.

  RF-15    Procesamiento de Integración      RT         Parseo conversacional + máquina de estados de
           solicitudes      WhatsApp                    conversación. No es CRUD; requiere investigar
           enviadas por                                 patrones de bots conversacionales.
           WhatsApp                                     

  RF-16    Envío de         Integración      VN         Cierra el flujo. Sin confirmación bidireccional,
           confirmaciones   WhatsApp                    el pasajero no sabe si su solicitud fue
           al usuario por                               procesada.
           WhatsApp                                     

  RF-17    Envío de         Integración      VN         Cierra el otro extremo del flujo. Sin esto, la
           notificaciones   WhatsApp                    asignación no llega al conductor.
           al conductor                                 
           asignado                                     

  RF-18    Actualización de Integración      RT         Reto técnico: mantener sincronización
           estado del viaje WhatsApp                    bidireccional entre el estado del sistema y el
           desde WhatsApp                               estado reportado por conductor vía chat.

  RF-19    Aislamiento      Multi-tenancy    VN + RT    Diferenciador confirmado: un tenant = un
           físico entre                                 operador de transporte con persistencia
           tenants (Silo)                               físicamente aislada. empresaId identifica al
                                                        cliente empresarial dentro de la BD del operador
                                                        y no funciona como filtro entre tenants.

  RF-20    Configuración    Multi-tenancy    RT         Configuración independiente por tenant dentro de
           separada de                                  su instancia Silo, sin compartir persistencia
           usuarios y                                   con otros operadores.
           recursos por                                 
           cliente                                      

  RF-21    Configuración    Configuración    VN + RT    Reglas parametrizables por tenant =
           avanzada de      avanzada                    diferenciador. Deben soportar turnos, balance de
           reglas de                                    carga y restricciones operativas/regulatorias;
           asignación                                   no reasignación dinámica por cercanía GPS.

  RF-54    Calificación     Calificaciones   VN         Capacidad incluida explícitamente en V1. Agrega
           pasajero →                                   trazabilidad de calidad operacional sobre Viaje
           conductor                                    y alimenta reportes básicos.

  RF-55    Ingesta de       Telemetría       VN + RT    Capacidad incluida en V1. Introduce
           telemetría GPS                               RegistroUbicacion, tratamiento de datos de
                                                        ubicación y una estrategia de
                                                        ingesta/persistencia adecuada al volumen
                                                        operativo.

  RF-56    Detección de     Telemetría       VN + RT    Usa telemetría para monitoreo de viajes en curso
           desviación de                                y alertas de desviación. No cambia el motor a
           ETA                                          asignación dinámica por proximidad.
  ------------------------------------------------------------------------------------------------------

*VN = Valor de Negocio · RT = Reto Técnico*

## Tabla B --- Requisitos funcionales NO significativos (dependencias)

  -----------------------------------------------------------------------------
  **Razón de        **Requisitos funcionales**
  descarte**        
  ----------------- -----------------------------------------------------------
  CRUD estándar --- Cierre de sesión; Recuperación de credenciales;
  patrón conocido,  Actualización de información de usuarios;
  no requiere       Activación/inactivación de usuarios; Registro de
  decisiones        conductores; Actualización de datos de conductores;
  arquitectónicas   Activación/inactivación de conductores; Validación de datos
  nuevas            mínimos de solicitud; Consulta del estado de solicitudes;
                    Historial de solicitudes y viajes; Modificación o
                    cancelación de solicitudes; Visualización de asignaciones
                    generadas; Actualización del estado de asignación;
                    Visualización centralizada de solicitudes y viajes;
                    Consulta de usuarios/conductores/asignaciones; Búsqueda y
                    filtrado de información; Visualización de indicadores
                    básicos

  Feature           Envío de recordatorios automáticos; Notificación automática
  secundaria ---    de cambios de estado; Notificación de reasignación o
  aporta valor pero novedades; Alertas operativas para administradores u
  no moldea la      operadores
  arquitectura      

  Reportes          Reporte de viajes realizados; Reporte de solicitudes
  operacionales --- atendidas y pendientes; Reporte de conductores asignados;
  necesarios para   Reporte histórico de operación por cliente empresarial
  el producto, pero 
  no introducen por 
  sí solos una      
  decisión          
  arquitectónica    
  adicional         

  Observabilidad    Registro de eventos del sistema; Registro de errores y
  transversal ---   excepciones; Seguimiento de acciones de usuarios
  cubierta por      administrativos; Monitoreo técnico de disponibilidad
  logging,          
  auditoría y       
  monitoreo técnico 

  Operación         Intervención manual estándar; consulta de catálogos;
  estándar del      búsqueda y filtrado. El monitoreo en tiempo real asociado a
  Panel Web ---     telemetría se cubre por RF-55 y RF-56.
  necesaria, pero   
  no toda es driver 
  arquitectónico    
  -----------------------------------------------------------------------------
