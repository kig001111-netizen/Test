SELECT
    BuildId,
    Position,
    RelicId
FROM BuildRelic
WHERE RelicId = $relicId
ORDER BY BuildId ASC, Position ASC;
