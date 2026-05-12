import 'dart:convert';
import 'dart:io';

import 'package:b2b_buyback/core/config/app_config.dart';
import 'package:b2b_buyback/features/dashboard/models/dashboard_models.dart';

class DashboardApi {
  Future<DashboardData> fetchDashboardData() async {
    final summaryJson = await _getJsonObject('/dashboard/summary');
    final requestsJson = await _getJsonList('/buybacks');

    return DashboardData(
      summary: DashboardSummary.fromJson(summaryJson),
      requests: requestsJson
          .map((item) => BuybackRequest.fromJson(item as Map<String, dynamic>))
          .toList(growable: false),
    );
  }

  Future<Map<String, dynamic>> _getJsonObject(String path) async {
    final payload = await _getJson(path);
    if (payload is Map<String, dynamic>) {
      return payload;
    }

    throw DashboardApiException('Unexpected response for $path.');
  }

  Future<List<dynamic>> _getJsonList(String path) async {
    final payload = await _getJson(path);
    if (payload is List<dynamic>) {
      return payload;
    }

    throw DashboardApiException('Unexpected response for $path.');
  }

  Future<dynamic> _getJson(String path) async {
    final client = HttpClient()..connectionTimeout = const Duration(seconds: 5);

    try {
      final request = await client.getUrl(
        Uri.parse('${AppConfig.apiBaseUrl}$path'),
      );
      request.headers.set(HttpHeaders.acceptHeader, ContentType.json.mimeType);

      final response = await request.close();
      final payload = await response.transform(utf8.decoder).join();

      if (response.statusCode < 200 || response.statusCode > 299) {
        throw DashboardApiException(
          'API returned ${response.statusCode} for $path.',
        );
      }

      return jsonDecode(payload);
    } on SocketException {
      throw const DashboardApiException(
        'Cannot reach the backend. Start the .NET API or override API_BASE_URL.',
      );
    } on HandshakeException {
      throw const DashboardApiException(
        'TLS handshake failed. Use the HTTP dev URL or enable HTTPS for the backend.',
      );
    } on HttpException {
      throw const DashboardApiException(
        'The backend connection closed unexpectedly.',
      );
    } on FormatException {
      throw const DashboardApiException('Backend response was not valid JSON.');
    } finally {
      client.close(force: true);
    }
  }
}

class DashboardApiException implements Exception {
  const DashboardApiException(this.message);

  final String message;

  @override
  String toString() => message;
}
