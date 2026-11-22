import asyncio
import dataclasses
import os
import pathlib
from typing import Any

from fastapi import Depends, FastAPI, Header, HTTPException
from pydantic import BaseModel
from roborock.devices.device_manager import UserParams, create_device_manager
from roborock.devices.file_cache import FileCache, load_value, store_value
from roborock.web_api import RoborockApiClient

CACHE_DIR = pathlib.Path.home() / ".cache"
USER_PARAMS_PATH = CACHE_DIR / "roborock-user-params.pkl"
DEVICE_CACHE_PATH = CACHE_DIR / "roborock-device-cache.pkl"

DEFAULT_COMMAND_MAP = {
    "start": "app_start",
    "pause": "app_pause",
    "stop": "app_stop",
    "dock": "app_charge",
}


class VerifyCodeRequest(BaseModel):
    code: str


class CommandRequest(BaseModel):
    action: str


class RoborockBridge:
    def __init__(self, email: str):
        self.email = email
        self._lock = asyncio.Lock()
        self._cache = FileCache(DEVICE_CACHE_PATH)
        self._device_manager = None

    async def _load_user_params(self) -> UserParams | None:
        return await load_value(USER_PARAMS_PATH)

    async def _save_user_params(self, user_params: UserParams) -> None:
        CACHE_DIR.mkdir(parents=True, exist_ok=True)
        await store_value(USER_PARAMS_PATH, user_params)

    async def request_login_code(self) -> dict[str, str]:
        web_api = RoborockApiClient(username=self.email)
        await web_api.request_code()
        return {"message": "Verification code sent to your email."}

    async def verify_code(self, code: str) -> dict[str, str]:
        web_api = RoborockApiClient(username=self.email)
        user_data = await web_api.code_login(code)
        base_url = await web_api.base_url
        user_params = UserParams(username=self.email, user_data=user_data, base_url=base_url)
        await self._save_user_params(user_params)
        await self._connect()
        return {"message": "Authenticated and connected."}

    async def _connect(self) -> None:
        async with self._lock:
            user_params = await self._load_user_params()
            if user_params is None:
                raise HTTPException(status_code=401, detail="Not authenticated. Call /auth/request-code and /auth/verify-code first.")

            self._device_manager = await create_device_manager(user_params, cache=self._cache)

    async def _require_connected(self):
        if self._device_manager is None:
            await self._connect()

    async def list_devices(self) -> list[dict[str, Any]]:
        await self._require_connected()
        devices = await self._device_manager.get_devices()

        result = []
        for device in devices:
            result.append(
                {
                    "name": device.name,
                    "id": device.duid,
                    "model": getattr(device, "model", "unknown"),
                    "supports_v1": bool(device.v1_properties),
                }
            )
        return result

    async def get_status(self, device_id: str) -> dict[str, Any]:
        await self._require_connected()
        device = await self._get_v1_device(device_id)
        status_trait = device.v1_properties.status
        await status_trait.refresh()
        return dataclasses.asdict(status_trait)

    async def send_command(self, device_id: str, action: str) -> dict[str, Any]:
        await self._require_connected()
        device = await self._get_v1_device(device_id)
        command = DEFAULT_COMMAND_MAP.get(action.lower())
        if not command:
            raise HTTPException(
                status_code=400,
                detail=f"Unsupported action '{action}'. Valid actions: {', '.join(DEFAULT_COMMAND_MAP.keys())}",
            )

        result = await device.v1_properties.command.send(command)
        return {"device_id": device_id, "action": action, "command": command, "result": result}

    async def _get_v1_device(self, device_id: str):
        devices = await self._device_manager.get_devices()
        for device in devices:
            if str(device.duid) == device_id and device.v1_properties:
                return device

        raise HTTPException(status_code=404, detail=f"No V1 vacuum found with id {device_id}")


def create_app() -> FastAPI:
    email = os.getenv("ROBOROCK_EMAIL", "").strip()
    if not email:
        raise RuntimeError("ROBOROCK_EMAIL is required")

    api_key = os.getenv("BRIDGE_API_KEY", "").strip()
    bridge = RoborockBridge(email=email)

    app = FastAPI(title="Roborock HS4 Bridge", version="1.0.0")

    def verify_api_key(x_api_key: str | None = Header(default=None)) -> None:
        if not api_key:
            return

        if x_api_key != api_key:
            raise HTTPException(status_code=401, detail="Missing or invalid x-api-key")

    @app.get("/health")
    async def healthcheck() -> dict[str, str]:
        return {"status": "ok"}

    @app.post("/auth/request-code", dependencies=[Depends(verify_api_key)])
    async def request_code() -> dict[str, str]:
        return await bridge.request_login_code()

    @app.post("/auth/verify-code", dependencies=[Depends(verify_api_key)])
    async def verify_code(payload: VerifyCodeRequest) -> dict[str, str]:
        return await bridge.verify_code(payload.code)

    @app.get("/devices", dependencies=[Depends(verify_api_key)])
    async def devices() -> list[dict[str, Any]]:
        return await bridge.list_devices()

    @app.get("/status/{device_id}", dependencies=[Depends(verify_api_key)])
    async def status(device_id: str) -> dict[str, Any]:
        return await bridge.get_status(device_id)

    @app.post("/command/{device_id}", dependencies=[Depends(verify_api_key)])
    async def command(device_id: str, payload: CommandRequest) -> dict[str, Any]:
        return await bridge.send_command(device_id, payload.action)

    return app


app = create_app()
