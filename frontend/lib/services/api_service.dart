// lib/services/api_service.dart
import 'dart:convert';
import 'dart:io';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5181',
  );

  static const _kToken        = 'dealer_token';
  static const _kRefreshToken = 'dealer_refresh_token';
  static const _kDealerCode   = 'dealer_code';
  static const _kDealerName   = 'dealer_name';
  static const _kDealerId     = 'dealer_id';
  static const _kMobile       = 'dealer_mobile';
  static const _kCity         = 'dealer_city';

  static Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_kToken);
  }

  static Future<Map<String, String>> _authHeaders() async {
    final token = await getToken();
    return {
      'Content-Type': 'application/json',
      'Accept'       : 'application/json',
      if (token != null && token.isNotEmpty) 'Authorization': 'Bearer $token',
    };
  }

  static Map<String, String> get _baseHeaders => {
    'Content-Type': 'application/json',
    'Accept'       : 'application/json',
  };

  static dynamic _safeJson(String body) {
    final trimmed = body.trim();
    if (trimmed.isEmpty) return null;
    try { return jsonDecode(trimmed); } catch (_) { return trimmed; }
  }

  static String _errorFrom(dynamic decoded, int statusCode) {
    if (decoded == null) return 'Server error ($statusCode). Is backend running at $baseUrl ?';
    if (decoded is String && decoded.isNotEmpty) return decoded;
    if (decoded is Map) {
      final msg = decoded['error']   ??
                  decoded['message'] ??
                  decoded['title']   ??
                  decoded['detail'];
      if (msg is String && msg.isNotEmpty) return msg;
      if (decoded['errors'] is Map) {
        final errs  = decoded['errors'] as Map;
        final first = errs.values.firstOrNull;
        if (first is List && first.isNotEmpty) return first.first.toString();
      }
    }
    return 'Request failed ($statusCode)';
  }

  static Future<Map<String, dynamic>> _post(
    String path,
    Map<String, dynamic> body, {
    bool withAuth = false,
  }) async {
    try {
      final headers  = withAuth ? await _authHeaders() : _baseHeaders;
      final response = await http
          .post(
            Uri.parse('$baseUrl$path'),
            headers: headers,
            body: jsonEncode(body),
          )
          .timeout(const Duration(seconds: 15));

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is Map<String, dynamic>) return decoded;
        return {'message': decoded?.toString() ?? 'OK'};
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server at $baseUrl\n\n'
            'Checklist:\n'
            '1. Backend is running (dotnet run)\n'
            '2. Phone & PC on same WiFi\n'
            '3. Firewall allows port 5181\n'
            '4. launchSettings.json uses 0.0.0.0:5181\n'
            '5. App launched via "Flutter: Android Device Debug" with your LAN IP';
    } on HttpException {
      throw 'Network error. Please try again.';
    }
  }

  static Future<Map<String, dynamic>> _get(String path) async {
    try {
      final headers  = await _authHeaders();
      final response = await http
          .get(Uri.parse('$baseUrl$path'), headers: headers)
          .timeout(const Duration(seconds: 15));

      if (response.statusCode == 401) throw 'SESSION_EXPIRED';

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is Map<String, dynamic>) return decoded;
        return {'data': decoded};
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server. Check connection.';
    }
  }

  static Future<void> saveSession(
    Map<String, dynamic> dealer,
    String token,
    String refreshToken,
  ) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kToken,        token);
    await prefs.setString(_kRefreshToken, refreshToken);
    await prefs.setString(_kDealerCode,   dealer['dealerCode']?.toString() ?? '');
    await prefs.setString(_kDealerName,   dealer['dealerName']?.toString() ?? '');
    await prefs.setInt   (_kDealerId,     (dealer['dealerId'] as num?)?.toInt() ?? 0);
    await prefs.setString(_kMobile,       dealer['mobile']?.toString() ?? '');
    await prefs.setString(_kCity,         dealer['city']?.toString() ?? '');
  }

  static Future<void> clearSession() async =>
      (await SharedPreferences.getInstance()).clear();

  static Future<bool> isLoggedIn() async {
    final t = await getToken();
    return t != null && t.isNotEmpty;
  }

  static Future<Map<String, String?>> getLocalDealerInfo() async {
    final p = await SharedPreferences.getInstance();
    return {
      'dealerCode': p.getString(_kDealerCode),
      'dealerName': p.getString(_kDealerName),
      'mobile'    : p.getString(_kMobile),
      'city'      : p.getString(_kCity),
    };
  }

  static Future<Map<String, dynamic>> sendOtp({
    required String mobile,
    required String dealerCode,
  }) =>
      _post('/api/DealerAuth/send-otp', {
        'mobileNumber': mobile.trim(),
        'dealerCode'  : dealerCode.trim().toUpperCase(),
      });

  static Future<Map<String, dynamic>> verifyOtp({
    required String mobile,
    required String dealerCode,
    required String otp,
  }) async {
    final data = await _post('/api/DealerAuth/verify-otp', {
      'mobileNumber': mobile.trim(),
      'dealerCode'  : dealerCode.trim().toUpperCase(),
      'otpCode'     : otp.trim(),
    });
    await saveSession(
      data['dealer']       as Map<String, dynamic>,
      data['token']        as String,
      data['refreshToken'] as String,
    );
    return data;
  }

  static Future<Map<String, dynamic>> getDashboardStats() =>
      _get('/api/DealerDashboard/stats');

  static Future<void> logout() async {
    try {
      final p       = await SharedPreferences.getInstance();
      final refresh = p.getString(_kRefreshToken);
      if (refresh != null && refresh.isNotEmpty) {
        await _post('/api/DealerAuth/logout', {'refreshToken': refresh});
      }
    } catch (_) {}
    await clearSession();
  }
}