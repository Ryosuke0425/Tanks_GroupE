import asyncio
import websockets
import json
import mysql.connector
import uuid
from datetime import datetime


def connect_to_database():
    return mysql.connector.connect(
        host="localhost",
        user="root",
        password="8VBJtMueZY#w",  # ローカルのMySQLのパスワード
        database="game_test",
    )


async def handle_connection(websocket, path):
    # conn = connect_to_database()
    # cursor = conn.cursor(dictionary=True)

    async for message in websocket:
        conn = connect_to_database()
        cursor = conn.cursor(dictionary=True)
        data = json.loads(message)
        print(data)

        if (
            data["type"] == "create_user"
        ):  # Csharpから送られてきたデータtypeに対応した処理
            user_id = str(uuid.uuid4())  # 一意のユーザIDの生成
            print("create user?")
            # user_id = "1"
            print(user_id)
            try:
                cursor.execute(  # すでにusersにデータがあるかないか
                    "SELECT user_id FROM users WHERE username = %s",
                    (data["username"],),
                )
                result = cursor.fetchone()

                if result:  # 以下死んでる、機能的に問題ないので後で修正、ある場合
                    response = {"status": "success", "user_id": result["user_id"]}
                    print("?")
                else:  # ない場合
                    query = "INSERT INTO users (username, user_id) VALUES (%s, %s)"  # SQLの命令
                    cursor.execute(query, (data["username"], user_id))  # %s部分の代入
                    conn.commit()  # データベースへの接続を表すオブジェクト,トランザクションを確定させる
                    query = "INSERT INTO battlerecords (user_id,wins,losses,total_matches,pre_rank) VALUES (%s,%s,%s,%s,%s)"
                    cursor.execute(query, (user_id, 0, 0, 0, "圏外"))
                    conn.commit()
                    # アイテム課題:アイテム数の初期化
                    query = "INSERT INTO user_items (user_id,stamina,staminaUp,armorPlus) VALUES (%s,%s,%s,%s)"
                    cursor.execute(query, (user_id, 3, 3, 3))
                    conn.commit()
                    print("user item initialized")
                    # アイテム課題:最終ログイン履歴の初期化
                    query = "INSERT INTO user_login (user_id,last_login,consecutive_days) VALUES (%s,%s,%s)"
                    cursor.execute(query, (user_id, datetime.now().date(), -1))
                    conn.commit()

                    cursor.close()
                    conn.close()

                    response = {
                        "status": "success",
                        "user_id": user_id,
                    }  # データベースへの登録完了を示す辞書型
                    print(response)
            except mysql.connector.Error as err:
                response = {
                    "status": "failure",
                    "error": str(err),
                }  # 失敗したときはこっちが返ってくる
            await websocket.send(
                json.dumps(response)
            )  # UnityにJson形式のレスポンスを非同期で返す
            print(response)
            print("create")

        elif data["type"] == "login":  # Csharpから送られてきたデータtypeに対応した処理
            query = "SELECT user_id FROM users WHERE userName=%s"
            cursor.execute(query, (data["username"],))  # csharpのusernameを参照、Player
            result = cursor.fetchone()
            if result:
                response = {"status": "success", "user_id": result["user_id"]}
                print("uk", data["username"], data["user_id"], "o")
            else:  # 以下死んでる、機能的に問題ないので後で修正
                user_id = str(uuid.uuid4())
                query = (
                    "INSERT INTO users (username, user_id) VALUES (%s, %s)"  # SQLの命令
                )
                cursor.execute(query, (data["username"], user_id))  # %s部分の代入
                conn.commit()  # データベースへの接続を表すオブジェクト,トランザクションを確定させる
                cursor.close()
                conn.close()

                response = {
                    "status": "success",
                    "user_id": user_id,
                }  # データベースへの登録完了を示す辞書型
                # response = {"status": "failure", "error": "User not found"}
            print(response)
            await websocket.send(json.dumps(response))
        elif data["type"] == "login_bonus":
            # データベースからアイテムを取得
            query = (
                "SELECT last_login, consecutive_days FROM user_login WHERE user_id=%s"
            )
            cursor.execute(query, (data["user_id"],))  # csharpのuser_idを参照、Player
            result = cursor.fetchone()
            first = (not (result["last_login"] == datetime.now().date())) or (
                result["consecutive_days"] == -1
            )
            consecutive_days = result["consecutive_days"]
            bonus = ((consecutive_days + 1) % 7) + 1
            if first:
                # データベース更新
                query = """UPDATE user_items SET staminaUp=staminaUp+%s,ArmorPlus=ArmorPlus+%s WHERE user_id=%s"""
                cursor.execute(
                    query,
                    (
                        bonus,
                        bonus,
                        data["user_id"],
                    ),
                )  # csharpのuser_idを参照、Player
                conn.commit()
                query = """UPDATE user_login SET last_login = CURDATE() ,consecutive_days = consecutive_days+1 WHERE user_id=%s"""
                cursor.execute(
                    query, (data["user_id"],)
                )  # csharpのuser_idを参照、Player
                conn.commit()
            # データベースからアイテムを取得
            query = (
                "SELECT stamina,staminaUp,armorPlus FROM user_items WHERE user_id=%s"
            )
            cursor.execute(query, (data["user_id"],))  # csharpのuser_idを参照、Player
            result = cursor.fetchone()
            print(result)
            if result:
                response = {
                    "status": "success",
                    "stamina": result["stamina"],
                    "staminaUp": result["staminaUp"],
                    "armorPlus": result["armorPlus"],
                    "first": first,
                    "consecutive_days": consecutive_days,
                }
                print("if")
            else:  # 以下死んでる、機能的に問題ないので後で修正
                user_id = str(uuid.uuid4())
                query = "INSERT INTO user_items (user_id,stamina,staminaUp,armorPlus) VALUES (%s, %s,%s,%s)"  # SQLの命令
                cursor.execute(query, (user_id, 3, 3, 3))  # %s部分の代入
                conn.commit()  # データベースへの接続を表すオブジェクト,トランザクションを確定させる
                query = "INSERT INTO user_login (user_id,last_login,consecutive_days) VALUES (%s,%s,%s)"
                cursor.execute(query, (user_id, datetime.now().date(), -1))
                conn.commit()
                cursor.close()
                conn.close()

                response = {
                    "status": "success",
                    "stamina": 3,
                    "staminaUp": 3,
                    "armorPlus": 3,
                    "first": False,
                    "consecutive_days": -1,
                    # "time":datetime.now().date()
                }  # データベースへの登録完了を示す辞書型
                # response = {"status": "failure", "error": "User not found"}
            await websocket.send(json.dumps(response))
        elif data["type"] == "modify_username":  # ユーザ名変更時の処理
            query = "UPDATE users SET username = %s WHERE user_id=%s"
            cursor.execute(
                query, (data["username"], data["user_id"])
            )  # csharpのusernameを参照、Player
            conn.commit()  # 変更をデータベースに反映
            response = {"status": "success_modify"}
            print(response)
            await websocket.send(json.dumps(response))
        elif data["type"] == "send_item":
            query = "UPDATE user_items SET stamina = %s,staminaUp = %s,armorPlus=%s WHERE user_id=%s"
            cursor.execute(
                query,
                (
                    data["stamina"],
                    data["staminaUp"],
                    data["armorPlus"],
                    data["user_id"],
                ),
            )  # csharpのusernameを参照、Player
            conn.commit()  # 変更をデータベースに反映
            response = {"status": "success_item"}
            await websocket.send(json.dumps(response))
        elif (
            data["type"] == "update_winer"
        ):  # ユーザが勝ったときの処理、ユーザidが送られる
            query = "UPDATE battlerecords SET wins = wins + 1, total_matches = total_matches+1 WHERE user_id=%s"
            cursor.execute(query, (data["user_id"],))
            conn.commit()
            response = {"status": "success_update_winer"}
            print(response)
            await websocket.send(json.dumps(response))

        elif (
            data["type"] == "update_loser"
        ):  # ユーザが負けたときの処理、ユーザidが送られる
            query = "UPDATE battlerecords SET wins = wins - 1, total_matches = total_matches - 1 WHERE user_id=%s"
            cursor.execute(query, (data["user_id"],))
            conn.commit()
            response = {"status": "success_update_loser"}
            print(response)
            await websocket.send(json.dumps(response))

        elif data["type"] == "show_winers":  # 自身の戦績とTop10の人の戦績を返す
            try:
                print("OK get message")
                query = """SELECT user_rank, ranked_records.user_id, username, wins, losses, win_rate FROM (SELECT ROW_NUMBER() OVER (ORDER BY wins / NULLIF(total_matches,0) DESC, total_matches DESC) AS user_rank,
                battlerecords.user_id,
                users.username,
                battlerecords.wins,
                battlerecords.losses,
                CAST(wins / total_matches AS FLOAT) AS win_rate 
                FROM battlerecords 
                JOIN users ON users.user_id = battlerecords.user_id 
                WHERE total_matches >= 10) AS ranked_records ORDER BY user_rank LIMIT 10"""
                cursor.execute(query)
                top_users = cursor.fetchall()
                print(top_users)
                cursor.execute(
                    """SELECT users.user_id, username, wins, losses, 
                    CAST(wins / total_matches AS FLOAT) AS win_rate, pre_rank FROM battlerecords JOIN users ON users.user_id = battlerecords.user_id WHERE users.user_id = %s """,
                    (data["user_id"],),
                )
                player_info = cursor.fetchone()
                print(player_info)
                if player_info and player_info["user_id"] not in [
                    top_users[i]["user_id"] for i in range(len(top_users))
                ]:  # 今のプレイヤーがランキングに入っているかいないかで場合分け、ランキングに入っていればfalseを返す、ランキングには10戦未満のプレイヤーは入らないので、暗黙的に10戦未満だがランキングに入っていることはない
                    player_info["user_rank"] = "圏外"
                else:
                    for i, user in enumerate(top_users):
                        if user["user_id"] == player_info["user_id"]:
                            player_info["user_rank"] = i + 1

                player_info = {  # ユーザ情報の整理
                    "user_rank": player_info["user_rank"],
                    "user_id": player_info["user_id"],
                    "username": player_info["username"],
                    "wins": player_info["wins"],
                    "losses": player_info["losses"],
                    "win_rate": player_info["win_rate"],
                    "pre_rank": player_info["pre_rank"],
                }
                query = "update battlerecords set pre_rank = %s where user_id=%s"
                cursor.execute(
                    query, (player_info["user_rank"], player_info["user_id"])
                )
                conn.commit()  # pre_rankの更新

                print(top_users)
                print(player_info)
                response = {
                    "status": "success_ranking",
                    "top_users": top_users,
                    "player_info": player_info,
                }
            except mysql.connector.Error as err:
                response = {
                    "status": "failure",
                    "error": str(err),
                }  # 失敗したときはこっちが返ってくる
            print(response)
            print(json.dumps(response))
            await websocket.send(json.dumps(response))

        cursor.close()
        conn.close()


start_server = websockets.serve(handle_connection, "localhost", 8765)
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
