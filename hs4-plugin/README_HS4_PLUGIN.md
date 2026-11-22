# HS4 Plugin: Roborock Bridge

This is a real HomeSeer HS4 plugin project that connects to the local Roborock bridge API in this repository.

Project path:

- hs4-plugin/HSPI_RoborockBridge

## What it does

- Adds an HS4 plugin named "Roborock Bridge"
- Adds plugin settings page fields:
  - Bridge URL
  - Bridge API key
  - Default Roborock device ID
- Adds an Event Action type named "Roborock command"
  - Action choices: start, pause, stop, dock
  - Optional per-action device ID override

## Build prerequisites

- .NET SDK installed (Windows)
- HomeSeer plugin NuGet access
- HS4 running on your host

## Build

```powershell
cd d:\projects\roborock_hs4\hs4-plugin\HSPI_RoborockBridge
dotnet restore
dotnet build -c Release
```

## Deploy to HS4

Copy output files from:

- .\bin\Release\net48\

to your HomeSeer install folder where HSPI executables live.

The executable must be named:

- HSPI_RoborockBridge.exe

HS4 should discover the plugin and list it in Plugins.

## Configure in HS4

In the plugin settings page, set:

- Bridge URL (example: http://127.0.0.1:8129)
- API key (must match BRIDGE_API_KEY for the Python bridge)
- Default device ID (from /devices endpoint)

## Create Event Action

In an HS4 event:

1. Add Action
2. Select plugin action "Roborock command"
3. Choose command (start/pause/stop/dock)
4. Optionally set device ID override

## Notes

- This plugin sends commands to the local bridge in src/roborock_hs4_bridge.py.
- Keep bridge and HS4 on the same LAN host or trusted network.
- If you need polling and status devices inside HS4, add feature/device mapping in a next iteration.
