using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlowRide.Inicializador.Migraciones
{
    /// <inheritdoc />
    public partial class TelemetriaParticionada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                ALTER TABLE ops.ubicaciones RENAME TO ubicaciones_anterior;
                ALTER INDEX ops.pk_ubicaciones RENAME TO pk_ubicaciones_anterior;
                ALTER INDEX ops.ix_ubicaciones_vehiculo_id_timestamp_origen RENAME TO ix_ubicaciones_anterior_vehiculo;
                ALTER INDEX ops.ix_ubicaciones_vehiculo_id_timestamp_origen_fuente RENAME TO ix_ubicaciones_anterior_fuente;

                CREATE TABLE ops.ubicaciones (
                    id uuid NOT NULL,
                    vehiculo_id uuid NOT NULL,
                    conductor_id uuid NULL,
                    viaje_id uuid NULL,
                    timestamp_origen timestamp with time zone NOT NULL,
                    timestamp_recepcion timestamp with time zone NOT NULL,
                    latitud double precision NOT NULL,
                    longitud double precision NOT NULL,
                    calidad character varying(40) NOT NULL,
                    fuente character varying(60) NOT NULL,
                    CONSTRAINT pk_ubicaciones PRIMARY KEY (id, timestamp_origen)
                ) PARTITION BY RANGE (timestamp_origen);

                CREATE INDEX ix_ubicaciones_vehiculo_id_timestamp_origen ON ops.ubicaciones (vehiculo_id, timestamp_origen DESC);
                CREATE UNIQUE INDEX ix_ubicaciones_vehiculo_id_timestamp_origen_fuente ON ops.ubicaciones (vehiculo_id, timestamp_origen, fuente);
                CREATE INDEX ix_ubicaciones_viaje_id_timestamp_origen ON ops.ubicaciones (viaje_id, timestamp_origen);

                CREATE TABLE ops.ubicaciones_default PARTITION OF ops.ubicaciones DEFAULT;

                DO $$
                DECLARE mes date;
                BEGIN
                    FOR mes IN SELECT DISTINCT date_trunc('month', timestamp_origen AT TIME ZONE 'UTC')::date FROM ops.ubicaciones_anterior LOOP
                        EXECUTE format(
                            'CREATE TABLE ops.%I PARTITION OF ops.ubicaciones FOR VALUES FROM (%L) TO (%L)',
                            'ubicaciones_' || to_char(mes, 'YYYY_MM'),
                            mes::text || ' 00:00:00+00',
                            (mes + interval '1 month')::date::text || ' 00:00:00+00');
                    END LOOP;
                END $$;

                INSERT INTO ops.ubicaciones (id, vehiculo_id, conductor_id, viaje_id, timestamp_origen, timestamp_recepcion, latitud, longitud, calidad, fuente)
                SELECT id, vehiculo_id, conductor_id, viaje_id, timestamp_origen, timestamp_recepcion, latitud, longitud, calidad, fuente
                FROM ops.ubicaciones_anterior;

                DROP TABLE ops.ubicaciones_anterior;
                """);

            migrationBuilder.Sql("""
                CREATE FUNCTION ops.mantener_particiones_ubicaciones(retencion_meses integer, meses_adelante integer DEFAULT 2)
                RETURNS integer
                LANGUAGE plpgsql
                AS $$
                DECLARE
                    inicio date := date_trunc('month', now() AT TIME ZONE 'UTC')::date;
                    mes date;
                    particion record;
                    eliminadas integer := 0;
                BEGIN
                    FOR i IN -1..meses_adelante LOOP
                        mes := (inicio + make_interval(months => i))::date;
                        EXECUTE format(
                            'CREATE TABLE IF NOT EXISTS ops.%I PARTITION OF ops.ubicaciones FOR VALUES FROM (%L) TO (%L)',
                            'ubicaciones_' || to_char(mes, 'YYYY_MM'),
                            mes::text || ' 00:00:00+00',
                            (mes + interval '1 month')::date::text || ' 00:00:00+00');
                    END LOOP;

                    FOR particion IN
                        SELECT c.relname
                        FROM pg_inherits h
                        JOIN pg_class c ON c.oid = h.inhrelid
                        JOIN pg_class padre ON padre.oid = h.inhparent
                        JOIN pg_namespace n ON n.oid = padre.relnamespace
                        WHERE n.nspname = 'ops' AND padre.relname = 'ubicaciones' AND c.relname ~ '^ubicaciones_[0-9]{4}_[0-9]{2}$'
                    LOOP
                        IF to_date(substr(particion.relname, 13), 'YYYY_MM') + interval '1 month'
                            <= (now() AT TIME ZONE 'UTC') - make_interval(months => retencion_meses) THEN
                            EXECUTE format('DROP TABLE ops.%I', particion.relname);
                            eliminadas := eliminadas + 1;
                        END IF;
                    END LOOP;

                    RETURN eliminadas;
                END $$;
                """);

            migrationBuilder.Sql("SELECT ops.mantener_particiones_ubicaciones(6);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP FUNCTION ops.mantener_particiones_ubicaciones(integer, integer);

                CREATE TABLE ops.ubicaciones_plana (
                    id uuid NOT NULL,
                    vehiculo_id uuid NOT NULL,
                    conductor_id uuid NULL,
                    viaje_id uuid NULL,
                    timestamp_origen timestamp with time zone NOT NULL,
                    timestamp_recepcion timestamp with time zone NOT NULL,
                    latitud double precision NOT NULL,
                    longitud double precision NOT NULL,
                    calidad character varying(40) NOT NULL,
                    fuente character varying(60) NOT NULL,
                    CONSTRAINT pk_ubicaciones_plana PRIMARY KEY (id)
                );

                INSERT INTO ops.ubicaciones_plana SELECT * FROM ops.ubicaciones;

                DROP TABLE ops.ubicaciones;

                ALTER TABLE ops.ubicaciones_plana RENAME TO ubicaciones;
                ALTER INDEX ops.pk_ubicaciones_plana RENAME TO pk_ubicaciones;
                CREATE INDEX ix_ubicaciones_vehiculo_id_timestamp_origen ON ops.ubicaciones (vehiculo_id, timestamp_origen DESC);
                CREATE UNIQUE INDEX ix_ubicaciones_vehiculo_id_timestamp_origen_fuente ON ops.ubicaciones (vehiculo_id, timestamp_origen, fuente);
                """);
        }
    }
}
