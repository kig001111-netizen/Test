-- BuildRelic: 装備位置 1〜6
-- 同一 Build 内で同じ RelicId を複数 Position に置ける
CREATE TABLE IF NOT EXISTS BuildRelic (
    BuildId  INTEGER NOT NULL,
    Position INTEGER NOT NULL,
    RelicId  INTEGER NOT NULL,
    PRIMARY KEY (BuildId, Position),
    CHECK (Position BETWEEN 1 AND 6),
    FOREIGN KEY (BuildId) REFERENCES Build (Id) ON DELETE CASCADE,
    FOREIGN KEY (RelicId) REFERENCES Relic (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_BuildRelic_RelicId ON BuildRelic (RelicId);
