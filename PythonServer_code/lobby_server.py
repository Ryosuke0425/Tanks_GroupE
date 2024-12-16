from typing import List
from queue import Queue
from asyncio import sleep, create_task
from contextlib import asynccontextmanager
import asyncio
import uuid  # マッチID生成のため

from fastapi import FastAPI, WebSocket, WebSocketDisconnect


class RoomManager:
    def __init__(self):
        self.connections: List[WebSocket] = []  # すべてのWebSocket接続
        self.matching = Queue()  # マッチング待ちのWebSocket
        self.ready_players = set()  # Ready状態のWebSocket
        self.pvp_server_url = "ws://localhost:8080"  # PVPサーバーのWebSocket URL
        self.player_number_map = {}  # WebSocketとプレイヤー番号のマッピング
        self.match_id_map = {}  # WebSocketとマッチIDのマッピング

    async def connect(self, ws: WebSocket):
        """WebSocket接続を受け入れる"""
        await ws.accept()
        self.connections.append(ws)
        print(f"A player connected: {ws}")

    async def join(self, ws: WebSocket):
        """プレイヤーをマッチングキューに追加"""
        print(f"Player joined: {ws}")
        await ws.send_json({"type": "status_update", "status": "matching"})
        self.matching.put(ws)

    async def match(self):
        """2人のプレイヤーをマッチングしてマッチIDを生成"""
        while True:
            if self.matching.qsize() >= 2:
                ws1 = self.matching.get()
                ws2 = self.matching.get()

                # マッチIDを生成
                match_id = str(uuid.uuid4())
                print(f"Matched players: {ws1}, {ws2} with match_id: {match_id}")

                # プレイヤー番号を割り当て
                self.player_number_map[ws1] = 1
                self.player_number_map[ws2] = 2
                self.match_id_map[ws1] = match_id
                self.match_id_map[ws2] = match_id

                # プレイヤーにマッチ情報を送信
                await ws1.send_json({"type": "status_update", "status": "matched", "player_number": 1, "match_id": match_id})
                await ws2.send_json({"type": "status_update", "status": "matched", "player_number": 2, "match_id": match_id})

                # 両プレイヤーがreadyになるのを待つ
                create_task(self.wait_for_ready(ws1, ws2, match_id))
            await sleep(1)

    async def wait_for_ready(self, ws1: WebSocket, ws2: WebSocket, match_id: str):
        """両プレイヤーがready状態になるのを待つ"""
        print(f"Waiting for both players to be ready for match_id: {match_id}")

        while True:
            if ws1 in self.ready_players and ws2 in self.ready_players:
                print(f"Both players are ready for match_id: {match_id}. Redirecting to PVP server...")
                await self.redirect_to_pvp(ws1, ws2, match_id)
                return
            await asyncio.sleep(0.5)

    async def set_ready(self, ws: WebSocket):
        """プレイヤーをready状態にする"""
        self.ready_players.add(ws)
        print(f"Player ready: {ws}")

    async def redirect_to_pvp(self, ws1: WebSocket, ws2: WebSocket, match_id: str):
        """PVPサーバーにリダイレクトし、Lobby接続をクローズ"""
        try:
            redirect_message = {
                "type": "game_start",
                "pvp_server_url": self.pvp_server_url,
                "match_id": match_id
            }
            await ws1.send_json(redirect_message)
            await ws2.send_json(redirect_message)
            print(f"Redirected players to PVP server at {self.pvp_server_url} with match_id: {match_id}")

            # Lobby接続をクローズ
            await self.disconnect(ws1)
            await self.disconnect(ws2)

        except Exception as e:
            print(f"Error redirecting players to PVP server: {e}")

    async def disconnect(self, ws: WebSocket):
        """プレイヤーの切断処理"""
        if ws in self.connections:
            self.connections.remove(ws)
        if ws in self.player_number_map:
            del self.player_number_map[ws]
        if ws in self.match_id_map:
            del self.match_id_map[ws]
        if ws in self.ready_players:
            self.ready_players.remove(ws)
        print(f"Player disconnected: {ws}")
        await ws.close()

    async def broadcast(self, sender_ws: WebSocket, data: dict):
        """全ての接続されているプレイヤーにデータを送信"""
        for connection in self.connections:
            sender_name = "You" if connection == sender_ws else "Opponent"
            message = {"type": "chat", "sender": sender_name, "text": data.get("text")}
            await connection.send_json(message)


manager = RoomManager()

@asynccontextmanager
async def lifespan(_: FastAPI):
    create_task(manager.match())
    yield

app = FastAPI(lifespan=lifespan)

@app.websocket("/")
async def handler(ws: WebSocket):
    """WebSocket接続を管理するハンドラー"""
    await manager.connect(ws)
    await manager.join(ws)
    try:
        while True:
            req = await ws.receive_json()
            print(req)

            if req.get("type") == "status_update" and req.get("status") == "ready":
                await manager.set_ready(ws)

            elif req.get("type") == "send_message":
                chat_message = req.get("text")
                await manager.broadcast(ws, {"text": chat_message})
                print(f"Chat message: {chat_message}")

            elif req.get("type") == "game_over":
                await manager.disconnect(ws)
                break

            elif req.get("type") == "stamp":
                # スタンプメッセージの処理を追加
                stamp_id = req.get("stampId")
                match_id = manager.match_id_map.get(ws)
                if match_id:
                    # 同じマッチ内の相手プレイヤーにスタンプメッセージを送信
                    for connection in manager.connections:
                        if manager.match_id_map.get(connection) == match_id and connection != ws:
                            await connection.send_json({"type": "stamp", "stampId": stamp_id})
                    print(f"Stamp {stamp_id} sent from {ws} to match {match_id}")
                else:
                    print(f"No match found for WebSocket: {ws}")

            else:
                # 未定義のメッセージタイプはブロードキャスト
                await manager.broadcast(ws, req)

    except WebSocketDisconnect:
        await manager.disconnect(ws)
        print(f"Player disconnected from lobby.")
