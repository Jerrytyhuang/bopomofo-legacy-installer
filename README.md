# ㄅ半舊注音安裝程式

在 Windows 10 / 11 加回傳統「ㄅ半」舊注音輸入法（每個字手動選字、字序固定、選完字跳出聯想字）的 GUI 安裝程式。

Windows 內建的「微軟注音」只剩自動選字的新注音模式，就算開啟「使用舊版的微軟注音」也一樣。
舊注音其實還在系統裡，只是對應的鍵盤 profile 被隱藏了；這個工具把它重新註冊並加進鍵盤清單。

## 下載與使用

1. 到 [Releases](../../releases) 下載 `BopomofoLegacyInstaller.exe`
2. 雙擊執行（會要求系統管理員權限）
3. 勾選需要的選項，按「安裝」
4. 登出再登入

右下角輸入法圖示中「左上角缺角的方框」就是 ㄅ半舊注音；用 `Ctrl+Shift` 或 `Win+空白鍵` 跟新注音切換。

> **注意**：設定會套用到「執行這個程式的帳號」。如果是用另一個管理員帳號提權執行，
> 鍵盤清單與聯想字會設定到那個管理員帳號，而不是目前登入的使用者。請用要使用輸入法的帳號本人執行。

## 安裝選項

| 選項 | 做了什麼 |
|---|---|
| （必做）註冊 ㄅ半舊注音 | 在 `HKLM\SOFTWARE\Microsoft\CTF\TIP\{B115690A-EA02-48D5-A231-E3578D2FDF80}\LanguageProfile\0x00000404\{761309DE-317A-11D4-9B5D-0080C882687E}` 註冊傳統注音 (Phonetic) profile，並透過 `Set-WinUserLanguageList` 加進目前使用者的繁體中文鍵盤清單 |
| 設為預設輸入法 | `Set-WinDefaultInputMethodOverride` |
| 開啟聯想字 | `HKCU\Software\Microsoft\IME\15.0\IMETC` 的 `Prompt Associate Phrase.Phonetic = 1`，以及 `HKCU\Software\Microsoft\Windows\CurrentVersion\IME\IMTC70` 的 `Phonetic.AssociatedWord = 1` |
| 開啟「使用舊版的微軟注音」相容性 | `HKCU\Software\Microsoft\Input\TSF\Tsf3Override\{B115690A-…}` 的 `NoTsf3Override2` 與 `NoTsf3Override5 = 1`（前者給 Windows 10，後者給新版 Windows 11） |

## 已知問題

- **第一次使用後聯想字被關掉**：輸入法第一次載入舊注音時，可能把 `Prompt Associate Phrase.Phonetic` 重設成 0。
  這時再開一次安裝程式按「修復聯想字」即可。
- 從 Windows 10 升級到 Windows 11 後，之前裝過的舊注音可能顯示為「無法使用的輸入法」。
  先到「設定 → 時間與語言 → 語言與地區 → 繁體中文 → 語言選項」把它移除，再執行本程式。

## 解除安裝

執行程式按「解除安裝」：從鍵盤清單移除、刪除系統註冊、關閉舊版相容性。原本的微軟注音（新注音）不受影響。

## 自行建置

不需要安裝任何 SDK，使用 Windows 內建的 .NET Framework 4.x 編譯器：

```bat
build.cmd
```

產出 `dist\BopomofoLegacyInstaller.exe`。

## 參考

登錄檔做法參考自老貓測3C〈Windows 11「ㄅ半」傳統舊注音輸入法安裝新增啟用〉：
<https://iqmore.tw/windows-11-chinese-traditional-phonetic>

修改登錄檔有一定風險，請自行評估。
