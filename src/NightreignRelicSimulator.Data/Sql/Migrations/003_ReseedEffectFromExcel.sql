-- v2 → v3: Excel（個人開発マスタ.xlsx）に基づく Effect Seed 差し替え
-- Relic の効果参照は Effect.Id（行PK）のため、マスタ更新に伴い RelicEffect もクリアする
DELETE FROM RelicEffect;
DELETE FROM Effect;
