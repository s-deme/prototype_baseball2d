# MIRACLE DIAMOND / ミラクル・ダイヤモンド

![Steam capsule master](Marketing/steam-capsule-master.png)

短い3イニングに打撃、投球、走者、必殺技、逆転演出を詰め込んだ、1人用2Dアーケード野球ゲームです。外部アセットやSteamworks SDKに依存せず、Unity標準APIだけで動作します。

## 現在入っているもの

- タイトル → 遊び方 → 3イニング戦 → 結果 → 再戦の一連の流れ
- 打撃のコース読みとタイミング、投球のコース・3球種・リリースメーター
- ボール、ストライク、アウト、走者進塁、押し出し、得点、延長、サヨナラ
- 攻守それぞれの必殺技とライバル側の必殺技
- ヒット、本塁打、画面揺れ、フラッシュ、パーティクル、合成効果音・BGM
- 音量、フルスクリーン、画面揺れ、フラッシュ軽減の設定保存
- 試合、勝利、本塁打、最高得点、最長飛距離の戦績保存
- 3勝または秘密コマンドで解放される月影スタジアムと隠しチーム
- 勝つたびに3ライバルを巡るアーケードラダーと連勝記録
- キーボード、一般的なXInputゲームパッド操作（メニューはマウスにも対応）

## 開発環境

- Unity `6000.3.22f1`（Unity 6.3 LTS）
- Windows 10/11 x64
- レンダーパイプライン: Built-in
- 日本語フォント: Noto Sans JP（SIL Open Font License 1.1）

Unity Hubでこのフォルダーを開き、`Assets/Scenes/Main.unity`を再生してください。起動オブジェクトと画面は実行時に自動生成されます。

## 操作

| 操作 | キーボード | ゲームパッド |
|---|---|---|
| 移動・選択 | 矢印 / WASD | 左スティック |
| 決定・打つ・投げる | Z / Space / Enter | A |
| 必殺技 | X / Shift | Y |
| 戻る・ポーズ | Esc | B |

攻撃では左右で狙うコースを決め、球が手元に来る瞬間に打ちます。守備では左右でコース、上下で球種を選び、メーター中央で投球します。

## 検証とビルド

```powershell
.\Scripts\check.ps1
.\Scripts\build-windows.ps1
.\Scripts\capture-screenshots.ps1
```

Windowsビルドは `Builds/Windows/MiracleDiamond.exe` に生成されます。Unityライセンスが有効である必要があります。
撮影スクリプトはQA画面3枚とSteam用ゲームプレイ画面5枚を `Marketing/Screenshots` に生成します。

## リリース資料

- [Steamリリースチェックリスト](Docs/STEAM_RELEASE.md)
- [手動QAチェックリスト](Docs/QA_CHECKLIST.md)
- [ストア文面案](Docs/STEAM_STORE_COPY.md)
- [マーケティング素材と生成記録](Marketing/README.md)

## ライセンス

ソースコード、ゲーム内容、同梱画像は [LICENSE.txt](LICENSE.txt) のとおり All Rights Reserved です。Unity本体とUnity PlayerにはUnity Technologiesの各ライセンスが適用されます。
