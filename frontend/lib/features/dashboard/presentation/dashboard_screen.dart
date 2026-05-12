import 'package:b2b_buyback/core/config/app_config.dart';
import 'package:b2b_buyback/features/dashboard/data/dashboard_api.dart';
import 'package:b2b_buyback/features/dashboard/models/dashboard_models.dart';
import 'package:flutter/material.dart';

class DashboardScreen extends StatefulWidget {
  DashboardScreen({super.key, DashboardApi? api})
    : _api = api ?? DashboardApi();

  final DashboardApi _api;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  late Future<DashboardData> _dashboardFuture;

  @override
  void initState() {
    super.initState();
    _dashboardFuture = widget._api.fetchDashboardData();
  }

  Future<void> _refresh() async {
    final future = widget._api.fetchDashboardData();
    setState(() {
      _dashboardFuture = future;
    });

    try {
      await future;
    } catch (_) {
      // The screen already renders a fallback message when sync fails.
    }
  }

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Scaffold(
      appBar: AppBar(title: const Text(AppConfig.appName)),
      body: SafeArea(
        child: FutureBuilder<DashboardData>(
          future: _dashboardFuture,
          builder: (context, snapshot) {
            final data = snapshot.data ?? DashboardData.seed();
            final isLoading =
                snapshot.connectionState == ConnectionState.waiting;
            final hasLiveData = snapshot.hasData && !snapshot.hasError;
            final statusMessage = snapshot.hasError
                ? '${snapshot.error} Showing starter data until the API is reachable.'
                : hasLiveData
                ? 'Connected to ${AppConfig.apiBaseUrl}'
                : 'Syncing with ${AppConfig.apiBaseUrl}';

            return RefreshIndicator(
              onRefresh: _refresh,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.fromLTRB(20, 16, 20, 32),
                children: [
                  _HeroCard(
                    summary: data.summary,
                    statusMessage: statusMessage,
                    isLoading: isLoading,
                    isLive: hasLiveData,
                    onRefresh: _refresh,
                  ),
                  const SizedBox(height: 20),
                  Text(
                    'Operations snapshot',
                    style: theme.textTheme.titleLarge,
                  ),
                  const SizedBox(height: 12),
                  GridView.count(
                    crossAxisCount: 2,
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    crossAxisSpacing: 12,
                    mainAxisSpacing: 12,
                    childAspectRatio: 1.2,
                    children: [
                      _StatCard(
                        label: 'Pending requests',
                        value: data.summary.pendingRequests.toString(),
                        accent: const Color(0xFF1F6B52),
                      ),
                      _StatCard(
                        label: 'Inspections',
                        value: data.summary.inspectionsScheduled.toString(),
                        accent: const Color(0xFFD97706),
                      ),
                      _StatCard(
                        label: 'Quoted devices',
                        value: data.summary.devicesQuoted.toString(),
                        accent: const Color(0xFF0F766E),
                      ),
                      _StatCard(
                        label: 'Payouts due',
                        value: data.summary.payoutsDue.toString(),
                        accent: const Color(0xFF7C3AED),
                      ),
                    ],
                  ),
                  const SizedBox(height: 24),
                  Text('Workflow anchors', style: theme.textTheme.titleLarge),
                  const SizedBox(height: 12),
                  const _WorkflowCard(),
                  const SizedBox(height: 24),
                  Text('Active requests', style: theme.textTheme.titleLarge),
                  const SizedBox(height: 12),
                  ...data.requests.map(
                    (request) => _RequestCard(request: request),
                  ),
                ],
              ),
            );
          },
        ),
      ),
    );
  }
}

class _HeroCard extends StatelessWidget {
  const _HeroCard({
    required this.summary,
    required this.statusMessage,
    required this.isLoading,
    required this.isLive,
    required this.onRefresh,
  });

  final DashboardSummary summary;
  final String statusMessage;
  final bool isLoading;
  final bool isLive;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      decoration: BoxDecoration(
        borderRadius: BorderRadius.circular(28),
        gradient: const LinearGradient(
          colors: [Color(0xFF123524), Color(0xFF1F6B52)],
          begin: Alignment.topLeft,
          end: Alignment.bottomRight,
        ),
      ),
      padding: const EdgeInsets.all(20),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.14),
              borderRadius: BorderRadius.circular(999),
            ),
            child: Text(
              isLive ? 'LIVE BACKEND' : 'STARTER MODE',
              style: theme.textTheme.labelMedium?.copyWith(
                color: Colors.white,
                letterSpacing: 1.1,
              ),
            ),
          ),
          const SizedBox(height: 16),
          Text(
            'Live buyback pipeline',
            style: theme.textTheme.headlineMedium?.copyWith(
              color: Colors.white,
            ),
          ),
          const SizedBox(height: 10),
          Text(
            'Track partner pickups, quote velocity, and payout readiness from a single Android workspace.',
            style: theme.textTheme.bodyLarge?.copyWith(
              color: const Color(0xFFE7F2EB),
            ),
          ),
          const SizedBox(height: 18),
          Wrap(
            spacing: 12,
            runSpacing: 12,
            children: [
              _HeroMetric(
                label: 'Quoted value',
                value: _currency(summary.totalQuotedValue),
              ),
              _HeroMetric(
                label: 'Last sync',
                value: _formatTime(summary.lastUpdatedUtc),
              ),
            ],
          ),
          const SizedBox(height: 18),
          Text(
            statusMessage,
            style: theme.textTheme.bodyMedium?.copyWith(
              color: Colors.white.withValues(alpha: 0.88),
            ),
          ),
          const SizedBox(height: 14),
          Row(
            children: [
              Expanded(
                child: OutlinedButton.icon(
                  onPressed: onRefresh,
                  icon: const Icon(Icons.sync),
                  label: const Text('Refresh data'),
                  style: OutlinedButton.styleFrom(
                    foregroundColor: Colors.white,
                    side: const BorderSide(color: Color(0x66FFFFFF)),
                  ),
                ),
              ),
            ],
          ),
          if (isLoading) ...[
            const SizedBox(height: 12),
            ClipRRect(
              borderRadius: BorderRadius.circular(999),
              child: const LinearProgressIndicator(
                minHeight: 4,
                color: Colors.white,
                backgroundColor: Color(0x33FFFFFF),
              ),
            ),
          ],
        ],
      ),
    );
  }
}

class _HeroMetric extends StatelessWidget {
  const _HeroMetric({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      width: 148,
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: Colors.white.withValues(alpha: 0.12),
        borderRadius: BorderRadius.circular(20),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: theme.textTheme.labelMedium?.copyWith(
              color: const Color(0xFFD1E7DA),
            ),
          ),
          const SizedBox(height: 6),
          Text(
            value,
            style: theme.textTheme.titleLarge?.copyWith(color: Colors.white),
          ),
        ],
      ),
    );
  }
}

class _StatCard extends StatelessWidget {
  const _StatCard({
    required this.label,
    required this.value,
    required this.accent,
  });

  final String label;
  final String value;
  final Color accent;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      elevation: 0,
      color: Colors.white,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              width: 12,
              height: 12,
              decoration: BoxDecoration(
                color: accent,
                borderRadius: BorderRadius.circular(999),
              ),
            ),
            const Spacer(),
            Text(
              value,
              style: theme.textTheme.headlineMedium?.copyWith(fontSize: 28),
            ),
            const SizedBox(height: 4),
            Text(label, style: theme.textTheme.bodyMedium),
          ],
        ),
      ),
    );
  }
}

class _WorkflowCard extends StatelessWidget {
  const _WorkflowCard();

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      elevation: 0,
      color: const Color(0xFFFFFBF5),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            _WorkflowRow(
              index: '01',
              title: 'Intake requests',
              subtitle:
                  'Capture partner, device model, quantity, and pickup window.',
              color: const Color(0xFF1F6B52),
            ),
            const SizedBox(height: 16),
            _WorkflowRow(
              index: '02',
              title: 'Grade inventory',
              subtitle:
                  'Schedule inspections and keep quote values visible to the team.',
              color: const Color(0xFFD97706),
            ),
            const SizedBox(height: 16),
            _WorkflowRow(
              index: '03',
              title: 'Release payout',
              subtitle:
                  'Move approved devices into payout-ready status without losing traceability.',
              color: const Color(0xFF0F766E),
            ),
            const SizedBox(height: 14),
            Align(
              alignment: Alignment.centerLeft,
              child: Text(
                'Pull to refresh anytime after the backend is running.',
                style: theme.textTheme.bodyMedium,
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _WorkflowRow extends StatelessWidget {
  const _WorkflowRow({
    required this.index,
    required this.title,
    required this.subtitle,
    required this.color,
  });

  final String index;
  final String title;
  final String subtitle;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          width: 42,
          height: 42,
          alignment: Alignment.center,
          decoration: BoxDecoration(
            color: color.withValues(alpha: 0.14),
            borderRadius: BorderRadius.circular(14),
          ),
          child: Text(
            index,
            style: theme.textTheme.labelMedium?.copyWith(color: color),
          ),
        ),
        const SizedBox(width: 14),
        Expanded(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(title, style: theme.textTheme.titleMedium),
              const SizedBox(height: 4),
              Text(subtitle, style: theme.textTheme.bodyMedium),
            ],
          ),
        ),
      ],
    );
  }
}

class _RequestCard extends StatelessWidget {
  const _RequestCard({required this.request});

  final BuybackRequest request;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      elevation: 0,
      color: Colors.white,
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(24)),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        request.vendorName,
                        style: theme.textTheme.titleMedium,
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${request.deviceModel}  |  ${request.id}',
                        style: theme.textTheme.bodyMedium,
                      ),
                    ],
                  ),
                ),
                Chip(
                  label: Text(request.status),
                  avatar: const Icon(Icons.inventory_2_outlined, size: 18),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Row(
              children: [
                Expanded(
                  child: _DetailPill(
                    label: 'Quantity',
                    value: '${request.quantity} units',
                  ),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: _DetailPill(
                    label: 'Quoted amount',
                    value: _currency(request.quotedAmount),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            _DetailPill(
              label: 'Pickup window',
              value: _formatDate(request.pickupWindowUtc),
            ),
          ],
        ),
      ),
    );
  }
}

class _DetailPill extends StatelessWidget {
  const _DetailPill({required this.label, required this.value});

  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    final theme = Theme.of(context);

    return Container(
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFFF7F1E7),
        borderRadius: BorderRadius.circular(18),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(label, style: theme.textTheme.labelMedium),
          const SizedBox(height: 4),
          Text(
            value,
            style: theme.textTheme.titleMedium?.copyWith(fontSize: 15),
          ),
        ],
      ),
    );
  }
}

String _currency(double value) => '\$${value.toStringAsFixed(0)}';

String _formatTime(DateTime value) {
  final local = value.toLocal();
  final hour = local.hour % 12 == 0 ? 12 : local.hour % 12;
  final minute = local.minute.toString().padLeft(2, '0');
  final suffix = local.hour >= 12 ? 'PM' : 'AM';
  return '$hour:$minute $suffix';
}

String _formatDate(DateTime value) {
  const months = [
    'Jan',
    'Feb',
    'Mar',
    'Apr',
    'May',
    'Jun',
    'Jul',
    'Aug',
    'Sep',
    'Oct',
    'Nov',
    'Dec',
  ];

  final local = value.toLocal();
  return '${months[local.month - 1]} ${local.day}, ${local.year}';
}
