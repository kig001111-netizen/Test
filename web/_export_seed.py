# -*- coding: utf-8 -*-
import json
import re
from pathlib import Path

root = Path(r"c:\Users\keigo-aiura.EAC\Desktop\AI駆動開発\NightreignRelicSimulator")
text = (root / "src/NightreignRelicSimulator.Data/Seed/EffectSeed.cs").read_text(encoding="utf-8")
pat = re.compile(
    r'new\((\d+),\s*"([^"]+)",\s*"([^"]+)",\s*(true|false),\s*([0-9.]+)m,\s*(\d+),\s*"([^"]+)",\s*(\d+)\)'
)
rows = []
for m in pat.finditer(text):
    rows.append(
        {
            "effectId": int(m.group(1)),
            "name": m.group(2),
            "category": m.group(3),
            "canStack": m.group(4) == "true",
            "value": float(m.group(5)),
            "level": int(m.group(6)),
            "description": m.group(7),
            "displayOrder": int(m.group(8)),
        }
    )

out = root / "web" / "data"
out.mkdir(parents=True, exist_ok=True)
(out / "effect-seed.json").write_text(json.dumps(rows, ensure_ascii=False, indent=2), encoding="utf-8")
print("count", len(rows))
