// lib/core/config/app_config.dart
class AppConfig {
  static const String appName = 'BGauss PI App';

  // Reads from --dart-define at build/run time.
  // Emulator default : http://10.0.2.2:5181
  // Physical device  : http://<your-LAN-IP>:5181
  static const String apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5181',
  );
}