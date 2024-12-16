from typing import Dict, Optional
from fastapi import FastAPI, WebSocket, WebSocketDisconnect
from starlette.websockets import WebSocketState
import asyncio

class MatchManager:
    def __init__(self):
        self.matches: Dict[str, Dict[str, WebSocket]] = {}

    async def safe_send(self, ws: WebSocket, message: dict):
        try:
            print(f"Sending message to {id(ws)}: {message}")
            await ws.send_json(message)
            print(f"Sent to {id(ws)}: {message}")
        except Exception as e:
            print(f"Error sending message to {id(ws)}: {e}")

    async def start_match(self, match_id: str, ws1: WebSocket, ws2: WebSocket):
        if match_id not in self.matches:
            print(f"start_match: Match {match_id} not found.")
            return

        if ws1.application_state != WebSocketState.CONNECTED or ws2.application_state != WebSocketState.CONNECTED:
            print(f"One of the players is not connected at the start of match {match_id}. Closing match.")
            await self.close_match(match_id, ws1, ws2)
            return

        print(f"Match {match_id} ready to start with player1={id(ws1)} player2={id(ws2)}")

        await self.safe_send(ws1, {"type": "assigned_role", "status": "ready", "role": "player1"})
        await self.safe_send(ws2, {"type": "assigned_role", "status": "ready", "role": "player2"})

        await self.handle_start_game(match_id)

    async def handle_start_game(self, match_id: str):
        match = self.matches.get(match_id)
        if match and "player1" in match and "player2" in match:
            ws1 = match["player1"]
            ws2 = match["player2"]
            print(f"Starting match {match_id}")
            await self.safe_send(ws1, {"type": "game_start", "status": "start", "opponent": "player2"})
            await self.safe_send(ws2, {"type": "game_start", "status": "start", "opponent": "player1"})

    async def handle_round_end(self, ws: WebSocket, opponent_ws: WebSocket, winner: str, loser: str):
        result_message = {"type": "round_result", "status": "end", "winner": winner, "loser": loser}
        await self.safe_send(ws, result_message)
        await self.safe_send(opponent_ws, result_message)

    async def close_match(self, match_id: str, ws1: Optional[WebSocket], ws2: Optional[WebSocket]):
        if match_id not in self.matches:
            print(f"close_match: Match {match_id} is already closed.")
            return

        print(f"Closing match {match_id}")
        self.matches.pop(match_id, None)

        for ws in [ws1, ws2]:
            if ws and ws.application_state == WebSocketState.CONNECTED:
                try:
                    await ws.close()
                    print(f"WebSocket {id(ws)} closed.")
                except Exception as e:
                    print(f"Error closing WebSocket {id(ws)}: {e}")

match_manager = MatchManager()
app = FastAPI()

@app.websocket("/match/{match_id}/{player_id}")
async def match_handler(ws: WebSocket, match_id: str, player_id: str):
    await ws.accept()
    print(f"Connection attempt: match_id={match_id}, player_id={player_id}, ws_id={id(ws)}")

    try:
        match = match_manager.matches.get(match_id)

        if not match:
            match_manager.matches[match_id] = {"player1": ws}
            await match_manager.safe_send(ws, {"type": "assigned_role", "status": "waiting", "role": "player1"})
            print(f"Player1 connected: {id(ws)} for match {match_id}")
        elif "player2" not in match:
            match["player2"] = ws
            await match_manager.safe_send(ws, {"type": "assigned_role", "status": "ready", "role": "player2"})
            print(f"Player2 connected: {id(ws)} for match {match_id}")
            await match_manager.start_match(match_id, match["player1"], match["player2"])
        else:
            await match_manager.safe_send(ws, {"type": "error", "status": "full", "message": "Match is full"})
            print(f"Match {match_id} is full. Closing connection for ws_id={id(ws)}")
            await ws.close()
            return

        while True:
            msg = await ws.receive_json()
            print(f"Received in match_handler: {msg}")

            msg_type = msg.get("type")
            if msg_type in {"position_update", "fire", "mine"}:
                opponent_ws = None
                current_match = match_manager.matches.get(match_id)
                if current_match:
                    if ws == current_match.get("player1"):
                        opponent_ws = current_match.get("player2")
                    elif ws == current_match.get("player2"):
                        opponent_ws = current_match.get("player1")

                if opponent_ws and opponent_ws.application_state == WebSocketState.CONNECTED:
                    await match_manager.safe_send(opponent_ws, msg)
                else:
                    print("Opponent WebSocket not connected. Cannot forward message.")

            elif msg_type == "round_end":
                winner = msg.get("winner")
                loser = msg.get("loser")
                print(f"Handling round_end: winner={winner}, loser={loser}")
                opponent_ws = None
                current_match = match_manager.matches.get(match_id)
                if current_match:
                    if ws == current_match.get("player1"):
                        opponent_ws = current_match.get("player2")
                    elif ws == current_match.get("player2"):
                        opponent_ws = current_match.get("player1")

                if opponent_ws and opponent_ws.application_state == WebSocketState.CONNECTED:
                    await match_manager.handle_round_end(ws, opponent_ws, winner, loser)

            elif msg_type == "start_game":
                print(f"Start game message received for match_id={match_id}")
                await match_manager.handle_start_game(match_id)

            else:
                print(f"Unknown message type '{msg_type}' from player, ignoring.")

    except WebSocketDisconnect:
        print(f"A player in match {match_id} disconnected.")
        match = match_manager.matches.get(match_id)
        if match:
            opponent_ws = None
            if ws == match.get("player1"):
                opponent_ws = match.get("player2")
            elif ws == match.get("player2"):
                opponent_ws = match.get("player1")
            if opponent_ws and opponent_ws.application_state == WebSocketState.CONNECTED:
                await match_manager.safe_send(opponent_ws, {"type": "opponent_disconnected", "status": "end"})
            await match_manager.close_match(match_id, ws, opponent_ws)
    except Exception as e:
        print(f"Unexpected error in match_handler for match {match_id}: {e}")
        import traceback
        traceback.print_exc()
        match = match_manager.matches.get(match_id)
        if match:
            opponent_ws = None
            if ws == match.get("player1"):
                opponent_ws = match.get("player2")
            elif ws == match.get("player2"):
                opponent_ws = match.get("player1")
            if opponent_ws and opponent_ws.application_state == WebSocketState.CONNECTED:
                await match_manager.safe_send(opponent_ws, {"type": "error", "status": "unexpected", "message": str(e)})
            await match_manager.close_match(match_id, ws, opponent_ws)
