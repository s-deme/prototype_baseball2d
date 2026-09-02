# Steam release checklist

## リポジトリ内で準備済み

- [x] Unity 6.3 LTSにバージョン固定
- [x] Windows x64ビルドメニューとCLIビルドスクリプト
- [x] オフラインで完結し、個人情報・通信・課金なし
- [x] ロゴ入りストアカプセル原画、背景原画、透過ロゴ
- [x] Steam必須寸法のストア・ライブラリ画像とアプリアイコン
- [x] ストア文面案、QA項目、権利表示、第三者表記
- [x] コアルールの自動スモークテストとUnity APIコンパイル確認

## アカウント所有者が行う項目

- [ ] Unity HubでUnity Editorライセンスを有効化する
- [ ] `.\Scripts\build-windows.ps1` でRelease Candidateを生成する
- [ ] `.\Scripts\capture-screenshots.ps1` でQA画面3枚とゲームプレイ画面5枚を生成・確認する
- [ ] [QA checklist](QA_CHECKLIST.md)を実機で完了する
- [ ] Steamworksパートナー登録、税務・銀行情報、アプリクレジットを完了してApp IDを取得する
- [ ] サポート連絡先、開発者名、価格、発売日、対応言語を確定する
- [ ] 内容調査で、マーケティング画像に開発中の事前生成AIを使用したことを正確に申告する
- [ ] ContentRootとApp/Depot IDを設定し、SteamPipeで非公開ブランチへアップロードする
- [ ] Steamクライアント経由の初回起動、更新、アンインストール、オフライン起動を確認する
- [ ] ストアページと製品ビルドを発売日の7営業日以上前にレビュー提出する

## 必須グラフィック書き出し

`Marketing/Steam`に書き出し済みです。マスター変更後は `.\Scripts\export-steam-art.ps1` で再生成し、Steamworksの最新テンプレートで安全領域を最終確認します。

| 用途 | 必須サイズ |
|---|---:|
| Header Capsule | 920×430 |
| Small Capsule | 462×174 |
| Main Capsule | 1232×706 |
| Vertical Capsule | 748×896 |
| Screenshot | 1920×1080以上、16:9 |
| Shortcut Icon | 256×256 PNGまたはICO |
| App Icon | 184×184 JPG |
| Library Capsule | 600×900 |
| Library Hero | 3840×1240 PNG、文字なし |
| Library Logo | 幅1280または高さ720までの透過PNG |
| Library Header Capsule | 920×430 |

カプセルには読みやすい製品ロゴのみを載せ、レビュー文や追加コピーは載せません。Library Heroには文字を入れません。スクリーンショットには実ゲーム画面だけを使用します。

## SteamPipe最小構成

Steamworks SDKの `ContentBuilder/content` に `Builds/Windows` の中身を置き、1つのWindows depotへ再帰マッピングします。App IDとDepot IDはSteamworksから発行後に設定し、認証情報やビルドアカウントのパスワードはリポジトリへ保存しません。

## Content Survey記載案

**Pre-Generated AI Content:** The title-screen illustration, app icon, and Steam marketing artwork were created during development with OpenAI image generation under human direction and review. The prompts requested original characters and explicitly excluded real teams, league marks, existing game characters, and watermarks. No live-generated AI is used while the game is running.

公式資料:

- [Graphical Assets](https://partner.steamgames.com/doc/store/assets)
- [Content Survey](https://partner.steamgames.com/doc/gettingstarted/contentsurvey)
- [Uploading to Steam](https://partner.steamgames.com/doc/sdk/uploading)
- [Review Process](https://partner.steamgames.com/doc/store/Review_Process)
