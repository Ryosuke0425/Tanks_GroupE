import asyncio
import websockets
import json
import mysql.connector
import uuid


def connect_to_database():
    return mysql.connector.connect(
        host="localhost",
        user="root",
        password="8VBJtMueZY#w",
        database="game_kadai_database",
    )


async def handle_connection(websocket, path):
    conn = connect_to_database()
    cursor = conn.cursor(dictionary=True)

    async for message in websocket:
        data = json.loads(message)
        print(data)

        if (
            data["type"] == "create_user"
        ):  # Csharpから送られてきたデータtypeに対応した処理
            user_id = str(uuid.uuid4())  # 一意のユーザIDの生成
            try:
                cursor.execute(
                    "SELECT user_id FROM users WHERE username = %s",
                    (data["username"],),
                )
                result = cursor.fetchone()

                if result:  # 以下死んでる、機能的に問題ないので後で修正
                    response = {"status": "success", "user_id": result["user_id"]}
                else:
                    query = "INSERT INTO users (username, user_id) VALUES (%s, %s)"  # SQLの命令
                    cursor.execute(query, (data["username"], user_id))  # %s部分の代入
                    conn.commit()  # データベースへの接続を表すオブジェクト,トランザクションを確定させる
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

        elif data["type"] == "login":  # Csharpから送られてきたデータtypeに対応した処理
            query = "SELECT user_id FROM users WHERE userName=%s"
            cursor.execute(query, (data["username"],))  # csharpのusernameを参照、Player
            result = cursor.fetchone()
            if result:
                response = {"status": "success", "user_id": result["user_id"]}
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

        elif data["type"] == "modify_username":  # ユーザ名変更時の処理
            query = "UPDATE users SET username = %s WHERE user_id=%s"
            cursor.execute(
                query, (data["username"], data["user_id"])
            )  # csharpのusernameを参照、Player
            conn.commit()  # 変更をデータベースに反映
            response = {"status": "success_modify"}
            print(response)
            await websocket.send(json.dumps(response))

    cursor.close()
    conn.close()


start_server = websockets.serve(handle_connection, "localhost", 8765)
asyncio.get_event_loop().run_until_complete(start_server)
asyncio.get_event_loop().run_forever()
