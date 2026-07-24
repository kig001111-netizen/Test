/**
 * sql.js + IndexedDB 永続化。
 * PC サーバー不要で、どの Wi-Fi / 端末のブラウザでも動作します。
 */
const Db = (() => {
  const DB_KEY = "nightreign-relic-simulator-db-v1";
  let SQL = null;
  let db = null;

  function nowIso() {
    return new Date().toISOString();
  }

  function rowsFrom(result) {
    if (!result.length) return [];
    const { columns, values } = result[0];
    return values.map((row) => {
      const obj = {};
      columns.forEach((col, i) => {
        obj[col] = row[i];
      });
      return obj;
    });
  }

  function mapEffect(row) {
    return {
      id: row.Id,
      effectId: row.EffectId,
      name: row.Name,
      category: row.Category,
      canStack: !!row.CanStack,
      value: row.Value,
      level: row.Level,
      description: row.Description,
      displayOrder: row.DisplayOrder
    };
  }

  function mapRelic(row) {
    return {
      id: row.Id,
      name: row.Name,
      color: row.Color,
      memo: row.Memo,
      createdAt: row.CreatedAt,
      updatedAt: row.UpdatedAt
    };
  }

  function mapBuild(row) {
    return {
      id: row.Id,
      name: row.Name,
      characterName: row.CharacterName,
      weaponName: row.WeaponName,
      createdAt: row.CreatedAt,
      updatedAt: row.UpdatedAt
    };
  }

  async function openIdb() {
    return new Promise((resolve, reject) => {
      const req = indexedDB.open("NightreignRelicSimulator", 1);
      req.onupgradeneeded = () => {
        const idb = req.result;
        if (!idb.objectStoreNames.contains("kv")) {
          idb.createObjectStore("kv");
        }
      };
      req.onsuccess = () => resolve(req.result);
      req.onerror = () => reject(req.error);
    });
  }

  async function idbGet(key) {
    const idb = await openIdb();
    return new Promise((resolve, reject) => {
      const tx = idb.transaction("kv", "readonly");
      const req = tx.objectStore("kv").get(key);
      req.onsuccess = () => resolve(req.result || null);
      req.onerror = () => reject(req.error);
    });
  }

  async function idbSet(key, value) {
    const idb = await openIdb();
    return new Promise((resolve, reject) => {
      const tx = idb.transaction("kv", "readwrite");
      tx.objectStore("kv").put(value, key);
      tx.oncomplete = () => resolve();
      tx.onerror = () => reject(tx.error);
    });
  }

  async function persist() {
    const data = db.export();
    await idbSet(DB_KEY, data);
  }

  async function init() {
    const base = "https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.10.3/";
    SQL = await initSqlJs({ locateFile: (file) => `${base}${file}` });

    const saved = await idbGet(DB_KEY);
    if (saved) {
      db = new SQL.Database(new Uint8Array(saved));
      return;
    }

    db = new SQL.Database();
    const schema = await (await fetch("./data/schema.sql")).text();
    db.run(schema);

    const seed = await (await fetch("./data/effect-seed.json")).json();
    const insert = db.prepare(
      `INSERT INTO Effect (EffectId, Name, Category, CanStack, Value, Level, Description, DisplayOrder)
       VALUES (?, ?, ?, ?, ?, ?, ?, ?)`
    );
    seed.forEach((e) => {
      insert.run([
        e.effectId,
        e.name,
        e.category,
        e.canStack ? 1 : 0,
        e.value,
        e.level,
        e.description,
        e.displayOrder
      ]);
    });
    insert.free();
    await persist();
  }

  function exec(sql, params = []) {
    if (!params.length) {
      return rowsFrom(db.exec(sql));
    }
    const stmt = db.prepare(sql);
    stmt.bind(params);
    const rows = [];
    while (stmt.step()) {
      rows.push(stmt.getAsObject());
    }
    stmt.free();
    return rows;
  }

  function run(sql, params = []) {
    db.run(sql, params);
  }

  return {
    init,
    persist,
    mapEffect,
    mapRelic,
    mapBuild,
    exec,
    run,
    nowIso,
    getDb: () => db
  };
})();
