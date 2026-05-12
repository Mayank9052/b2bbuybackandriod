class DashboardSummary {
  const DashboardSummary({
    required this.pendingRequests,
    required this.inspectionsScheduled,
    required this.devicesQuoted,
    required this.payoutsDue,
    required this.totalQuotedValue,
    required this.lastUpdatedUtc,
  });

  final int pendingRequests;
  final int inspectionsScheduled;
  final int devicesQuoted;
  final int payoutsDue;
  final double totalQuotedValue;
  final DateTime lastUpdatedUtc;

  factory DashboardSummary.fromJson(Map<String, dynamic> json) {
    return DashboardSummary(
      pendingRequests: _readInt(json['pendingRequests']),
      inspectionsScheduled: _readInt(json['inspectionsScheduled']),
      devicesQuoted: _readInt(json['devicesQuoted']),
      payoutsDue: _readInt(json['payoutsDue']),
      totalQuotedValue: _readDouble(json['totalQuotedValue']),
      lastUpdatedUtc: _readDateTime(json['lastUpdatedUtc']),
    );
  }
}

class BuybackRequest {
  const BuybackRequest({
    required this.id,
    required this.vendorName,
    required this.deviceModel,
    required this.status,
    required this.quantity,
    required this.quotedAmount,
    required this.pickupWindowUtc,
  });

  final String id;
  final String vendorName;
  final String deviceModel;
  final String status;
  final int quantity;
  final double quotedAmount;
  final DateTime pickupWindowUtc;

  factory BuybackRequest.fromJson(Map<String, dynamic> json) {
    return BuybackRequest(
      id: json['id'] as String? ?? '',
      vendorName: json['vendorName'] as String? ?? '',
      deviceModel: json['deviceModel'] as String? ?? '',
      status: json['status'] as String? ?? '',
      quantity: _readInt(json['quantity']),
      quotedAmount: _readDouble(json['quotedAmount']),
      pickupWindowUtc: _readDateTime(json['pickupWindowUtc']),
    );
  }
}

class DashboardData {
  const DashboardData({required this.summary, required this.requests});

  final DashboardSummary summary;
  final List<BuybackRequest> requests;

  factory DashboardData.seed() {
    return DashboardData(
      summary: DashboardSummary(
        pendingRequests: 4,
        inspectionsScheduled: 1,
        devicesQuoted: 10,
        payoutsDue: 1,
        totalQuotedValue: 28450,
        lastUpdatedUtc: DateTime.now().toUtc(),
      ),
      requests: [
        BuybackRequest(
          id: 'BB-2401',
          vendorName: 'Orbit Devices',
          deviceModel: 'iPhone 13 Pro',
          status: 'Awaiting Pickup',
          quantity: 12,
          quotedAmount: 8200,
          pickupWindowUtc: DateTime.now().toUtc().add(const Duration(days: 1)),
        ),
        BuybackRequest(
          id: 'BB-2402',
          vendorName: 'Nova Trade',
          deviceModel: 'Samsung S22',
          status: 'Inspection Scheduled',
          quantity: 7,
          quotedAmount: 4100,
          pickupWindowUtc: DateTime.now().toUtc().add(const Duration(days: 2)),
        ),
        BuybackRequest(
          id: 'BB-2403',
          vendorName: 'Green Loop',
          deviceModel: 'MacBook Air M1',
          status: 'Quoted',
          quantity: 4,
          quotedAmount: 9600,
          pickupWindowUtc: DateTime.now().toUtc().add(const Duration(days: 3)),
        ),
      ],
    );
  }
}

int _readInt(dynamic value) {
  if (value is int) {
    return value;
  }

  if (value is num) {
    return value.toInt();
  }

  return int.tryParse(value?.toString() ?? '') ?? 0;
}

double _readDouble(dynamic value) {
  if (value is double) {
    return value;
  }

  if (value is num) {
    return value.toDouble();
  }

  return double.tryParse(value?.toString() ?? '') ?? 0;
}

DateTime _readDateTime(dynamic value) {
  final parsed = DateTime.tryParse(value?.toString() ?? '');
  return parsed?.toUtc() ?? DateTime.now().toUtc();
}
