-- Effect: 効果マスタ（計算入力の定義。ロジックは持たない）
-- Id: DB 行 PK（RelicEffect FK 用）
-- EffectId + Level: 業務上の一意キー（段階効果は同一 EffectId）
-- Value: 倍率（例 1.04）。百分率では保持しない
-- DisplayOrder / Category: UI 用
CREATE TABLE IF NOT EXISTS Effect (
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
