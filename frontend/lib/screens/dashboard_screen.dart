// lib/screens/dashboard_screen.dart
// Matches dashboard.tsx — Scooty Inventory grid + pincode search + add item sheet
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';
import 'vehicle_detail_screen.dart';
import 'inventory_add_sheet.dart';
import 'exchange_program_screen.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key});
  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _DashboardScreenState extends State<DashboardScreen> {
  // ── State ─────────────────────────────────────────────────
  List<Map<String, dynamic>> _allVehicles  = [];
  List<Map<String, dynamic>> _pinVehicles  = [];
  bool   _loading     = true;
  bool   _pinLoading  = false;
  String _error       = '';
  String _searchQuery = '';
  int    _navIndex    = 0;

  Map<String, dynamic>? _activeLocation;

  final _searchCtrl  = TextEditingController();
  final _pincodeCtrl = TextEditingController();

  List<Map<String, dynamic>> _pinSuggestions = [];
  bool _pinDropOpen = false;

  String _username = '';
  String _role     = '';

  // ── Init ──────────────────────────────────────────────────
  @override
  void initState() {
    super.initState();
    _loadMeta();
    _fetchVehicles();
    _searchCtrl.addListener(() => setState(() => _searchQuery = _searchCtrl.text));
    _pincodeCtrl.addListener(_onPincodeChanged);
  }

  @override
  void dispose() {
    _searchCtrl.dispose();
    _pincodeCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadMeta() async {
    final info = await ApiService.getLocalDealerInfo();
    setState(() {
      _username = info['dealerName'] ?? '';
      _role     = info['role']       ?? '';
    });
  }

  // ── Fetchers ──────────────────────────────────────────────
  Future<void> _fetchVehicles({Map<String, dynamic>? loc}) async {
    final isLoc = loc != null;
    if (isLoc) setState(() => _pinLoading = true);
    else       setState(() => _loading = true);

    try {
      final vehicles = await ApiService.getVehicles(
        pincode: loc?['pincode'],
        cityId:  loc?['cityId'],
      );
      if (isLoc) {
        setState(() { _pinVehicles = vehicles; _pinLoading = false; });
      } else {
        setState(() { _allVehicles = vehicles; _loading = false; _error = ''; });
      }
    } catch (e) {
      setState(() {
        if (!isLoc) { _error = 'Failed to load vehicles'; _loading = false; }
        else _pinLoading = false;
      });
    }
  }

  void _onPincodeChanged() {
    final q = _pincodeCtrl.text;
    if (q.length < 2) {
      setState(() { _pinSuggestions = []; _pinDropOpen = false; });
      return;
    }
    Future.delayed(const Duration(milliseconds: 300), () async {
      if (_pincodeCtrl.text != q) return;
      try {
        final s = await ApiService.searchPincode(q);
        setState(() { _pinSuggestions = s; _pinDropOpen = s.isNotEmpty; });
      } catch (_) {}
    });
  }

  void _showProfileSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => Container(
        height: MediaQuery.of(context).size.height * 0.55,
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(24)),
        ),
        child: Column(children: [
          // Handle
          Container(
            margin: const EdgeInsets.only(top: 12),
            width: 40, height: 4,
            decoration: BoxDecoration(color: AppColors.gray3, borderRadius: BorderRadius.circular(2)),
          ),
          // Header
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 16, 20, 0),
            child: Row(children: [
              const Text('My Profile', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: AppColors.gray7)),
              const Spacer(),
              GestureDetector(onTap: () => Navigator.pop(context), child: const Icon(Icons.close, color: AppColors.gray5)),
            ]),
          ),
          const Divider(height: 24),
          // Avatar + name
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Row(children: [
              Container(
                width: 56, height: 56,
                decoration: BoxDecoration(
                  gradient: const LinearGradient(colors: [AppColors.gold, AppColors.goldDark]),
                  borderRadius: BorderRadius.circular(28),
                ),
                alignment: Alignment.center,
                child: Text(_initials, style: const TextStyle(
                  color: AppColors.navy, fontWeight: FontWeight.w900, fontSize: 20,
                )),
              ),
              const SizedBox(width: 14),
              Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                Text(_username.isNotEmpty ? _username : 'Dealer', style: const TextStyle(
                  fontWeight: FontWeight.w800, fontSize: 16, color: AppColors.gray7,
                )),
                const SizedBox(height: 2),
                Text(_role.isNotEmpty ? _role : 'BGauss Dealer', style: const TextStyle(
                  color: AppColors.gray4, fontSize: 13,
                )),
              ])),
            ]),
          ),
          const SizedBox(height: 20),
          // Info rows
          Padding(
            padding: const EdgeInsets.symmetric(horizontal: 20),
            child: Container(
              decoration: BoxDecoration(
                color: AppColors.gray1,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: AppColors.gray3),
              ),
              child: Column(children: [
                _profileRow(Icons.person_outline, 'Name', _username.isNotEmpty ? _username : '—'),
                const Divider(height: 1, indent: 16, endIndent: 16),
                _profileRow(Icons.badge_outlined, 'Role', _role.isNotEmpty ? _role : 'Dealer'),
                const Divider(height: 1, indent: 16, endIndent: 16),
                _profileRow(Icons.store_outlined, 'Portal', 'BGauss B2B Buyback'),
              ]),
            ),
          ),
          const Spacer(),
          // Logout button
          Padding(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 24),
            child: GestureDetector(
              onTap: () { Navigator.pop(context); _handleLogout(); },
              child: Container(
                width: double.infinity,
                padding: const EdgeInsets.symmetric(vertical: 14),
                decoration: BoxDecoration(
                  color: const Color(0xFFFEE2E2),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: AppColors.red.withValues(alpha: 0.3)),
                ),
                child: const Row(mainAxisAlignment: MainAxisAlignment.center, children: [
                  Icon(Icons.logout, color: AppColors.red, size: 18),
                  SizedBox(width: 8),
                  Text('Logout', style: TextStyle(color: AppColors.red, fontWeight: FontWeight.w800, fontSize: 15)),
                ]),
              ),
            ),
          ),
        ]),
      ),
    );
  }

  Widget _profileRow(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(children: [
        Icon(icon, color: AppColors.gray4, size: 18),
        const SizedBox(width: 12),
        Text(label, style: const TextStyle(color: AppColors.gray5, fontSize: 13)),
        const Spacer(),
        Text(value, style: const TextStyle(color: AppColors.gray7, fontWeight: FontWeight.w600, fontSize: 13)),
      ]),
    );
  }

  Future<void> _selectLocation(Map<String, dynamic> area) async {
    final loc = {
      'pincode':  area['pincode'],
      'cityId':   area['cityId'],
      'areaId':   area['id'],
      'areaName': area['areaName'],
      'cityName': area['cityName'],
    };
    setState(() {
      _activeLocation = loc;
      _pincodeCtrl.text = area['pincode'] ?? '';
      _pinDropOpen = false;
      _pinSuggestions = [];
    });
    await _fetchVehicles(loc: loc);
  }

  void _clearLocation() {
    setState(() {
      _activeLocation = null;
      _pinVehicles = [];
      _pincodeCtrl.clear();
      _pinSuggestions = [];
      _pinDropOpen = false;
    });
  }

  // ── Filtered list ──────────────────────────────────────────
  List<Map<String, dynamic>> get _displayVehicles =>
      _activeLocation != null ? _pinVehicles : _allVehicles;

  List<Map<String, dynamic>> get _filtered {
    final q = _searchQuery.toLowerCase().trim();
    if (q.isEmpty) return _displayVehicles;
    return _displayVehicles.where((v) =>
      (v['modelName']   ?? '').toString().toLowerCase().contains(q) ||
      (v['variantName'] ?? '').toString().toLowerCase().contains(q) ||
      (v['price']       ?? '').toString().contains(q) ||
      (v['rangeKm']     ?? '').toString().contains(q)
    ).toList();
  }

  String get _initials {
    if (_username.isEmpty) return '?';
    final parts = _username.trim().split(' ').where((s) => s.isNotEmpty).toList();
    if (parts.length == 1) return parts[0][0].toUpperCase();
    return (parts.first[0] + parts.last[0]).toUpperCase();
  }

  // ── Navigate to vehicle ────────────────────────────────────
  void _openVehicle(Map<String, dynamic> v) {
    Navigator.push(context, MaterialPageRoute(
      builder: (_) => VehicleDetailScreen(
        scootyId: v['scootyId'],
        pincode:  _activeLocation?['pincode'],
        cityId:   _activeLocation?['cityId'],
      ),
    ));
  }

  void _openAddSheet() {
    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => InventoryAddSheet(onAdded: _fetchVehicles),
    );
  }

  void _handleLogout() async {
    await ApiService.logout();
    if (mounted) Navigator.pushReplacementNamed(context, '/login');
  }

  // ══════════════════════════════════════════════════════════
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.gray1,
      body: SafeArea(
        child: Column(
          children: [
            _buildNavBar(),
            _buildPincodeBar(),
            if (_activeLocation != null) _buildLocationBadge(),
            _buildTitleRow(),
            Expanded(child: _buildBody()),
          ],
        ),
      ),
      bottomNavigationBar: _buildBottomNav(),
    );
  }

  // ── NAVBAR ────────────────────────────────────────────────
  Widget _buildNavBar() {
    return Container(
      color: AppColors.navy,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(
        children: [
          // Logo + brand
          Container(
            width: 38, height: 38,
            decoration: BoxDecoration(color: AppColors.gold, borderRadius: BorderRadius.circular(8)),
            alignment: Alignment.center,
            child: const Text('BG', style: TextStyle(fontWeight: FontWeight.w900, fontSize: 13, color: AppColors.navy)),
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              const Text('BGauss Portal', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 14)),
              Text('Dashboard', style: TextStyle(color: Colors.white.withValues(alpha:0.55), fontSize: 11)),
            ]),
          ),
          // Search bar
          Expanded(
            flex: 2,
            child: Container(
              height: 38,
              decoration: BoxDecoration(color: AppColors.navyMid, borderRadius: BorderRadius.circular(20)),
              padding: const EdgeInsets.symmetric(horizontal: 12),
              child: Row(children: [
                const Icon(Icons.search, color: AppColors.gray4, size: 16),
                const SizedBox(width: 8),
                Expanded(
                  child: TextField(
                    controller: _searchCtrl,
                    style: const TextStyle(color: Colors.white, fontSize: 13),
                    decoration: const InputDecoration(
                      hintText: 'Search model, variant…',
                      hintStyle: TextStyle(color: AppColors.gray4, fontSize: 12),
                      border: InputBorder.none,
                      isDense: true,
                      contentPadding: EdgeInsets.zero,
                      filled: false,
                    ),
                  ),
                ),
                if (_searchQuery.isNotEmpty)
                  GestureDetector(
                    onTap: () => _searchCtrl.clear(),
                    child: const Icon(Icons.close, color: AppColors.gray4, size: 16),
                  ),
              ]),
            ),
          ),
          const SizedBox(width: 10),
          // Avatar
          Container(
            width: 36, height: 36,
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [AppColors.gold, AppColors.goldDark]),
              borderRadius: BorderRadius.circular(18),
            ),
            alignment: Alignment.center,
            child: Text(_initials, style: const TextStyle(color: AppColors.navy, fontWeight: FontWeight.w900, fontSize: 13)),
          ),
          const SizedBox(width: 8),
          PopupMenuButton<String>(
            icon: const Icon(Icons.more_vert, color: Colors.white, size: 22),
            onSelected: (v) {
              if (v == 'logout') _handleLogout();
              if (v == 'exchange') {
                Navigator.push(context, MaterialPageRoute(builder: (_) => const ExchangeProgramScreen()));
              }
            },
            itemBuilder: (_) => [
              const PopupMenuItem(value: 'exchange', child: Text('Exchange / Buyback')),
              const PopupMenuItem(value: 'logout',   child: Text('Logout')),
            ],
          ),
        ],
      ),
    );
  }

  // ── PINCODE BAR ───────────────────────────────────────────
  Widget _buildPincodeBar() {
    return Container(
      color: AppColors.navy,
      padding: const EdgeInsets.only(left: 16, right: 16, bottom: 12),
      child: Column(
        children: [
          Container(
            height: 40,
            decoration: BoxDecoration(
              color: _activeLocation != null
                  ? const Color(0xFF064E3B)
                  : AppColors.navyMid,
              borderRadius: BorderRadius.circular(20),
              border: _activeLocation != null
                  ? Border.all(color: AppColors.green.withValues(alpha: 0.4))
                  : null,
            ),
            padding: const EdgeInsets.symmetric(horizontal: 12),
            child: Row(children: [
              Icon(Icons.location_on_outlined,
                  color: _activeLocation != null ? AppColors.green : AppColors.gray4,
                  size: 16),
              const SizedBox(width: 8),
              Expanded(
                child: TextField(
                  controller: _pincodeCtrl,
                  style: TextStyle(
                    color: _activeLocation != null ? AppColors.green : Colors.white,
                    fontSize: 13,
                  ),
                  decoration: const InputDecoration(
                    hintText: 'Pincode or City…',
                    hintStyle: TextStyle(color: AppColors.gray4, fontSize: 12),
                    border: InputBorder.none,
                    isDense: true,
                    contentPadding: EdgeInsets.zero,
                    filled: false,
                  ),
                ),
              ),
              if (_pinLoading)
                const SizedBox(width: 16, height: 16,
                    child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.green)),
              if (!_pinLoading && _pincodeCtrl.text.isNotEmpty)
                GestureDetector(
                  onTap: _clearLocation,
                  child: const Icon(Icons.close, color: AppColors.gray4, size: 16),
                ),
            ]),
          ),
          // Dropdown suggestions
          if (_pinDropOpen && _pinSuggestions.isNotEmpty)
            Container(
              margin: const EdgeInsets.only(top: 4),
              decoration: BoxDecoration(
                color: AppColors.navyMid,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.gray5.withValues(alpha: 0.3)),
              ),
              child: Column(
                children: _pinSuggestions.map((area) => InkWell(
                  onTap: () => _selectLocation(area),
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                    child: Row(children: [
                      const Icon(Icons.place_outlined, color: AppColors.gray4, size: 14),
                      const SizedBox(width: 8),
                      Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        Text(area['areaName'] ?? '', style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w600)),
                        Text('${area['cityName']}, ${area['stateName']}', style: const TextStyle(color: AppColors.gray4, fontSize: 11)),
                      ])),
                      Text(area['pincode'] ?? '', style: const TextStyle(color: AppColors.gold, fontSize: 12, fontWeight: FontWeight.w700)),
                    ]),
                  ),
                )).toList(),
              ),
            ),
        ],
      ),
    );
  }

  // ── LOCATION BADGE ────────────────────────────────────────
  Widget _buildLocationBadge() {
    final loc = _activeLocation!;
    return Container(
      color: const Color(0xFFECFDF5),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 8),
      child: Row(children: [
        const Text('📍', style: TextStyle(fontSize: 14)),
        const SizedBox(width: 6),
        Expanded(
          child: Text(
            '${loc['areaName'] ?? loc['cityName']} · ${_pinVehicles.length} vehicle${_pinVehicles.length != 1 ? 's' : ''}',
            style: const TextStyle(color: AppColors.green, fontWeight: FontWeight.w600, fontSize: 13),
          ),
        ),
        GestureDetector(
          onTap: _clearLocation,
          child: const Text('✕ Clear', style: TextStyle(color: AppColors.green, fontSize: 12, fontWeight: FontWeight.w600)),
        ),
      ]),
    );
  }

  // ── TITLE ROW ─────────────────────────────────────────────
  Widget _buildTitleRow() {
    final loc = _activeLocation;
    return Container(
      color: Colors.white,
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      child: Row(children: [
        Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          Text(
            loc != null
                ? 'Scooties in ${loc['areaName'] ?? loc['cityName']}'
                : 'Scooty Inventory',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w900, color: AppColors.gray7),
          ),
          Text(
            _loading ? 'Loading…'
                : '${_filtered.length} vehicle${_filtered.length != 1 ? 's' : ''}',
            style: const TextStyle(color: AppColors.gray4, fontSize: 12),
          ),
        ])),
        // Exchange button
        GestureDetector(
          onTap: () => Navigator.push(context, MaterialPageRoute(builder: (_) => const ExchangeProgramScreen())),
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
            decoration: BoxDecoration(
              gradient: const LinearGradient(colors: [Color(0xFF16A34A), Color(0xFF14532D)]),
              borderRadius: BorderRadius.circular(10),
            ),
            child: const Text('🔄 Exchange', style: TextStyle(color: Colors.white, fontSize: 12, fontWeight: FontWeight.w700)),
          ),
        ),
        const SizedBox(width: 8),
        // Add button
        GestureDetector(
          onTap: _openAddSheet,
          child: Container(
            width: 36, height: 36,
            decoration: BoxDecoration(color: AppColors.navy, borderRadius: BorderRadius.circular(10)),
            child: const Icon(Icons.add, color: Colors.white, size: 20),
          ),
        ),
      ]),
    );
  }

  // ── BODY ──────────────────────────────────────────────────
  Widget _buildBody() {
    if (_loading && _allVehicles.isEmpty) return _buildSkeletons();
    if (_error.isNotEmpty) return _buildError();
    if (_activeLocation != null && _pinVehicles.isEmpty && !_pinLoading) return _buildNoPinResults();
    if (_filtered.isEmpty && _searchQuery.isNotEmpty) return _buildNoSearch();
    return RefreshIndicator(
      onRefresh: () => _fetchVehicles(loc: _activeLocation),
      color: AppColors.gold,
      child: GridView.builder(
        padding: const EdgeInsets.all(12),
        gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
          crossAxisCount: 2,
          crossAxisSpacing: 10,
          mainAxisSpacing: 10,
          childAspectRatio: 0.78,
        ),
        itemCount: _filtered.length,
        itemBuilder: (_, i) => _VehicleCard(
          vehicle: _filtered[i],
          showStock: _activeLocation != null,
          onTap: () => _openVehicle(_filtered[i]),
        ),
      ),
    );
  }

  Widget _buildSkeletons() {
    return GridView.builder(
      padding: const EdgeInsets.all(12),
      gridDelegate: const SliverGridDelegateWithFixedCrossAxisCount(
        crossAxisCount: 2, crossAxisSpacing: 10, mainAxisSpacing: 10, childAspectRatio: 0.78,
      ),
      itemCount: 8,
      itemBuilder: (_, __) => _SkeletonCard(),
    );
  }

  Widget _buildError() {
    return Center(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        const Icon(Icons.wifi_off, color: AppColors.gray4, size: 48),
        const SizedBox(height: 12),
        Text(_error, style: const TextStyle(color: AppColors.gray5)),
        const SizedBox(height: 16),
        ElevatedButton(onPressed: _fetchVehicles, child: const Text('Retry')),
      ]),
    );
  }

  Widget _buildNoPinResults() {
    return Center(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        const Text('🛵', style: TextStyle(fontSize: 48)),
        const SizedBox(height: 12),
        Text(
          'No in-stock vehicles in ${_activeLocation!['areaName'] ?? _activeLocation!['pincode']}',
          textAlign: TextAlign.center,
          style: const TextStyle(fontWeight: FontWeight.w700, color: AppColors.gray7),
        ),
        const SizedBox(height: 8),
        TextButton(onPressed: _clearLocation, child: const Text('View all inventory')),
      ]),
    );
  }

  Widget _buildNoSearch() {
    return Center(
      child: Column(mainAxisSize: MainAxisSize.min, children: [
        const Text('🔍', style: TextStyle(fontSize: 48)),
        const SizedBox(height: 12),
        Text('No vehicles match "$_searchQuery"', style: const TextStyle(color: AppColors.gray7)),
        const SizedBox(height: 8),
        TextButton(onPressed: () => _searchCtrl.clear(), child: const Text('Clear search')),
      ]),
    );
  }

  // ── BOTTOM NAV ────────────────────────────────────────────
  Widget _buildBottomNav() {
    return NavigationBar(
      selectedIndex: _navIndex,
      onDestinationSelected: (i) {
        setState(() => _navIndex = i);
        if (i == 1) {
          Navigator.push(context, MaterialPageRoute(builder: (_) => const ExchangeProgramScreen()))
              .then((_) => setState(() => _navIndex = 0));
        } else if (i == 2) {
          _showProfileSheet();
          setState(() => _navIndex = 0); // reset tab after opening sheet
        }
      },
      backgroundColor: Colors.white,
      indicatorColor: const Color(0xFFFEF3C7),
      destinations: const [
        NavigationDestination(
          icon: Icon(Icons.electric_scooter_outlined),
          selectedIcon: Icon(Icons.electric_scooter, color: AppColors.goldDark),
          label: 'Inventory',
        ),
        NavigationDestination(
          icon: Icon(Icons.swap_horiz_outlined),
          selectedIcon: Icon(Icons.swap_horiz, color: AppColors.goldDark),
          label: 'Exchange',
        ),
        NavigationDestination(
          icon: Icon(Icons.person_outline),
          selectedIcon: Icon(Icons.person, color: AppColors.goldDark),
          label: 'Profile',
        ),
      ],
    );
  }
}

// ── VEHICLE CARD ──────────────────────────────────────────────
class _VehicleCard extends StatelessWidget {
  final Map<String, dynamic> vehicle;
  final bool showStock;
  final VoidCallback onTap;

  const _VehicleCard({required this.vehicle, required this.onTap, this.showStock = false});

  @override
  Widget build(BuildContext context) {
    final price = vehicle['price'];
    final range = vehicle['rangeKm'];
    final qty   = vehicle['stockQuantity'];
    final inStock = vehicle['stockAvailable'] as bool? ?? true;

    return GestureDetector(
      onTap: onTap,
      child: Container(
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [BoxShadow(color: Colors.black.withValues(alpha: 0.06), blurRadius: 8, offset: const Offset(0, 2))],
        ),
        child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
          // Image
          ClipRRect(
            borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
            child: Stack(children: [
              AspectRatio(
                aspectRatio: 16 / 10,
                child: vehicle['imageUrl'] != null
                    ? Image.network(
                        vehicle['imageUrl'],
                        fit: BoxFit.cover,
                        errorBuilder: (_, __, ___) => _placeholderImage(),
                      )
                    : _placeholderImage(),
              ),
              if (showStock && qty != null)
                Positioned(
                  top: 8, right: 8,
                  child: Container(
                    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 4),
                    decoration: BoxDecoration(
                      color: qty > 0 ? AppColors.green : AppColors.red,
                      borderRadius: BorderRadius.circular(20),
                    ),
                    child: Text(
                      qty > 0 ? '$qty left' : 'Sold out',
                      style: const TextStyle(color: Colors.white, fontSize: 10, fontWeight: FontWeight.w700),
                    ),
                  ),
                ),
              if (!inStock)
                Positioned.fill(
                  child: Container(
                    color: Colors.black.withValues(alpha: 0.3),
                    alignment: Alignment.center,
                    child: Container(
                      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
                      decoration: BoxDecoration(color: AppColors.red, borderRadius: BorderRadius.circular(8)),
                      child: const Text('Out of Stock', style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.w700)),
                    ),
                  ),
                ),
            ]),
          ),
          // Body
          Padding(
            padding: const EdgeInsets.all(10),
            child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
              Text(vehicle['modelName'] ?? '', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14, color: AppColors.gray7)),
              const SizedBox(height: 2),
              Text(vehicle['variantName'] ?? '', style: const TextStyle(color: AppColors.gray5, fontSize: 12)),
              const SizedBox(height: 8),
              Wrap(spacing: 6, runSpacing: 4, children: [
                if (range != null) _Chip('🛣 $range km', AppColors.blue.withValues(alpha: 0.08), AppColors.blue),
                if (price != null) _Chip('₹ ${_fmtPrice(price)}', AppColors.green.withValues(alpha: 0.08), AppColors.green),
              ]),
            ]),
          ),
        ]),
      ),
    );
  }

  Widget _placeholderImage() => Container(
    color: AppColors.gray2,
    child: const Center(child: Icon(Icons.electric_scooter, color: AppColors.gray4, size: 36)),
  );

  String _fmtPrice(dynamic p) {
    try {
      final n = (p as num).toInt();
      if (n >= 100000) return '${(n / 100000).toStringAsFixed(1)}L';
      if (n >= 1000)   return '${(n / 1000).toStringAsFixed(0)}K';
      return n.toString();
    } catch (_) { return p.toString(); }
  }
}

Widget _Chip(String label, Color bg, Color color) {
  return Container(
    padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
    decoration: BoxDecoration(color: bg, borderRadius: BorderRadius.circular(8)),
    child: Text(label, style: TextStyle(color: color, fontSize: 11, fontWeight: FontWeight.w600)),
  );
}

// ── SKELETON ──────────────────────────────────────────────────
class _SkeletonCard extends StatelessWidget {
  @override
  Widget build(BuildContext context) {
    return Container(
      decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(16)),
      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
        ClipRRect(
          borderRadius: const BorderRadius.vertical(top: Radius.circular(16)),
          child: AspectRatio(aspectRatio: 16 / 10, child: Container(color: AppColors.gray2)),
        ),
        Padding(
          padding: const EdgeInsets.all(10),
          child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
            Container(height: 14, width: 100, decoration: BoxDecoration(color: AppColors.gray2, borderRadius: BorderRadius.circular(4))),
            const SizedBox(height: 6),
            Container(height: 11, width: 70, decoration: BoxDecoration(color: AppColors.gray2, borderRadius: BorderRadius.circular(4))),
            const SizedBox(height: 10),
            Container(height: 22, width: 120, decoration: BoxDecoration(color: AppColors.gray2, borderRadius: BorderRadius.circular(8))),
          ]),
        ),
      ]),
    );
  }
}