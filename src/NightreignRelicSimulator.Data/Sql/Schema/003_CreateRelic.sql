-- Relic: 登録遺物（同一内容の複数行登録を許容）
CREATE TABLE IF NOT EXISTS Relic (
    Id        INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
    Name      TEXT    NOT NULL,
    Color     INTEGER NOT NULL DEFAULT 0,
    Memo      TEXT    NOT NULL DEFAULT '',
    CreatedAt TEXT    NOT NULL,
    UpdatedAt TEXT    NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_Relic_Name  ON Relic (Name);
CREATE INDEX IF NOT EXISTS IX_Relic_Color ON Relic (Color);
