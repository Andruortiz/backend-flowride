using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowRide.Inicializador.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ops");

            migrationBuilder.EnsureSchema(
                name: "audit");

            migrationBuilder.CreateTable(
                name: "alertas",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    severidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    titulo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    detalle = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    referencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    clave_deduplicacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    leida_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_alertas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "asignaciones",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitud_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conductor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    version_configuracion = table.Column<int>(type: "integer", nullable: false),
                    explicacion = table.Column<string>(type: "jsonb", nullable: true),
                    es_manual = table.Column<bool>(type: "boolean", nullable: false),
                    motivo = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    respondida_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asignaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calificaciones",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pasajero_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conductor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    puntaje = table.Column<int>(type: "integer", nullable: false),
                    comentario = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calificaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "clientes_empresariales",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_clientes_empresariales", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conductores",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    licencia = table.Column<string>(type: "jsonb", nullable: false),
                    vehiculo_id = table.Column<Guid>(type: "uuid", nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    disponible = table.Column<bool>(type: "boolean", nullable: false),
                    motivo_no_disponible = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    turnos = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conductores", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "configuraciones_asignacion",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    reglas = table.Column<string>(type: "jsonb", nullable: false),
                    creada_por = table.Column<Guid>(type: "uuid", nullable: true),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_configuraciones_asignacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "conversaciones",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    origen = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    destino = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    fecha_hora_utc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    urgente = table.Column<bool>(type: "boolean", nullable: true),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_conversaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "itinerarios",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    codigo_servicio = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    cliente_empresarial_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origen = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    destino = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    hora_salida = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    hora_llegada = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_itinerarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "mensajes_entrantes",
                schema: "ops",
                columns: table => new
                {
                    external_message_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recibido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_mensajes_entrantes", x => x.external_message_id);
                });

            migrationBuilder.CreateTable(
                name: "notificaciones_enviadas",
                schema: "ops",
                columns: table => new
                {
                    evento_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destinatario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    canal = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    enviada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_notificaciones_enviadas", x => new { x.evento_id, x.destinatario_id, x.canal });
                });

            migrationBuilder.CreateTable(
                name: "operador",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    razon_social = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    nit = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    correo_contacto = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    zona_horaria = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_operador", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "outbox",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tipo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    carga = table.Column<string>(type: "jsonb", nullable: false),
                    ocurrio_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    disponible_desde = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    procesado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    ultimo_error = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_outbox", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "parametros",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clave = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    valor = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    modificado_por = table.Column<Guid>(type: "uuid", nullable: true),
                    modificado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_parametros", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "pasajeros",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cliente_empresarial_id = table.Column<Guid>(type: "uuid", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_pasajeros", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "registros",
                schema: "audit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    actor_nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    accion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recurso = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    resultado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    motivo = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    antes = table.Column<string>(type: "text", nullable: true),
                    despues = table.Column<string>(type: "text", nullable: true),
                    instante = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_registros", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "solicitudes",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    pasajero_id = table.Column<Guid>(type: "uuid", nullable: false),
                    itinerario_id = table.Column<Guid>(type: "uuid", nullable: true),
                    origen = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    destino = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    destino_latitud = table.Column<double>(type: "double precision", nullable: true),
                    destino_longitud = table.Column<double>(type: "double precision", nullable: true),
                    fecha_hora_programada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    urgente = table.Column<bool>(type: "boolean", nullable: false),
                    es_personal = table.Column<bool>(type: "boolean", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    asignacion_id = table.Column<Guid>(type: "uuid", nullable: true),
                    intentos = table.Column<int>(type: "integer", nullable: false),
                    recordatorio_enviado = table.Column<bool>(type: "boolean", nullable: false),
                    motivo_cancelacion = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    cancelada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_solicitudes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ubicaciones",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conductor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    viaje_id = table.Column<Guid>(type: "uuid", nullable: true),
                    timestamp_origen = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    timestamp_recepcion = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    latitud = table.Column<double>(type: "double precision", nullable: false),
                    longitud = table.Column<double>(type: "double precision", nullable: false),
                    calidad = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    fuente = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_ubicaciones", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    correo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    telefono = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    codigo_interno = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    rol = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    identidad_externa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    desactivado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vehiculos",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    placa = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    capacidad = table.Column<int>(type: "integer", nullable: false),
                    habilitado = table.Column<bool>(type: "boolean", nullable: false),
                    documentos = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vehiculos", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "viajes",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    solicitud_id = table.Column<Guid>(type: "uuid", nullable: false),
                    asignacion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pasajero_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conductor_id = table.Column<Guid>(type: "uuid", nullable: false),
                    vehiculo_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    programado_para = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    eta_programada = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    destino_latitud = table.Column<double>(type: "double precision", nullable: true),
                    destino_longitud = table.Column<double>(type: "double precision", nullable: true),
                    iniciado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    finalizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    alerta_eta_activa = table.Column<bool>(type: "boolean", nullable: false),
                    hitos = table.Column<string>(type: "jsonb", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_viajes", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_alertas_clave_deduplicacion",
                schema: "ops",
                table: "alertas",
                column: "clave_deduplicacion",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_alertas_leida_en",
                schema: "ops",
                table: "alertas",
                column: "leida_en");

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_conductor_id_estado",
                schema: "ops",
                table: "asignaciones",
                columns: new[] { "conductor_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_solicitud_id",
                schema: "ops",
                table: "asignaciones",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "ix_asignaciones_solicitud_id1",
                schema: "ops",
                table: "asignaciones",
                column: "solicitud_id",
                unique: true,
                filter: "estado IN ('PendienteAceptacion','Aceptada','EnCurso')");

            migrationBuilder.CreateIndex(
                name: "ix_calificaciones_viaje_id",
                schema: "ops",
                table: "calificaciones",
                column: "viaje_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conductores_usuario_id",
                schema: "ops",
                table: "conductores",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_configuraciones_asignacion_version",
                schema: "ops",
                table: "configuraciones_asignacion",
                column: "version",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_conversaciones_usuario_id",
                schema: "ops",
                table: "conversaciones",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_itinerarios_cliente_empresarial_id_fecha",
                schema: "ops",
                table: "itinerarios",
                columns: new[] { "cliente_empresarial_id", "fecha" });

            migrationBuilder.CreateIndex(
                name: "ix_outbox_disponible_desde",
                schema: "ops",
                table: "outbox",
                column: "disponible_desde",
                filter: "procesado_en IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_parametros_clave",
                schema: "ops",
                table: "parametros",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pasajeros_cliente_empresarial_id",
                schema: "ops",
                table: "pasajeros",
                column: "cliente_empresarial_id");

            migrationBuilder.CreateIndex(
                name: "ix_pasajeros_usuario_id",
                schema: "ops",
                table: "pasajeros",
                column: "usuario_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_registros_accion",
                schema: "audit",
                table: "registros",
                column: "accion");

            migrationBuilder.CreateIndex(
                name: "ix_registros_instante",
                schema: "audit",
                table: "registros",
                column: "instante");

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_estado_fecha_hora_programada",
                schema: "ops",
                table: "solicitudes",
                columns: new[] { "estado", "fecha_hora_programada" });

            migrationBuilder.CreateIndex(
                name: "ix_solicitudes_pasajero_id",
                schema: "ops",
                table: "solicitudes",
                column: "pasajero_id");

            migrationBuilder.CreateIndex(
                name: "ix_ubicaciones_vehiculo_id_timestamp_origen",
                schema: "ops",
                table: "ubicaciones",
                columns: new[] { "vehiculo_id", "timestamp_origen" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_ubicaciones_vehiculo_id_timestamp_origen_fuente",
                schema: "ops",
                table: "ubicaciones",
                columns: new[] { "vehiculo_id", "timestamp_origen", "fuente" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_codigo_interno",
                schema: "ops",
                table: "usuarios",
                column: "codigo_interno",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                schema: "ops",
                table: "usuarios",
                column: "correo",
                unique: true,
                filter: "correo IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_identidad_externa",
                schema: "ops",
                table: "usuarios",
                column: "identidad_externa",
                unique: true,
                filter: "identidad_externa IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_telefono",
                schema: "ops",
                table: "usuarios",
                column: "telefono",
                unique: true,
                filter: "telefono IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_vehiculos_placa",
                schema: "ops",
                table: "vehiculos",
                column: "placa",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_viajes_asignacion_id",
                schema: "ops",
                table: "viajes",
                column: "asignacion_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_viajes_conductor_id_estado",
                schema: "ops",
                table: "viajes",
                columns: new[] { "conductor_id", "estado" });

            migrationBuilder.CreateIndex(
                name: "ix_viajes_solicitud_id",
                schema: "ops",
                table: "viajes",
                column: "solicitud_id");

            migrationBuilder.CreateIndex(
                name: "ix_viajes_vehiculo_id",
                schema: "ops",
                table: "viajes",
                column: "vehiculo_id",
                unique: true,
                filter: "estado = 'EnCurso'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "alertas",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "asignaciones",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "calificaciones",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "clientes_empresariales",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "conductores",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "configuraciones_asignacion",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "conversaciones",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "itinerarios",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "mensajes_entrantes",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "notificaciones_enviadas",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "operador",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "outbox",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "parametros",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "pasajeros",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "registros",
                schema: "audit");

            migrationBuilder.DropTable(
                name: "solicitudes",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "ubicaciones",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "vehiculos",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "viajes",
                schema: "ops");
        }
    }
}
