SELECT

    Id,

    Name,

    CharacterName,

    WeaponName,

    CreatedAt,

    UpdatedAt

FROM Build

WHERE Name LIKE '%' || $keyword || '%'

ORDER BY Id ASC;

