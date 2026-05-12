# B2B Buyback

Base project setup for a Flutter Android frontend and a .NET 9 backend API.

## Project structure

- `frontend` - Flutter Android application with a buyback dashboard starter UI
- `backend/B2BBuyback.Api` - ASP.NET Core Web API with seeded buyback endpoints
- `B2BBuyback.sln` - solution file for the backend

## Run the backend

```powershell
dotnet run --project backend/B2BBuyback.Api
```

The API starts on `http://localhost:5280` in development.

## Run the Android app

```powershell
cd frontend
flutter pub get
flutter run
```

The Flutter app is configured to call `http://10.0.2.2:5280/api` by default so it works with the Android emulator.

For a physical device, override the API URL:

```powershell
flutter run --dart-define=API_BASE_URL=http://YOUR-LAN-IP:5280/api
```

## VS Code full-stack debug

The workspace now includes shared run/debug settings in `.vscode/`.

- `Full Stack: Android Emulator Debug` starts the .NET API and Flutter app together with debuggers attached.
- `Full Stack: Android Device Debug` does the same, but prompts for the backend API URL to use from a real Android device.
- `Backend: B2B Buyback API` runs the backend in debug mode on `http://0.0.0.0:5280`.

Notes:

- For the emulator, the frontend uses `http://10.0.2.2:5280/api`.
- For a real Android device, use your PC's LAN IP such as `http://192.168.1.10:5280/api`.
- Flutter DevTools opens automatically for Flutter debug sessions.
- If you use a physical device, Windows Firewall may prompt you to allow `dotnet` on your local network.
