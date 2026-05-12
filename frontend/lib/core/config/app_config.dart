class AppConfig {
  static const appName = 'B2B Buyback';
  static const apiBaseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5280/api',
  );
}
