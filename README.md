# VRCFTPicoModule (modified)
> [!CAUTION]
> **このモジュールは、元のリポジトリを独自に改変したものですので、動作保証はありません。自己責任でご利用ください。**  
> **This module is an independently modified fork of the original repository. As such, operation is not guaranteed. Please use it at your own risk.**  

> [!TIP]
> アップデートをする際は一度モジュールをアンインストールするか、 `%AppData%\VRCFaceTracking\CustomLibs\f3df57d5-1c5b-887d-abb0-be555e14bf09` の中身を手動で置き換えてください。  
> When updating, please either uninstall the module once or manually replace the contents of `%AppData%\VRCFaceTracking\CustomLibs\f3df57d5-1c5b-887d-abb0-be555e14bf09` 

## VRCFTPicoModule
改変元のオリジナル版は[こちら](https://github.com/lonelyicer/VRCFTPicoModule)


## 使い方
### 1.ダウンロード  
最新のモジュール(VRCFTPicoModule.zip)を[ここ](https://github.com/pikepikeid/VRCFTPicoModule/releases/latest)からダウンロードしてください。

### 2.PICO Connectのプロトコルを切り替える
> [!IMPORTANT]  
> `settings.json`を編集して、`faceTrackingTransferProtocol: 2,`に変更する必要があります。  
> ここが適切に変更されていないと動作しません。念の為、再確認を推奨します。  

設定ファイルは`%USERPROFILE%\AppData\Roaming\PICO Connect\settings.json`または`%AppData%\PICO Connect\settings.json`にあります。  

```
"lab": {
　"quic": true,
　"faceTrackingMode": 1,　←1は顔のみ 4はハイブリッド
　"faceTrackingTransferProtocol": 2,　←ここを変更
　"bodyTracking": false,
　"controllerSensitivity": 50,
　"superResolution": false,
　"gamma": 1
}
```

> [!NOTE]
> フェイストラッキングモードも設定ファイル内で変更できます。
> **「顔のみ」となる1のままにしておくことを推奨します。** 「ハイブリッド」の4にする場合、ARKit用のリップシンクアニメーションなどを用意する必要があり、それらがないと声を出したときに口が閉じるようになってしまいます。VRChat側のVisemeが用意されている都合上、ハイブリッドモードでの動作は恐らく考慮されていないはずなので、変更せずにそのままにしておきましょう。

> “faceTrackingMode”: 0 -> オフ  
> “faceTrackingMode”: 1 -> 顔のみ（フェイストラッキングのみ）【推奨】  
> “faceTrackingMode”: 4 -> ハイブリッド（フェイストラッキングとリップシンク）	

### 3.VRCFTにモジュールをインストールする
`VRCFaceTracking`を起動して、`モジュール管理`タブを開きます。  
`Install Module from .zip`ボタンを押して、`VRCFTPicoModule.zip`を選択してください。  

## モジュールの設定  

`config.ini`を開いて、各種設定が可能です。  
```
# ===== Tracking =====
eye-tracking:enable
expression-tracking:enable

# ===== Eye Gain =====
eye_gain:1.0,1.0

# ===== Mode =====
# === 0:Test-Mode ======================
# === 1:VRCFTPicoModule-Mode ===========
# === 2:Extended-Mode (EXPERIMENTAL) ===
mode:1
```
1. モジュールのインストール先を開いてください`%AppData%\VRCFaceTracking\CustomLibs\f3df57d5-1c5b-887d-abb0-be555e14bf09`  
2. `config.ini`を開いて編集・保存します  
3. 変更を適用するにはVRCFTの再起動が必要です  
### Tracking  
アイトラッキングまたはフェイストラッキングの有効・無効を切り替えます。  
デフォルトで有効`enable`となっています。無効にするには`disable`に変更してください。  

### Eye Gain  
目の可動域を調整できます。  
デフォルトで等倍`1.0,1.0`になっています。それぞれ`x,y`に対応します。  
視線をより大きく動かしたい場合は、`1.2,1.2`などに変更してみてください。 
https://github.com/user-attachments/assets/c2471a69-c00b-4eae-aeea-7837e4e342de

### Mode  
モジュールのモード変更が行えます。  
> mode:0 -> テストモード（PICOからの生のデータを使用します）  
> mode:1 -> 通常モード（これまで通りの動作です）  
> mode:2 -> 拡張モード  

`mode:0`はPICO本体からのデータをそのまま使うので、あくまでも動作テスト用です。[Pico4SAFTextTrackingModule](https://github.com/regzo2/PicoStreamingAssistantFTUDP/tree/vrcfacetracking-module)や[VRCFT-ALVR](https://github.com/alvr-org/VRCFT-ALVR)などに近い動作です。  
`mode:1`がデフォルト値です。これはVRCFTPicoModule本来の動作に近いものです。オリジナル版から多少の変更が入っています。  
`mode:2`は拡張モードとなります。実験的機能のため、思わぬ動作をする可能性があります。`mode:1`をベースに、悲しい顔を意図して出せるように調整してあります。

https://github.com/user-attachments/assets/b7dcc163-15d3-4046-bcab-06abd5b0ee9a
