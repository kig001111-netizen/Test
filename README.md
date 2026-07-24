# Nightreign Relic Simulator

ELDEN RING NIGHTREIGN 向けの **遺物管理・ビルド火力シミュレーター**（Windows デスクトップアプリ）です。

> 本リポジトリはファンメイドの非公式ツールです。ELDEN RING / NIGHTREIGN および関連する名称・設定は FromSoftware / Bandai Namco の商標または著作物です。本プロジェクトはそれら権利者と無関係です。

## できること

- 効果マスタ（Effect）の閲覧・追加・編集・削除・カテゴリ絞込
- 遺物の登録・編集・削除・検索（効果 3 スロット）
- ビルドの保存・読込・削除（遺物 6 スロット）
- 火力計算（最終火力 = 武器表示火力 × 全倍率）
- 重複不可効果の判定・計算ログ表示
- サイドナビ付きダークテーマ UI（画面間で武器火力を引き継ぎ）

## 技術スタック

- Visual Studio 2022 / .NET 8
- C# / Windows Forms
- SQLite + ADO.NET（`Microsoft.Data.Sqlite`）
- Repository / Service パターン

## リポジトリ構成

```
NightreignRelicSimulator/
├── NightreignRelicSimulator.sln
├── Directory.Build.props
├── LICENSE
├── README.md
├── web/                                      # 静的 Web（GitHub Pages 向け）
│   ├── index.html
│   ├── css/ / js/ / data/
│   └── .nojekyll
├── docs/
│   ├── ドメインルール.md
│   └── sqlite-設計.md
└── src/
    ├── NightreignRelicSimulator.Core/       # Model / Interface / 定数
    ├── NightreignRelicSimulator.Data/       # SQLite / Repository / Seed
    ├── NightreignRelicSimulator.Services/   # 業務・DamageCalculator
    ├── NightreignRelicSimulator.App/        # WinForms UI
    └── NightreignRelicSimulator.Web/        # HTML/JS UI + API（同一 LAN 向け）
```

### 依存関係

```
App (WinForms)  → Services → Data → Core
Web (ASP.NET)   → Services → Data → Core
web/ (静的)     → ブラウザ内 sql.js + IndexedDB（サーバー不要）
```

| プロジェクト | 役割 |
|---|---|
| **Core** | ドメインモデル、インターフェース、定数 |
| **Data** | SQLite / Repository / Effect Seed |
| **Services** | 業務処理・DamageCalculator |
| **App** | Windows Forms UI |
| **Web** | HTML/JS + REST API（PC 上の SQLite 共用・同一 LAN） |
| **web/** | 静的 HTML/JS/CSS（どの Wi‑Fi / 端末からでも開ける） |

## 必要環境

- Windows 10/11（WinForms 実行時）
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- （任意）Visual Studio 2022

## ビルド / 実行

### デスクトップ（WinForms）

```powershell
dotnet build NightreignRelicSimulator.sln
dotnet run --project src/NightreignRelicSimulator.App
```

### Web（どの Wi‑Fi からでも開ける・推奨）

`web/` は **HTML / JavaScript / CSS のみ**です。ブラウザ内で SQLite（[sql.js](https://sql.js.org/)）を動かし、データは端末の IndexedDB に保存します。  
PC をサーバーにしなくても、GitHub Pages 等に置けば **別の Wi‑Fi・スマホからも同じ機能**を使えます。

#### ローカルで試す

```powershell
cd web
npx --yes serve .
# または: python -m http.server 8080
```

ブラウザで表示された URL（例: `http://localhost:3000`）を開きます。

#### GitHub Pages で公開

1. このリポジトリを GitHub に push
2. **Settings → Pages → Build and deployment**
3. Source: **Deploy from a branch**
4. Branch: `main`、Folder: **`/web`**
5. 数分後、`https://<user>.github.io/<repo>/` で開けます

> **注意:** 遺物・ビルドは **ブラウザごと** に保存されます（PC の WinForms / ASP.NET Web の DB とは共有しません）。  
> 別端末では別データになります。Effect マスタは初回起動時に seed から入ります。

### Web（同一 LAN・PC の SQLite 共用）

```powershell
dotnet run --project src/NightreignRelicSimulator.Web
```

ブラウザで `http://localhost:5152`（LAN なら `http://<PCのIP>:5152`）を開きます。  
データは `%LocalAppData%\NightreignRelicSimulator\nightreign.db` を API 経由で共有します（PC 起動中・同じネットワーク向け）。

初回起動時に DB を自動生成し、Effect マスタを投入します。

## 設計メモ

- Excel / 効果マスタの数値を計算の正解とする
- 計算結果は DB に保存しない（毎回 `DamageCalculator` で再計算）
- `Effect` はデータのみ。計算ロジックは `DamageCalculator` に集約
- 詳細は [ドメインルール](docs/ドメインルール.md) / [SQLite 設計](docs/sqlite-設計.md)

## GitHub への公開手順

本リポジトリを GitHub に載せる例です（初回のみ）。

```powershell
# 1. GitHub で空リポジトリを作成（例: NightreignRelicSimulator）

# 2. リモート追加と push
git remote add origin https://github.com/<your-account>/NightreignRelicSimulator.git
git branch -M main
git push -u origin main
```

`gh` がある場合:

```powershell
gh repo create NightreignRelicSimulator --private --source=. --remote=origin --push
```

## ライセンス

[MIT](LICENSE)
