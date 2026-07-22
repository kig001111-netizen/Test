-- DatabaseInfo: スキーマバージョン管理（単一行を想定）
CREATE TABLE IF NOT EXISTS DatabaseInfo (
    Version       INTEGER NOT NULL,
    InitializedAt TEXT    NOT NULL
);
