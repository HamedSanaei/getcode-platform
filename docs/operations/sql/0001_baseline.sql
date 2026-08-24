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

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824085615_AddTraceContextToOutbox') THEN
    ALTER TABLE outbox_messages ADD span_id character varying(16);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824085615_AddTraceContextToOutbox') THEN
    ALTER TABLE outbox_messages ADD trace_id character varying(32);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824085615_AddTraceContextToOutbox') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824085615_AddTraceContextToOutbox', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    CREATE TABLE identity_audit_events (
        id uuid NOT NULL,
        occurred_at_utc timestamp with time zone NOT NULL,
        user_id uuid,
        event_type character varying(200) NOT NULL,
        succeeded boolean NOT NULL,
        correlation_id character varying(128),
        details_json jsonb,
        CONSTRAINT pk_identity_audit_events PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    CREATE TABLE users (
        id uuid NOT NULL,
        normalized_email character varying(320) NOT NULL,
        password_hash character varying(512) NOT NULL,
        status integer NOT NULL,
        registered_at_utc timestamp with time zone NOT NULL,
        password_changed_at_utc timestamp with time zone NOT NULL,
        failed_login_count integer NOT NULL,
        first_failed_login_at_utc timestamp with time zone,
        locked_until_utc timestamp with time zone,
        lock_reason character varying(256),
        disabled_at_utc timestamp with time zone,
        CONSTRAINT pk_users PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    CREATE INDEX ix_identity_audit_events__event_type ON identity_audit_events (event_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    CREATE INDEX ix_identity_audit_events__user_id_occurred_at_utc ON identity_audit_events (user_id, occurred_at_utc);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    CREATE UNIQUE INDEX ix_users__normalized_email ON users (normalized_email);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824091620_AddIdentity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824091620_AddIdentity', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE TABLE countries (
        id uuid NOT NULL,
        code character(2) NOT NULL,
        default_display_name character varying(200) NOT NULL,
        is_enabled boolean NOT NULL,
        display_order integer NOT NULL,
        CONSTRAINT pk_countries PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE TABLE services (
        id uuid NOT NULL,
        slug character varying(64) NOT NULL,
        default_display_name character varying(200) NOT NULL,
        is_enabled boolean NOT NULL,
        display_order integer NOT NULL,
        CONSTRAINT pk_services PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE TABLE country_localized_names (
        culture_code character varying(10) NOT NULL,
        country_id uuid NOT NULL,
        display_name character varying(200) NOT NULL,
        CONSTRAINT pk_country_localized_names PRIMARY KEY (country_id, culture_code),
        CONSTRAINT fk_country_localized_names_countries FOREIGN KEY (country_id) REFERENCES countries (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE TABLE service_localized_names (
        culture_code character varying(10) NOT NULL,
        service_id uuid NOT NULL,
        display_name character varying(200) NOT NULL,
        CONSTRAINT pk_service_localized_names PRIMARY KEY (service_id, culture_code),
        CONSTRAINT fk_service_localized_names_services FOREIGN KEY (service_id) REFERENCES services (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE UNIQUE INDEX ix_countries__code ON countries (code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    CREATE UNIQUE INDEX ix_services__slug ON services (slug);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824093919_AddCatalog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824093919_AddCatalog', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824095644_AddProductSkus') THEN
    CREATE TABLE product_skus (
        id uuid NOT NULL,
        country_id uuid NOT NULL,
        service_id uuid NOT NULL,
        product_type integer NOT NULL,
        is_offered boolean NOT NULL,
        CONSTRAINT pk_product_skus PRIMARY KEY (id),
        CONSTRAINT fk_product_skus_countries FOREIGN KEY (country_id) REFERENCES countries (id) ON DELETE CASCADE,
        CONSTRAINT fk_product_skus_services FOREIGN KEY (service_id) REFERENCES services (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824095644_AddProductSkus') THEN
    CREATE UNIQUE INDEX ix_product_skus__country_id_service_id_product_type ON product_skus (country_id, service_id, product_type);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824095644_AddProductSkus') THEN
    CREATE INDEX ix_product_skus__service_id ON product_skus (service_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824095644_AddProductSkus') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824095644_AddProductSkus', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824100525_AddProviderRegistry') THEN
    CREATE TABLE providers (
        id uuid NOT NULL,
        provider_key character varying(64) NOT NULL,
        display_name character varying(200) NOT NULL,
        is_enabled boolean NOT NULL,
        supports_activation boolean NOT NULL,
        supports_rental boolean NOT NULL,
        CONSTRAINT pk_providers PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824100525_AddProviderRegistry') THEN
    CREATE TABLE provider_mappings (
        id uuid NOT NULL,
        provider_id uuid NOT NULL,
        kind integer NOT NULL,
        external_code character varying(128) NOT NULL,
        canonical_id uuid NOT NULL,
        CONSTRAINT pk_provider_mappings PRIMARY KEY (id),
        CONSTRAINT fk_provider_mappings_providers FOREIGN KEY (provider_id) REFERENCES providers (id) ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824100525_AddProviderRegistry') THEN
    CREATE UNIQUE INDEX ix_provider_mappings__provider_id_kind_external_code ON provider_mappings (provider_id, kind, external_code);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824100525_AddProviderRegistry') THEN
    CREATE UNIQUE INDEX ix_providers__provider_key ON providers (provider_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824100525_AddProviderRegistry') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824100525_AddProviderRegistry', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824103523_AddAuthorization') THEN
    CREATE TABLE roles (
        id uuid NOT NULL,
        key character varying(64) NOT NULL,
        display_name character varying(200) NOT NULL,
        is_system_role boolean NOT NULL,
        permissions jsonb NOT NULL,
        CONSTRAINT pk_roles PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824103523_AddAuthorization') THEN
    CREATE TABLE user_roles (
        user_id uuid NOT NULL,
        role_id uuid NOT NULL,
        assigned_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_user_roles PRIMARY KEY (user_id, role_id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824103523_AddAuthorization') THEN
    CREATE UNIQUE INDEX ix_roles__key ON roles (key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824103523_AddAuthorization') THEN
    CREATE INDEX ix_user_roles__user_id ON user_roles (user_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824103523_AddAuthorization') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824103523_AddAuthorization', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    CREATE TABLE wallets (
        id uuid NOT NULL,
        owner_user_id uuid NOT NULL,
        currency character varying(3) NOT NULL,
        opened_at_utc timestamp with time zone NOT NULL,
        balance_minor bigint NOT NULL,
        is_closed boolean NOT NULL,
        CONSTRAINT pk_wallets PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    CREATE TABLE wallet_entries (
        id uuid NOT NULL,
        wallet_id uuid NOT NULL,
        entry_type integer NOT NULL,
        amount_minor bigint NOT NULL,
        currency character varying(3) NOT NULL,
        reference_type character varying(64),
        reference_id uuid,
        idempotency_key text NOT NULL,
        resulting_balance_minor bigint NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        CONSTRAINT pk_wallet_entries PRIMARY KEY (id),
        CONSTRAINT fk_wallet_entries_wallets FOREIGN KEY (wallet_id) REFERENCES wallets (id) ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    CREATE UNIQUE INDEX ix_wallet_entries__idempotency_key ON wallet_entries (idempotency_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    CREATE INDEX ix_wallet_entries__wallet_id ON wallet_entries USING btree (wallet_id);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    CREATE UNIQUE INDEX ix_wallets__owner_user_id_currency ON wallets (owner_user_id, currency);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824105033_AddWallets') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824105033_AddWallets', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824111759_AddLedgerRequestHash') THEN
    ALTER TABLE wallet_entries ALTER COLUMN idempotency_key TYPE character varying(128);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824111759_AddLedgerRequestHash') THEN
    ALTER TABLE wallet_entries ADD request_hash character varying(64) NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824111759_AddLedgerRequestHash') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824111759_AddLedgerRequestHash', '10.0.11');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824120937_AddSessions') THEN
    CREATE TABLE sessions (
        id uuid NOT NULL,
        user_id uuid NOT NULL,
        site_key character varying(64) NOT NULL,
        token_hash character varying(64) NOT NULL,
        created_at_utc timestamp with time zone NOT NULL,
        expires_at_utc timestamp with time zone NOT NULL,
        revoked_at_utc timestamp with time zone,
        revocation_reason character varying(128),
        rotated_from_session_id uuid,
        CONSTRAINT pk_sessions PRIMARY KEY (id)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824120937_AddSessions') THEN
    CREATE UNIQUE INDEX ix_sessions__token_hash ON sessions (token_hash);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824120937_AddSessions') THEN
    CREATE INDEX ix_sessions__user_id_site_key ON sessions (user_id, site_key);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260824120937_AddSessions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260824120937_AddSessions', '10.0.11');
    END IF;
END $EF$;
COMMIT;

