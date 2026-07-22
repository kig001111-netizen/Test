# Nightreign Relic Simulator

ELDEN RING NIGHTREIGN 向けの **遺物管理・ビルド火力シミュレーター**（Windows デスクトップアプリ）です。

> 本リポジトリはファンメイドの非公式ツールです。ELDEN RING / NIGHTREIGN および関連する名称・設定は FromSoftware / Bandai Namco の商標または著作物です。本プロジェクトはそれら権利者と無関係です。

## できること

- 効果マスタ（Effect）の閲覧・追加・編集・削除・カテゴリ絞込
- 遺物の登録・編集・削除・検索（効果 3 スロット）
- ビルドの保存・読込・削除（遺物 6 スロット）
- 火力計算（最終火力 = 武器表示火力 × 全倍率）
- 重複不可効果の判定・計算ログ表示

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
├── docs/
│   ├── ドメインルール.md
│   └── sqlite-設計.md
└── src/
    ├── NightreignRelicSimulator.Core/       # Model / Interface / 定数
    ├── NightreignRelicSimulator.Data/       # SQLite / Repository / Seed
    ├── NightreignRelicSimulator.Services/   # 業務・DamageCalculator
    └── NightreignRelicSimulator.App/        # WinForms UI
```

### 依存関係

```
App → Services → Data → Core
 App ──────────→ Data（起動時 DB 初期化）
```

## 必要環境

- Windows 10/11
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- （任意）Visual Studio 2022（Windows Forms 開発ワークロード）

## ビルド / 実行

```powershell
dotnet build NightreignRelicSimulator.sln
dotnet run --project src/NightreignRelicSimulator.App
```

初回起動時に `%LocalAppData%\NightreignRelicSimulator\nightreign.db` を自動生成し、Effect マスタを投入します。

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
