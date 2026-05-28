-- 模板元数据同步：已有库结构迁移（EnsureCreated 不会自动变更）
ALTER TABLE templates ADD COLUMN IF NOT EXISTS "Alias" character varying(500) NOT NULL DEFAULT '';
ALTER TABLE templates ADD COLUMN IF NOT EXISTS "SourceUpdatedAt" timestamp with time zone NULL;

ALTER TABLE templates DROP COLUMN IF EXISTS "ContentHtml";
ALTER TABLE templates DROP COLUMN IF EXISTS "ContentMarkdown";
ALTER TABLE templates DROP COLUMN IF EXISTS "ExternalId";

DROP INDEX IF EXISTS "IX_templates_ExternalId";

CREATE TABLE IF NOT EXISTS template_sync_state (
    "Id" integer NOT NULL PRIMARY KEY,
    "LastSyncedAt" timestamp with time zone,
    "LastRunAt" timestamp with time zone,
    "LastRunStatus" character varying(50),
    "LastRunProcessed" integer NOT NULL DEFAULT 0,
    "LastRunErrors" text[],
    "UpdatedAt" timestamp with time zone NOT NULL
);

INSERT INTO template_sync_state ("Id", "LastRunErrors", "UpdatedAt")
VALUES (1, '{}', NOW() AT TIME ZONE 'utc')
ON CONFLICT ("Id") DO NOTHING;

UPDATE template_sync_state SET "LastRunErrors" = '{}' WHERE "LastRunErrors" IS NULL;
