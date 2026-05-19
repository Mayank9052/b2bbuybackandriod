// lib/services/api_service.dart
import 'dart:convert';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

class ApiService {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:5181',  // emulator default
  );

  // ── Storage keys ───────────────────────────────────────────
  static const _kToken        = 'dealer_token';
  static const _kRefreshToken = 'dealer_refresh_token';
  static const _kDealerCode   = 'dealer_code';
  static const _kDealerName   = 'dealer_name';
  static const _kDealerId     = 'dealer_id';
  static const _kMobile       = 'dealer_mobile';
  static const _kCity         = 'dealer_city';

  // ── Token helpers ───────────────────────────────────────────
  static Future<String?> getToken() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getString(_kToken);
  }

  static Future<Map<String, String>> authHeaders() async {
    final token = await getToken();
    return {
      'Content-Type': 'application/json',
      if (token != null) 'Authorization': 'Bearer $token',
    };
  }

  static Future<void> saveSession(Map<String, dynamic> dealer,
      String token, String refreshToken) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(_kToken, token);
    await prefs.setString(_kRefreshToken, refreshToken);
    await prefs.setString(_kDealerCode, dealer['dealerCode'] ?? '');
    await prefs.setString(_kDealerName, dealer['dealerName'] ?? '');
    await prefs.setInt(_kDealerId, dealer['dealerId'] ?? 0);
    await prefs.setString(_kMobile, dealer['mobile'] ?? '');
    await prefs.setString(_kCity, dealer['city'] ?? '');
  }

  static Future<void> clearSession() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.clear();
  }

  static Future<bool> isLoggedIn() async {
    final token = await getToken();
    return token != null && token.isNotEmpty;
  }

  static Future<Map<String, String?>> getLocalDealerInfo() async {
    final prefs = await SharedPreferences.getInstance();
    return {
      'dealerCode': prefs.getString(_kDealerCode),
      'dealerName': prefs.getString(_kDealerName),
      'mobile':     prefs.getString(_kMobile),
      'city':       prefs.getString(_kCity),
    };
  }

  // ══════════════════════════════════════════════════════════
  // AUTH — Send OTP
  // ══════════════════════════════════════════════════════════
  static Future<Map<String, dynamic>> sendOtp({
    required String mobile,
    required String dealerCode,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/DealerAuth/send-otp'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'mobileNumber': mobile,
        'dealerCode':   dealerCode.toUpperCase(),
      }),
    );

    final data = jsonDecode(res.body);
    if (res.statusCode == 200) return {'success': true, ...data};
    throw data['error'] ?? 'Failed to send OTP';
  }

  // ══════════════════════════════════════════════════════════
  // AUTH — Verify OTP
  // ══════════════════════════════════════════════════════════
  static Future<Map<String, dynamic>> verifyOtp({
    required String mobile,
    required String dealerCode,
    required String otp,
  }) async {
    final res = await http.post(
      Uri.parse('$baseUrl/api/DealerAuth/verify-otp'),
      headers: {'Content-Type': 'application/json'},
      body: jsonEncode({
        'mobileNumber': mobile,
        'dealerCode':   dealerCode.toUpperCase(),
        'otpCode':      otp,
      }),
    );

    final data = jsonDecode(res.body);
    if (res.statusCode == 200) {
      await saveSession(data['dealer'], data['token'], data['refreshToken']);
      return data;
    }
    throw data['error'] ?? 'Invalid OTP';
  }

  // ══════════════════════════════════════════════════════════
  // DASHBOARD — Stats
  // ══════════════════════════════════════════════════════════
  static Future<Map<String, dynamic>> getDashboardStats() async {
    final headers = await authHeaders();
    final res = await http.get(
      Uri.parse('$baseUrl/api/DealerDashboard/stats'),
      headers: headers,
    );

    if (res.statusCode == 200) return jsonDecode(res.body);
    if (res.statusCode == 401) throw 'SESSION_EXPIRED';
    throw 'Failed to load dashboard';
  }

  // ══════════════════════════════════════════════════════════
  // LOGOUT
  // ══════════════════════════════════════════════════════════
  static Future<void> logout() async {
    try {
      final prefs   = await SharedPreferences.getInstance();
      final refresh = prefs.getString(_kRefreshToken);
      if (refresh != null) {
        await http.post(
          Uri.parse('$baseUrl/api/DealerAuth/logout'),
          headers: {'Content-Type': 'application/json'},
          body: jsonEncode({'refreshToken': refresh}),
        );
      }
    } catch (_) {}
    await clearSession();
  }
}