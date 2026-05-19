// lib/screens/dashboard_screen.dart
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  int _navIndex = 0;

  Map<String, dynamic>? _stats;
  Map<String, String?>  _dealer = {};
  bool  _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = null; });
    try {
      final results = await Future.wait([
        ApiService.getDashboardStats(),
        ApiService.getLocalDealerInfo(),
      ]);
      setState(() {
        _stats  = results[0] as Map<String, dynamic>;
        _dealer = results[1] as Map<String, String?>;
      });
    } catch (e) {
      if (e.toString() == 'SESSION_EXPIRED') {
        await ApiService.clearSession();
        if (mounted) Navigator.pushReplacementNamed(context, '/login');
        return;
      }
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _logout() async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (_) => AlertDialog(
        title: const Text('Logout'),
        content: const Text('Are you sure you want to logout?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Cancel'),
          ),
          TextButton(
            onPressed: () => Navigator.pop(context, true),
            style: TextButton.styleFrom(foregroundColor: AppColors.red),
            child: const Text('Logout'),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await ApiService.logout();
      if (mounted) Navigator.pushReplacementNamed(context, '/login');
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.navy,
      body: SafeArea(
        bottom: false,
        child: Column(
          children: [
            // ── Top navbar ──────────────────────────────────────
            _buildNavBar(),

            // ── Body (white rounded card) ───────────────────────
            Expanded(
              child: Container(
                decoration: const BoxDecoration(
                  color: AppColors.gray1,
                  borderRadius: BorderRadius.only(
                    topLeft:  Radius.circular(24),
                    topRight: Radius.circular(24),
                  ),
                ),
                child: _navIndex == 0
                    ? _buildHomeTab()
                    : _navIndex == 1
                        ? _buildInventoryTab()
                        : _buildProfileTab(),
              ),
            ),
          ],
        ),
      ),
      bottomNavigationBar: _buildBottomNav(),
      floatingActionButton: _navIndex == 0
          ? FloatingActionButton.extended(
              onPressed: () {
                // TODO: Navigate to new inspection flow
                ScaffoldMessenger.of(context).showSnackBar(
                  const SnackBar(
                    content: Text('New Inspection flow — coming next sprint!'),
                  ),
                );
              },
              backgroundColor: AppColors.gold,
              foregroundColor: AppColors.navy,
              icon: const Icon(Icons.add),
              label: const Text(
                'START NEW INSPECTION',
                style: TextStyle(fontWeight: FontWeight.w800, fontSize: 12),
              ),
            )
          : null,
      floatingActionButtonLocation: FloatingActionButtonLocation.centerFloat,
    );
  }

  // ── Navbar ─────────────────────────────────────────────────
  Widget _buildNavBar() {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          // Logo
          Container(
            width: 40, height: 40,
            decoration: const BoxDecoration(
              color: AppColors.gold, shape: BoxShape.circle,
            ),
            alignment: Alignment.center,
            child: const Text(
              'BG',
              style: TextStyle(
                fontSize: 14, fontWeight: FontWeight.w900, color: AppColors.navy,
              ),
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'Welcome back,',
                  style: TextStyle(
                    fontSize: 11, color: Colors.white.withOpacity(0.55),
                  ),
                ),
                Text(
                  _dealer['dealerName'] ?? 'Dealer',
                  style: const TextStyle(
                    fontSize: 15, fontWeight: FontWeight.w900, color: Colors.white,
                  ),
                ),
                Text(
                  'Dealer ID: ${_dealer['dealerCode'] ?? '—'}',
                  style: TextStyle(
                    fontSize: 11, color: Colors.white.withOpacity(0.5),
                  ),
                ),
              ],
            ),
          ),
          // Notification bell
          Stack(
            children: [
              IconButton(
                icon: const Icon(Icons.notifications_outlined,
                    color: Colors.white, size: 24),
                onPressed: () {},
              ),
              if ((_stats?['pendingApproval'] ?? 0) > 0)
                Positioned(
                  top: 8, right: 8,
                  child: Container(
                    width: 9, height: 9,
                    decoration: const BoxDecoration(
                      color: AppColors.amber, shape: BoxShape.circle,
                    ),
                  ),
                ),
            ],
          ),
          IconButton(
            icon: const Icon(Icons.logout, color: Colors.white, size: 22),
            onPressed: _logout,
          ),
        ],
      ),
    );
  }

  // ── Home tab ────────────────────────────────────────────────
  Widget _buildHomeTab() {
    return RefreshIndicator(
      onRefresh: _load,
      color: AppColors.gold,
      child: _loading
          ? const Center(child: CircularProgressIndicator(color: AppColors.gold))
          : _error != null
              ? _buildError()
              : ListView(
                  padding: const EdgeInsets.only(
                      left: 16, right: 16, top: 20, bottom: 120),
                  children: [
                    // KPI cards
                    _buildKpiRow(),
                    const SizedBox(height: 24),

                    // My Vehicles header
                    Row(
                      children: [
                        const Text(
                          'My Vehicles',
                          style: TextStyle(
                            fontSize: 16, fontWeight: FontWeight.w800,
                            color: AppColors.gray7,
                          ),
                        ),
                        const Spacer(),
                        TextButton(
                          onPressed: () => setState(() => _navIndex = 1),
                          child: const Text(
                            'View All →',
                            style: TextStyle(
                              fontSize: 12, color: AppColors.blue,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),

                    // Vehicle list
                    ...(_stats?['recentVehicles'] as List<dynamic>? ?? [])
                        .map((v) => _VehicleCard(v))
                        .toList(),

                    if ((_stats?['recentVehicles'] as List<dynamic>? ?? []).isEmpty)
                      Container(
                        padding: const EdgeInsets.all(32),
                        alignment: Alignment.center,
                        child: Column(
                          children: [
                            Icon(Icons.electric_scooter,
                                size: 48, color: AppColors.gray3),
                            const SizedBox(height: 12),
                            const Text(
                              'No vehicles yet',
                              style: TextStyle(
                                fontSize: 15, fontWeight: FontWeight.w700,
                                color: AppColors.gray5,
                              ),
                            ),
                            const SizedBox(height: 4),
                            const Text(
                              'Tap "Start New Inspection" to begin',
                              style: TextStyle(
                                fontSize: 12, color: AppColors.gray4,
                              ),
                            ),
                          ],
                        ),
                      ),
                  ],
                ),
    );
  }

  Widget _buildKpiRow() {
    final cards = [
      _KpiData(
        label: 'Procured\nThis Month',
        value: '${_stats?['procuredThisMonth'] ?? 0}',
        color: AppColors.blue,
        icon:  Icons.check_circle_outline,
      ),
      _KpiData(
        label: 'Pending\nApproval',
        value: '${_stats?['pendingApproval'] ?? 0}',
        color: AppColors.amber,
        icon:  Icons.hourglass_top_outlined,
      ),
      _KpiData(
        label: 'Sold &\nSettled',
        value: '${_stats?['soldAndSettled'] ?? 0}',
        color: AppColors.green,
        icon:  Icons.monetization_on_outlined,
      ),
    ];

    return Row(
      children: cards
          .map((d) => Expanded(child: _KpiCard(data: d)))
          .toList(),
    );
  }

  Widget _buildInventoryTab() {
    return const Center(
      child: Text('Inventory — coming soon',
          style: TextStyle(color: AppColors.gray5)),
    );
  }

  Widget _buildProfileTab() {
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const SizedBox(height: 16),
        Center(
          child: Container(
            width: 72, height: 72,
            decoration: const BoxDecoration(
              color: AppColors.navy, shape: BoxShape.circle,
            ),
            alignment: Alignment.center,
            child: Text(
              (_dealer['dealerName'] ?? 'D').substring(0, 1).toUpperCase(),
              style: const TextStyle(
                fontSize: 28, fontWeight: FontWeight.w900, color: Colors.white,
              ),
            ),
          ),
        ),
        const SizedBox(height: 12),
        Center(
          child: Text(
            _dealer['dealerName'] ?? '',
            style: const TextStyle(
              fontSize: 18, fontWeight: FontWeight.w800, color: AppColors.gray7,
            ),
          ),
        ),
        Center(
          child: Text(
            _dealer['dealerCode'] ?? '',
            style: const TextStyle(fontSize: 13, color: AppColors.blue),
          ),
        ),
        const SizedBox(height: 28),
        _ProfileRow(Icons.phone_outlined, 'Mobile',
            '+91 ${_dealer['mobile'] ?? ''}'),
        _ProfileRow(Icons.location_city_outlined, 'City',
            _dealer['city'] ?? '—'),
        const SizedBox(height: 32),
        ElevatedButton.icon(
          onPressed: _logout,
          style: ElevatedButton.styleFrom(
            backgroundColor: AppColors.red,
            foregroundColor: Colors.white,
          ),
          icon: const Icon(Icons.logout, size: 18),
          label: const Text('Logout'),
        ),
      ],
    );
  }

  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const Icon(Icons.wifi_off, size: 48, color: AppColors.gray4),
            const SizedBox(height: 12),
            Text(_error!, textAlign: TextAlign.center,
                style: const TextStyle(color: AppColors.gray5)),
            const SizedBox(height: 20),
            ElevatedButton(onPressed: _load, child: const Text('Retry')),
          ],
        ),
      ),
    );
  }

  // ── Bottom nav ──────────────────────────────────────────────
  Widget _buildBottomNav() {
    return NavigationBar(
      selectedIndex: _navIndex,
      onDestinationSelected: (i) => setState(() => _navIndex = i),
      backgroundColor: Colors.white,
      indicatorColor: const Color(0xFFFEF3C7),
      destinations: const [
        NavigationDestination(
          icon:         Icon(Icons.home_outlined),
          selectedIcon: Icon(Icons.home, color: AppColors.goldDark),
          label:        'Home',
        ),
        NavigationDestination(
          icon:         Icon(Icons.electric_scooter_outlined),
          selectedIcon: Icon(Icons.electric_scooter, color: AppColors.goldDark),
          label:        'Inventory',
        ),
        NavigationDestination(
          icon:         Icon(Icons.person_outline),
          selectedIcon: Icon(Icons.person, color: AppColors.goldDark),
          label:        'Profile',
        ),
      ],
    );
  }
}

// ── KPI card ─────────────────────────────────────────────────

class _KpiData {
  final String label;
  final String value;
  final Color  color;
  final IconData icon;
  const _KpiData({
    required this.label, required this.value,
    required this.color, required this.icon,
  });
}

class _KpiCard extends StatelessWidget {
  final _KpiData data;
  const _KpiCard({required this.data});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 4),
      padding: const EdgeInsets.symmetric(vertical: 14, horizontal: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border(top: BorderSide(color: data.color, width: 3)),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.05),
            blurRadius: 8, offset: const Offset(0, 2),
          )
        ],
      ),
      child: Column(
        children: [
          Icon(data.icon, color: data.color, size: 22),
          const SizedBox(height: 8),
          Text(
            data.value,
            style: TextStyle(
              fontSize: 26, fontWeight: FontWeight.w900, color: data.color,
            ),
          ),
          const SizedBox(height: 4),
          Text(
            data.label,
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 10, color: AppColors.gray5),
          ),
        ],
      ),
    );
  }
}

// ── Vehicle card ─────────────────────────────────────────────

class _VehicleCard extends StatelessWidget {
  final Map<String, dynamic> vehicle;
  const _VehicleCard(this.vehicle);

  Color _statusColor(String status) {
    return switch (status) {
      'PendingAdminReview' => AppColors.amber,
      'AdminApproved'      => AppColors.blue,
      'AdminRejected'      => AppColors.red,
      'Refurbishment'      => const Color(0xFF7C3AED),
      'Listed'             => AppColors.green,
      'Sold' || 'Settled'  => AppColors.gray5,
      _                    => AppColors.gray4,
    };
  }

  String _statusLabel(String status) {
    return switch (status) {
      'PendingAdminReview' => 'PENDING APPROVAL',
      'AdminApproved'      => 'APPROVED',
      'AdminRejected'      => 'REJECTED',
      'AdminModified'      => 'PRICE MODIFIED',
      'Refurbishment'      => 'REFURBISHMENT',
      'Listed'             => 'LISTED',
      'Sold'               => 'SOLD',
      'Settled'            => 'SETTLED',
      _                    => status.toUpperCase(),
    };
  }

  @override
  Widget build(BuildContext context) {
    final status  = vehicle['status'] as String? ?? '';
    final sColor  = _statusColor(status);
    final price   = vehicle['approvedPrice'];

    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(14),
        border: Border.all(color: AppColors.gray3),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.04),
            blurRadius: 6, offset: const Offset(0, 2),
          )
        ],
      ),
      child: Material(
        color: Colors.transparent,
        borderRadius: BorderRadius.circular(14),
        child: InkWell(
          borderRadius: BorderRadius.circular(14),
          onTap: () {
            // TODO: Navigate to vehicle detail
          },
          child: Padding(
            padding: const EdgeInsets.all(14),
            child: Row(
              children: [
                // Image placeholder
                Container(
                  width: 64, height: 56,
                  decoration: BoxDecoration(
                    color: AppColors.gray2,
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: const Icon(Icons.electric_scooter,
                      color: AppColors.gray4, size: 28),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        '${vehicle['vehicleModel'] ?? ''} ${vehicle['vehicleVariant'] ?? ''}',
                        style: const TextStyle(
                          fontSize: 13, fontWeight: FontWeight.w800,
                          color: AppColors.gray7,
                        ),
                      ),
                      const SizedBox(height: 2),
                      Text(
                        '${vehicle['registrationNo'] ?? ''} · ${vehicle['customerName'] ?? ''}',
                        style: const TextStyle(
                          fontSize: 11, color: AppColors.gray5,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 8, vertical: 3),
                        decoration: BoxDecoration(
                          color: sColor.withOpacity(0.12),
                          borderRadius: BorderRadius.circular(999),
                        ),
                        child: Text(
                          _statusLabel(status),
                          style: TextStyle(
                            fontSize: 9, fontWeight: FontWeight.w800,
                            color: sColor, letterSpacing: 0.4,
                          ),
                        ),
                      ),
                    ],
                  ),
                ),
                if (price != null) ...[
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      Text(
                        '₹${_formatPrice(price)}',
                        style: const TextStyle(
                          fontSize: 13, fontWeight: FontWeight.w900,
                          color: AppColors.green,
                        ),
                      ),
                      const Text(
                        'Approved',
                        style: TextStyle(fontSize: 9, color: AppColors.gray4),
                      ),
                    ],
                  ),
                ],
                const SizedBox(width: 4),
                const Icon(Icons.chevron_right, color: AppColors.gray3, size: 20),
              ],
            ),
          ),
        ),
      ),
    );
  }

  String _formatPrice(dynamic price) {
    try {
      final n = (price as num).toInt();
      if (n >= 100000) return '${(n / 100000).toStringAsFixed(1)}L';
      if (n >= 1000) return '${(n / 1000).toStringAsFixed(1)}K';
      return n.toString();
    } catch (_) {
      return price.toString();
    }
  }
}

// ── Profile row ───────────────────────────────────────────────

class _ProfileRow extends StatelessWidget {
  final IconData icon;
  final String   label;
  final String   value;
  const _ProfileRow(this.icon, this.label, this.value);

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 10),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.gray3),
      ),
      child: Row(
        children: [
          Icon(icon, color: AppColors.gray4, size: 20),
          const SizedBox(width: 12),
          Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(label,
                  style: const TextStyle(
                      fontSize: 10, color: AppColors.gray4,
                      fontWeight: FontWeight.w700)),
              const SizedBox(height: 2),
              Text(value,
                  style: const TextStyle(
                      fontSize: 14, fontWeight: FontWeight.w700,
                      color: AppColors.gray7)),
            ],
          ),
        ],
      ),
    );
  }
}