-- v1 → v2: Effect を新仕様へ再構築（既存 Effect / RelicEffect をクリアして Seed し直す）
DELETE FROM RelicEffect;
DELETE FROM Effect;
DROP TABLE IF EXISTS Effect;

CREATE TABLE Effect (
    Id           INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    EffectId     INTEGER NOT NULL,
    Name         TEXT    NOT NULL,
    Category     TEXT    NOT NULL DEFAULT '',
    CanStack     INTEGER NOT NULL DEFAULT 1
                 CHECK (CanStack IN (0, 1)),
    Value        REAL    NOT NULL,
    Level        INTEGER NOT NULL DEFAULT 1
                 CHECK (Level >= 1),
    Description  TEXT    NOT NULL DEFAULT '',
    DisplayOrder INTEGER NOT NULL DEFAULT 0,
    UNIQUE (EffectId, Level)
);

CREATE INDEX IF NOT EXISTS IX_Effect_DisplayOrder ON Effect (DisplayOrder);
CREATE INDEX IF NOT EXISTS IX_Effect_Category     ON Effect (Category);
CREATE INDEX IF NOT EXISTS IX_Effect_EffectId     ON Effect (EffectId);
