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

  // ── Resolve image URL (backend returns /ExchangeImages/... relative paths) ─
  static String resolveImageUrl(String? path) {
    if (path == null || path.isEmpty) return '';
    if (path.startsWith('http://') || path.startsWith('https://')) return path;
    final clean = path.startsWith('/') ? path : '/$path';
    return '$baseUrl$clean';
  }

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
            '4. launchSettings.json uses 0.0.0.0:5181';
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

  static Future<List<Map<String, dynamic>>> _getList(String path) async {
    try {
      final headers  = await _authHeaders();
      final response = await http
          .get(Uri.parse('$baseUrl$path'), headers: headers)
          .timeout(const Duration(seconds: 15));

      if (response.statusCode == 401) throw 'SESSION_EXPIRED';

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is List) return List<Map<String, dynamic>>.from(decoded);
        return [];
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server. Check connection.';
    }
  }

  static Future<Map<String, dynamic>> _patch(
    String path,
    Map<String, dynamic> body,
  ) async {
    try {
      final headers  = await _authHeaders();
      final response = await http
          .patch(
            Uri.parse('$baseUrl$path'),
            headers: headers,
            body: jsonEncode(body),
          )
          .timeout(const Duration(seconds: 15));

      if (response.statusCode == 401) throw 'SESSION_EXPIRED';

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is Map<String, dynamic>) return decoded;
        return {'message': decoded?.toString() ?? 'OK'};
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server. Check connection.';
    }
  }

  // ── Session ───────────────────────────────────────────────

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

  // ── Auth ──────────────────────────────────────────────────

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

  // ── Dashboard ─────────────────────────────────────────────

  static Future<Map<String, dynamic>> getDashboardStats() =>
      _get('/api/DealerDashboard/stats');

  // ── Vehicles / Inventory ──────────────────────────────────

  static Future<List<Map<String, dynamic>>> getVehicles({
    String? pincode,
    int? cityId,
  }) async {
    var path = '/api/ScootyInventory/models-list';
    if (pincode != null) path += '?pincode=$pincode';
    else if (cityId != null) path += '?cityId=$cityId';
    final list = await _getList(path);
    // Resolve image URLs so dashboard grid shows real images
    return list.map((v) {
      final raw = v['imageUrl'] ?? v['imagePath'];
      return {
        ...v,
        'imageUrl': resolveImageUrl(raw?.toString()),
      };
    }).toList();
  }

  static Future<Map<String, dynamic>> getVehicleDetail(
    int id, {
    String? pincode,
    int? cityId,
  }) async {
    // ← FIXED: was /vehicle/$id, backend endpoint is /details/$id
    var path = '/api/ScootyInventory/details/$id';
    if (pincode != null) path += '?pincode=$pincode';
    else if (cityId != null) path += '?cityId=$cityId';
    final v = await _get(path);
    final raw = v['imageUrl'] ?? v['imagePath'];
    return {
      ...v,
      'imageUrl': resolveImageUrl(raw?.toString()),
    };
  }

  static Future<void> addInventoryItem(Map<String, dynamic> data) async {
    await _post('/api/ScootyInventory/add-item', data, withAuth: true);
  }

  // ── Models / Variants / Colours ───────────────────────────

  static Future<List<Map<String, dynamic>>> getModels() =>
      _getList('/api/ScootyInventory/models');

  static Future<List<Map<String, dynamic>>> getVariants(int modelId) =>
      _getList('/api/ScootyInventory/variants/$modelId');

  static Future<List<Map<String, dynamic>>> getColours(
    int modelId,
    int variantId,
  ) =>
      _getList(
        '/api/ScootyInventory/colours?modelId=$modelId&variantId=$variantId',
      );

  // ── Pincode search ────────────────────────────────────────

  static Future<List<Map<String, dynamic>>> searchPincode(String q) =>
      _getList('/api/City/search?q=${Uri.encodeComponent(q)}');

  // ── Exchange / Inspection ─────────────────────────────────

  static Future<List<Map<String, dynamic>>> getInspectionParams() =>
      _getList('/api/ExchangeCases/inspection-params');

  static Future<Map<String, dynamic>> startCase(
    Map<String, dynamic> body,
  ) =>
      _post('/api/ExchangeCases/start', body, withAuth: true);
  
  // Returns the FULL inventory list (all variants, not just one per model)
  // Used by vehicle detail screen to show all variants of the same model
  static Future<List<Map<String, dynamic>>> getAllInventory() async {
    final list = await _getList('/api/ScootyInventory');
    return list.map((v) {
      final raw = v['imageUrl'] ?? v['imagePath'] ?? v['ImageUrl'];
      return {
        ...v,
        // Normalise casing — backend returns PascalCase from this endpoint
        'scootyId':      v['scootyId']      ?? v['ScootyId'],
        'modelId':       v['modelId']       ?? v['ModelId'],
        'modelName':     v['modelName']     ?? v['ModelName'],
        'variantId':     v['variantId']     ?? v['VariantId'],
        'variantName':   v['variantName']   ?? v['VariantName'],
        'price':         v['price']         ?? v['Price'],
        'stockAvailable':v['stockAvailable']?? v['StockAvailable'] ?? true,
        'stockQuantity': v['stockQuantity'] ?? v['StockQuantity']  ?? 0,
        'rangeKm':       v['rangeKm']       ?? v['RangeKm'],
        'imageUrl':      resolveImageUrl(raw?.toString()),
      };
    }).toList();
  }

  static Future<List<Map<String, dynamic>>> getDocuments(int caseId) =>
    _getList('/api/ExchangeCases/$caseId/documents');

  static Future<Map<String, dynamic>> saveScores(
    int caseId,
    List<Map<String, dynamic>> scores,
  ) async {
    try {
      final headers = await _authHeaders();
      final response = await http
          .post(
            Uri.parse('$baseUrl/api/ExchangeCases/$caseId/scores'),
            headers: headers,
            body: jsonEncode(scores), // ← send list directly, NOT {'scores': scores}
          )
          .timeout(const Duration(seconds: 15));

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is Map<String, dynamic>) return decoded;
        return {'totalScore': 5.0, 'grade': 'Good'};
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server. Check connection.';
    }
  }

  static Future<Map<String, dynamic>> generatePrice(int caseId) =>
      _post('/api/ExchangeCases/$caseId/generate-price', {}, withAuth: true);

  static Future<void> submitCase(int caseId) =>
      _post('/api/ExchangeCases/$caseId/submit', {}, withAuth: true);

  static Future<Map<String, dynamic>> getCaseDetail(int caseId) =>
      _get('/api/ExchangeCases/$caseId');

  static Future<void> confirmExchangeDone(int caseId) =>
      _post('/api/ExchangeCases/$caseId/confirm-exchange', {}, withAuth: true);

  // ── Refurbishment ─────────────────────────────────────────

  static Future<Map<String, dynamic>> getRefurbishmentDetail(int caseId) =>
      _get('/api/ExchangeCases/$caseId/refurbishment');

  static Future<void> updateRefurbItem(
    int caseId,
    String component,
    bool done,
  ) =>
      _patch('/api/ExchangeCases/$caseId/refurbishment', {
        'component': component,
        'done'     : done,
      });

  static Future<void> submitForBgService(int caseId, double totalCost) =>
      _post('/api/ExchangeCases/$caseId/submit-bg-service', {
        'totalCost': totalCost,
      }, withAuth: true);

  // ── File uploads ──────────────────────────────────────────

  static Future<void> uploadDocument(
    int caseId,
    String docType,
    List<int> fileBytes,
    String fileName,
  ) async {
    final token = await getToken();
 
    final req = http.MultipartRequest(
      'POST',
      Uri.parse('$baseUrl/api/ExchangeCases/$caseId/documents'),
    );
 
    if (token != null && token.isNotEmpty) {
      req.headers['Authorization'] = 'Bearer $token';
    }
 
    // Field names MUST match controller params exactly
    req.fields['documentType'] = docType;
    req.files.add(
      http.MultipartFile.fromBytes(
        'document',   // ← matches IFormFile document in controller
        fileBytes,
        filename: fileName,
      ),
    );
 
    final streamed = await req.send().timeout(const Duration(seconds: 30));
    final respBody = await streamed.stream.bytesToString();
 
    if (streamed.statusCode < 200 || streamed.statusCode >= 300) {
      String errorMsg = 'Upload failed (${streamed.statusCode})';
      try {
        final decoded = jsonDecode(respBody);
        if (decoded is Map && decoded['error'] != null) {
          errorMsg = decoded['error'].toString();
        }
      } catch (_) {
        if (respBody.isNotEmpty) errorMsg = respBody;
      }
      throw errorMsg;
    }
  }

  static Future<void> uploadCaseImage(
    int caseId,
    String imageType,
    List<int> bytes,
    String fileName,
  ) async {
    final token = await getToken();
    final req   = http.MultipartRequest(
      'POST',
      Uri.parse('$baseUrl/api/ExchangeCases/$caseId/images'),
    );
    if (token != null && token.isNotEmpty) {
      req.headers['Authorization'] = 'Bearer $token';
    }
    req.fields['imageType'] = imageType;
    req.files.add(
      http.MultipartFile.fromBytes('image', bytes, filename: fileName),
    );
    final streamed = await req.send();
    if (streamed.statusCode != 200) throw Exception('Image upload failed');
  }

  // ── Likes ─────────────────────────────────────────────────

  static Future<Map<String, dynamic>> getLikeStatus(
    int scootyId,
    String userId,
  ) =>
      _get('/api/UserLikes/$scootyId?userId=${Uri.encodeComponent(userId)}');

  static Future<Map<String, dynamic>> toggleLike(
    int scootyId,
    String userId,
  ) async {
    try {
      final headers = await _authHeaders();
      final response = await http
          .post(
            Uri.parse(
                '$baseUrl/api/UserLikes/$scootyId?userId=${Uri.encodeComponent(userId)}'),
            headers: headers,
          )
          .timeout(const Duration(seconds: 15));

      final decoded = _safeJson(response.body);
      if (response.statusCode >= 200 && response.statusCode < 300) {
        if (decoded is Map<String, dynamic>) return decoded;
        return {'liked': false, 'count': 0};
      }
      throw _errorFrom(decoded, response.statusCode);
    } on SocketException {
      throw 'Cannot reach server. Check connection.';
    }
  }

  // ── Reviews ───────────────────────────────────────────────

  static Future<Map<String, dynamic>> getReviewSummary(int scootyId) =>
      _get('/api/VehicleReviews/$scootyId');

  static Future<Map<String, dynamic>> submitReview(
    Map<String, dynamic> body,
  ) =>
      _post('/api/VehicleReviews', body, withAuth: true);

  // ── EMI Enquiry ───────────────────────────────────────────

  static Future<Map<String, dynamic>> submitEmiEnquiry(
    Map<String, dynamic> body,
  ) =>
      _post('/api/EmiEnquiry', body, withAuth: true);
}