SELECT
    BuildId,
    Position,
    RelicId
FROM BuildRelic
WHERE BuildId = $buildId
ORDER BY Position ASC;
