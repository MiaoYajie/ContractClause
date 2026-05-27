-- 已有数据库需手动执行（EnsureCreated 不会自动加列）
ALTER TABLE templates ADD COLUMN IF NOT EXISTS "ContentHtml" text NOT NULL DEFAULT '';
ALTER TABLE templates ADD COLUMN IF NOT EXISTS "ExternalId" character varying(128);
ALTER TABLE templates ADD COLUMN IF NOT EXISTS "SourceUpdatedAt" timestamp with time zone;

CREATE UNIQUE INDEX IF NOT EXISTS "IX_templates_ExternalId" ON templates ("ExternalId");

CREATE TABLE IF NOT EXISTS template_sync_state (
    "Id" integer NOT NULL PRIMARY KEY,
    "LastSyncedAt" timestamp with time zone,
    "LastRunAt" timestamp with time zone,
    "LastRunStatus" character varying(50),
    "LastRunProcessed" integer NOT NULL DEFAULT 0,
    "LastRunErrors" text[],
    "UpdatedAt" timestamp with time zone NOT NULL
);

INSERT INTO template_sync_state ("Id", "UpdatedAt")
VALUES (1, NOW() AT TIME ZONE 'utc')
ON CONFLICT ("Id") DO NOTHING;
