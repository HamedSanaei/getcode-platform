CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824084031_InitialCreate') THEN
    CREATE TABLE outbox_messages (
        id uuid NOT NULL,
        occurred_at_utc timestamp with time zone NOT NULL,
        type character varying(500) NOT NULL,
        payload_json jsonb NOT NULL,
        correlation_id character varying(128),
        processed_at_utc timestamp with time zone,
        attempt_count integer NOT NULL,
        last_error_code character varying(200),
        CONSTRAINT pk_outbox_messages PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824084031_InitialCreate') THEN
    CREATE INDEX ix_outbox_messages__processed_at_utc_occurred_at_utc ON outbox_messages (processed_at_utc, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824084031_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824084031_InitialCreate', '10.0.11');
    END IF;
END $EF$;
COMMIT;

