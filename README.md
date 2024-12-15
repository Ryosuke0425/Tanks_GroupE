# Tanks_GroupE

## アイテム課題
### 仕様
#### ログインボーナス
* 一日一回,初めのログインでアイテムがもらえる.
* ログイン数が増えていくごとに貰えるアイテムが豪華になる.7日までいくと一度リセットされる.
#### Stamina
* 上限は5
* バトル開始時に-1
* StaminaUp使用で+1
* Staminaが0になるとバトル開始ができない.バトル開始しようとすると「No Stamina!」と表示される.
#### StaminaUp
* 1つ消費することでStaminaが+1される
* Staminaの上限は5であるためStaminaが5の時に使用すると「Failded to use StaminaUp」と表示される
* StaminaUp数が0の時は使用できず,使用しようとすると「Failded to use StaminaUp」と表示される
#### ArmorPlus
* 1つ消費することで「Armor Normal」から「Armor * 2」に変わる.これにより次のバトル時にHPが2倍になる.
* 既に「ArmorPlus * 2」のときは使用できない.使用しようとすると「Failed to use ArmorPlus」と表示される.
* ArmorPlus数が0の時は使用できず,使用しようとすると「Failded to use ArmorPlus」と表示される
