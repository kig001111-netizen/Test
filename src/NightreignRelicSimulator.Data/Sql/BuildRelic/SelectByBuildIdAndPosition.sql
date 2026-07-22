SELECT
    BuildId,
    Position,
    RelicId
FROM BuildRelic
WHERE BuildId = $buildId
  AND Position = $position;
