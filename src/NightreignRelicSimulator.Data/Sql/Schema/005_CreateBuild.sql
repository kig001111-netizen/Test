-- Build: ビルドヘッダ
-- CharacterName / WeaponName は現状文字列。将来マスタ FK 化を想定。
CREATE TABLE IF NOT EXISTS Build (
    Id            INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name          TEXT    NOT NULL,
    CharacterName TEXT    NOT NULL DEFAULT '',
    WeaponName    TEXT    NOT NULL DEFAULT '',
    CreatedAt     TEXT    NOT NULL,
    UpdatedAt     TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Build_Name          ON Build (Name);
CREATE INDEX IF NOT EXISTS IX_Build_CharacterName ON Build (CharacterName);
CREATE INDEX IF NOT EXISTS IX_Build_WeaponName    ON Build (WeaponName);
