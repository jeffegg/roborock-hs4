# Roborock Qrevo 2 Pro -> HomeSeer HS4 Bridge

This project creates a local HTTP bridge so HomeSeer HS4 can control your Roborock vacuum.

## What this supports

- Request Roborock login code (email based)
- Verify code and cache session
- Discover devices
- Read status
- Send commands: start, pause, stop, dock

## 1) Create and activate virtual environment

```powershell
cd d:\projects\roborock_hs4
py -3.11 -m venv .venv
.\.venv\Scripts\Activate.ps1
```

## 2) Install dependencies

```powershell
pip install -r requirements.txt
```

## 3) Configure environment

```powershell
copy .env.example .env
```

Edit `.env` and set:

- `ROBOROCK_EMAIL`: your Roborock app email
- `BRIDGE_API_KEY`: long random key for HS4 requests
- `BRIDGE_HOST` and `BRIDGE_PORT` if you need custom bind values

## 4) Run the bridge

```powershell
$env:ROBOROCK_EMAIL = "you@example.com"
$env:BRIDGE_API_KEY = "change-me"
$env:BRIDGE_HOST = "0.0.0.0"
$env:BRIDGE_PORT = "8129"
uvicorn src.roborock_hs4_bridge:app --host $env:BRIDGE_HOST --port $env:BRIDGE_PORT
```

## 5) First-time auth

1. Request code:

```powershell
curl -Method Post -Uri "http://127.0.0.1:8129/auth/request-code" -Headers @{"x-api-key"="change-me"}
```

2. Verify code:

```powershell
curl -Method Post -Uri "http://127.0.0.1:8129/auth/verify-code" -Headers @{"x-api-key"="change-me"; "Content-Type"="application/json"} -Body '{"code":"123456"}'
```

3. List devices and copy the `id` for your Qrevo 2 Pro:

```powershell
curl -Uri "http://127.0.0.1:8129/devices" -Headers @{"x-api-key"="change-me"}
```

## 6) HS4 event actions

In HS4, create events that run a command/script (PowerShell/curl) to hit the bridge.

Example command from HS4 host to start cleaning:

```powershell
curl -Method Post -Uri "http://127.0.0.1:8129/command/<DEVICE_ID>" -Headers @{"x-api-key"="change-me"; "Content-Type"="application/json"} -Body '{"action":"start"}'
```

Use these actions:

- `start`
- `pause`
- `stop`
- `dock`

Status check:

```powershell
curl -Uri "http://127.0.0.1:8129/status/<DEVICE_ID>" -Headers @{"x-api-key"="change-me"}
```

## Notes

- Keep the bridge on your local network only.
- If session expires, repeat auth steps 1-2.
- The bridge uses Roborock cloud login and local cached session data.

## HS4 plugin project

An actual HS4 plugin implementation is included in this repository:

- `hs4-plugin/HSPI_RoborockBridge`

Build and deploy instructions are in:

- `hs4-plugin/README_HS4_PLUGIN.md`
