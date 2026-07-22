-- RelicEffect: 遺物スロット 1〜3
CREATE TABLE IF NOT EXISTS RelicEffect (
    RelicId    INTEGER NOT NULL,
    SlotNumber INTEGER NOT NULL,
    EffectId   INTEGER NOT NULL,
    PRIMARY KEY (RelicId, SlotNumber),
    CHECK (SlotNumber BETWEEN 1 AND 3),
    FOREIGN KEY (RelicId)  REFERENCES Relic (Id)  ON DELETE CASCADE,
    FOREIGN KEY (EffectId) REFERENCES Effect (Id) ON DELETE RESTRICT
);

CREATE INDEX IF NOT EXISTS IX_RelicEffect_EffectId ON RelicEffect (EffectId);
