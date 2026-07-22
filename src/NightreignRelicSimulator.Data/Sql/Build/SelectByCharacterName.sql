SELECT
    Id,
    Name,
    CharacterName,
    WeaponName,
    CreatedAt,
    UpdatedAt
FROM Build
WHERE CharacterName = $characterName
ORDER BY Id ASC;
