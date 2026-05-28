// lib/screens/vehicle_detail_screen.dart
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class VehicleDetailScreen extends StatefulWidget {
  final int scootyId;
  final String? pincode;
  final int? cityId;

  const VehicleDetailScreen({
    super.key,
    required this.scootyId,
    this.pincode,
    this.cityId,
  });

  @override
  State<VehicleDetailScreen> createState() => _VehicleDetailScreenState();
}

class _VehicleDetailScreenState extends State<VehicleDetailScreen> {
  Map<String, dynamic>? _vehicle;
  List<Map<String, dynamic>> _variants = [];
  bool _loading = true;
  String _error = '';

  bool _isLiked = false;
  int _likeCount = 0;
  bool _likeLoading = false;
  String _shareMsg = '';

  Map<String, dynamic>? _reviewSummary;

  // ── EMI modal state kept but button is commented out ─────────
  // bool _emiModalOpen = false;
  // final _emiNameCtrl   = TextEditingController();
  // final _emiMobileCtrl = TextEditingController();
  // final _emiPinCtrl    = TextEditingController();
  // bool? _emiWantsLoan;
  // bool  _emiSubmitting = false;
  // String _emiMsg  = '';
  // bool   _emiSuccess = false;

  String get _username => 'dealer';

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void dispose() {
    // _emiNameCtrl.dispose();
    // _emiMobileCtrl.dispose();
    // _emiPinCtrl.dispose();
    super.dispose();
  }

  Future<void> _load() async {
    setState(() { _loading = true; _error = ''; });
    try {
      // ── 1. Load THIS vehicle's detail ────────────────────────
      final v = await ApiService.getVehicleDetail(
        widget.scootyId,
        pincode: widget.pincode,
        cityId:  widget.cityId,
      );

      // ── 2. Load ALL variants of the same model ───────────────
      // Use modelId from the detail response to fetch variants properly.
      // We get all inventory then filter by modelId so every colour/variant
      // of this model shows up in the variants grid.
      // Use full inventory endpoint to get ALL variants of this model
      List<Map<String, dynamic>> allItems = [];
      try {
        allItems = await ApiService.getAllInventory();
      } catch (_) {
        // fallback to models-list if full inventory fails
        allItems = await ApiService.getVehicles();
      }
      final current = allItems.firstWhere(
        (i) => (i['scootyId'] ?? i['ScootyId']) == widget.scootyId,
        orElse: () => <String, dynamic>{},
      );
      final modelName = v['modelName']?.toString().toLowerCase() ?? '';
      final related = allItems.where((i) {
        // Match by modelId if available, else by modelName
        final iModelId = i['modelId'] ?? i['ModelId'];
        final cModelId = current['modelId'] ?? current['ModelId'];
        if (iModelId != null && cModelId != null) {
          return iModelId.toString() == cModelId.toString();
        }
        return (i['modelName'] ?? '').toString().toLowerCase() == modelName;
      }).toList();
      related.sort((a, b) {
        final aStock = a['stockAvailable'] as bool? ?? false;
        final bStock = b['stockAvailable'] as bool? ?? false;
        if (aStock != bStock) return aStock ? -1 : 1;
        return ((b['price'] as num?)?.toDouble() ?? 0)
            .compareTo((a['price'] as num?)?.toDouble() ?? 0);
      });

      // Sort: in-stock first, then by price descending
      related.sort((a, b) {
        final aStock = a['stockAvailable'] as bool? ?? false;
        final bStock = b['stockAvailable'] as bool? ?? false;
        if (aStock != bStock) return aStock ? -1 : 1;
        return ((b['price'] as num?)?.toDouble() ?? 0)
            .compareTo((a['price'] as num?)?.toDouble() ?? 0);
      });

      // ── 3. Likes & reviews ───────────────────────────────────
      try {
        final like = await ApiService.getLikeStatus(widget.scootyId, _username);
        _isLiked   = like['liked'] as bool? ?? false;
        _likeCount = like['count'] as int?  ?? 0;
      } catch (_) {}

      try {
        final rev = await ApiService.getReviewSummary(widget.scootyId);
        _reviewSummary = rev['summary'] as Map<String, dynamic>?;
      } catch (_) {}

      setState(() {
        _vehicle  = v;
        _variants = related;
        _loading  = false;
      });
    } catch (e) {
      setState(() {
        _error   = e.toString().replaceFirst('Exception: ', '');
        _loading = false;
      });
    }
  }

  Future<void> _handleLike() async {
    if (_vehicle == null || _likeLoading) return;
    setState(() => _likeLoading = true);
    try {
      final res = await ApiService.toggleLike(widget.scootyId, _username);
      setState(() {
        _isLiked   = res['liked'] as bool? ?? false;
        _likeCount = res['count'] as int?  ?? 0;
      });
    } catch (_) {}
    setState(() => _likeLoading = false);
  }

  Future<void> _handleShare() async {
    final v = _vehicle;
    if (v == null) return;
    final text = '${v['modelName']} ${v['variantName']} — Check it out!';
    await Clipboard.setData(ClipboardData(text: text));
    setState(() => _shareMsg = 'Link copied!');
    await Future.delayed(const Duration(seconds: 2));
    if (mounted) setState(() => _shareMsg = '');
  }

  String _fmtPrice(dynamic p) {
    if (p == null) return 'Price on request';
    try {
      final n = (p as num).toInt();
      final s = n.toString();
      if (s.length <= 3) return '₹ $s';
      final buf = StringBuffer();
      int count = 0;
      for (int i = s.length - 1; i >= 0; i--) {
        if (count > 0 &&
            (count == 3 || (count > 3 && (count - 3) % 2 == 0))) {
          buf.write(',');
        }
        buf.write(s[i]);
        count++;
      }
      return '₹ ${buf.toString().split('').reversed.join()}';
    } catch (_) {
      return '₹ $p';
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.gray1,
      body: _loading
          ? const Center(
              child: CircularProgressIndicator(color: AppColors.gold))
          : _error.isNotEmpty
              ? _buildError()
              : _buildDetail(),
    );
  }

  Widget _buildError() {
    return Center(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(mainAxisSize: MainAxisSize.min, children: [
          const Icon(Icons.error_outline, color: AppColors.red, size: 48),
          const SizedBox(height: 12),
          Text(_error,
              textAlign: TextAlign.center,
              style:
                  const TextStyle(color: AppColors.gray5, fontSize: 13)),
          const SizedBox(height: 20),
          ElevatedButton(
            onPressed: _load,
            child: const Text('Retry'),
          ),
          const SizedBox(height: 8),
          TextButton(
            onPressed: () => Navigator.pop(context),
            child: const Text('← Go Back'),
          ),
        ]),
      ),
    );
  }

  Widget _buildDetail() {
    final v        = _vehicle!;
    final imageUrl = v['imageUrl'] as String? ?? '';
    final inStock  = v['stockAvailable'] as bool? ?? false;
    final areaStock =
        v['areaStock'] as Map<String, dynamic>?;
    final isInStock = areaStock != null
        ? (areaStock['stockAvailable'] as bool? ?? false)
        : inStock;

    String stockQtyDisplay;
    if (areaStock != null) {
      stockQtyDisplay = areaStock['stockAvailable'] == true
          ? '${areaStock['stockQuantity']} unit(s) in ${areaStock['areaName']}'
          : 'Out of stock in ${areaStock['areaName']}';
    } else {
      stockQtyDisplay = inStock ? 'In Stock' : 'Out of Stock';
    }

    final specs = <Map<String, String>>[
      if (v['maxPowerKw']      != null) {'icon': '⚡',  'label': 'Max Power',      'value': '${v['maxPowerKw']} kW'},
      if (v['batterySpecs']    != null) {'icon': '🔋',  'label': 'Battery',        'value': '${v['batterySpecs']}'},
      if (v['rangeKm']         != null) {'icon': '📍',  'label': 'Range',          'value': '${v['rangeKm']} km'},
      if (v['chargingTimeHrs'] != null) {'icon': '⏱️', 'label': 'Charging Time',  'value': '${v['chargingTimeHrs']}'},
      if (v['wheelSize']       != null) {'icon': '🛞',  'label': 'Wheel Size',     'value': '${v['wheelSize']}'},
      if (v['wheelType']       != null) {'icon': '🔩',  'label': 'Wheel Type',     'value': '${v['wheelType']}'},
      if (v['brakeFront']      != null) {'icon': '🛑',  'label': 'Front Brake',    'value': '${v['brakeFront']}'},
      if (v['brakeRear']       != null) {'icon': '🛑',  'label': 'Rear Brake',     'value': '${v['brakeRear']}'},
      if (v['brakingType']     != null) {'icon': '🔐',  'label': 'Braking Type',   'value': '${v['brakingType']}'},
      if (v['startingType']    != null) {'icon': '🚀',  'label': 'Starting',       'value': '${v['startingType']}'},
      if (v['speedometer']     != null) {'icon': '🎛️', 'label': 'Speedometer',    'value': '${v['speedometer']}'},
      if (v['colourName']      != null) {'icon': '🎨',  'label': 'Colour',         'value': '${v['colourName']}'},
    ];

    final greatThings = _buildGreatThings(v);

    return CustomScrollView(
      slivers: [
        // ── Hero image ──────────────────────────────────────
        SliverAppBar(
          expandedHeight: 260,
          pinned: true,
          backgroundColor: AppColors.navy,
          leading: IconButton(
            icon: const Icon(Icons.arrow_back, color: Colors.white),
            onPressed: () => Navigator.pop(context),
          ),
          flexibleSpace: FlexibleSpaceBar(
            background: imageUrl.isNotEmpty
                ? Image.network(
                    imageUrl,
                    fit: BoxFit.cover,
                    loadingBuilder: (_, child, progress) => progress == null
                        ? child
                        : Container(
                            color: AppColors.gray2,
                            child: const Center(
                                child: CircularProgressIndicator(
                                    color: AppColors.gold))),
                    errorBuilder: (_, __, ___) => _placeholderImage(),
                  )
                : _placeholderImage(),
          ),
        ),

        SliverToBoxAdapter(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              // ── Breadcrumb ─────────────────────────────────
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                child: Text(
                  'Home / BGauss / ${v['modelName']} / ${v['variantName']}'
                  '${areaStock != null ? ' · 📍 ${areaStock['areaName']} (${areaStock['pincode']})' : ''}',
                  style: const TextStyle(
                      color: AppColors.gray4, fontSize: 11),
                ),
              ),

              // ── Like / Share row ───────────────────────────
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 10, 16, 0),
                child: Row(children: [
                  // Like
                  GestureDetector(
                    onTap: _handleLike,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: _isLiked
                            ? const Color(0xFFFEE2E2)
                            : AppColors.gray2,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(
                            color: _isLiked
                                ? AppColors.red.withValues(alpha: 0.4)
                                : AppColors.gray3),
                      ),
                      child: Row(mainAxisSize: MainAxisSize.min, children: [
                        Icon(
                          _isLiked
                              ? Icons.favorite
                              : Icons.favorite_border,
                          color: _isLiked ? AppColors.red : AppColors.gray5,
                          size: 16,
                        ),
                        if (_likeCount > 0) ...[
                          const SizedBox(width: 4),
                          Text('$_likeCount',
                              style: TextStyle(
                                  color: _isLiked
                                      ? AppColors.red
                                      : AppColors.gray5,
                                  fontSize: 12,
                                  fontWeight: FontWeight.w700)),
                        ],
                      ]),
                    ),
                  ),
                  const SizedBox(width: 8),
                  // Share
                  GestureDetector(
                    onTap: _handleShare,
                    child: Container(
                      padding: const EdgeInsets.symmetric(
                          horizontal: 12, vertical: 8),
                      decoration: BoxDecoration(
                        color: AppColors.gray2,
                        borderRadius: BorderRadius.circular(20),
                        border: Border.all(color: AppColors.gray3),
                      ),
                      child: Row(mainAxisSize: MainAxisSize.min, children: [
                        const Icon(Icons.share_outlined,
                            color: AppColors.gray5, size: 16),
                        const SizedBox(width: 4),
                        Text(
                          _shareMsg.isNotEmpty ? _shareMsg : 'Share',
                          style: TextStyle(
                              color: _shareMsg.isNotEmpty
                                  ? AppColors.green
                                  : AppColors.gray5,
                              fontSize: 12,
                              fontWeight: FontWeight.w600),
                        ),
                      ]),
                    ),
                  ),
                  // ── EMI BUTTON COMMENTED OUT ─────────────────
                  // const SizedBox(width: 8),
                  // GestureDetector(
                  //   onTap: () => setState(() => _emiModalOpen = true),
                  //   child: Container(
                  //     padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 8),
                  //     decoration: BoxDecoration(
                  //       color: AppColors.navy,
                  //       borderRadius: BorderRadius.circular(20),
                  //     ),
                  //     child: const Row(mainAxisSize: MainAxisSize.min, children: [
                  //       Icon(Icons.location_on_outlined, color: Colors.white, size: 14),
                  //       SizedBox(width: 4),
                  //       Text('On Road Price', style: TextStyle(color: Colors.white, fontSize: 11, fontWeight: FontWeight.w700)),
                  //     ]),
                  //   ),
                  // ),
                ]),
              ),

              // ── Model name + stock badge ───────────────────
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 16, 16, 0),
                child: Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(v['modelName'] ?? '',
                                  style: const TextStyle(
                                      fontSize: 22,
                                      fontWeight: FontWeight.w900,
                                      color: AppColors.gray7)),
                              const SizedBox(height: 4),
                              Text(v['variantName'] ?? '',
                                  style: const TextStyle(
                                      fontSize: 15,
                                      color: AppColors.gray5)),
                              const SizedBox(height: 4),
                              Text(
                                'Colour: ${v['colourName'] ?? 'Not specified'}',
                                style: const TextStyle(
                                    fontSize: 12, color: AppColors.gray4),
                              ),
                            ]),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                            horizontal: 12, vertical: 6),
                        decoration: BoxDecoration(
                          color: (isInStock ? AppColors.green : AppColors.red)
                              .withValues(alpha: 0.1),
                          borderRadius: BorderRadius.circular(20),
                          border: Border.all(
                              color: (isInStock
                                      ? AppColors.green
                                      : AppColors.red)
                                  .withValues(alpha: 0.3)),
                        ),
                        child: Text(
                          isInStock ? '✓ In Stock' : '✗ Out of Stock',
                          style: TextStyle(
                              color: isInStock
                                  ? AppColors.green
                                  : AppColors.red,
                              fontWeight: FontWeight.w700,
                              fontSize: 12),
                        ),
                      ),
                    ]),
              ),

              // ── Stock qty ──────────────────────────────────
              Padding(
                padding: const EdgeInsets.fromLTRB(16, 6, 16, 0),
                child: Text(
                  stockQtyDisplay,
                  style: TextStyle(
                      color: isInStock ? AppColors.green : AppColors.red,
                      fontWeight: FontWeight.w600,
                      fontSize: 13),
                ),
              ),

              if (areaStock != null)
                _buildAreaStockBanner(areaStock),

              // ── Price card ─────────────────────────────────
              if (v['price'] != null)
                Container(
                  margin: const EdgeInsets.fromLTRB(16, 14, 16, 0),
                  padding: const EdgeInsets.all(16),
                  decoration: BoxDecoration(
                    gradient: const LinearGradient(
                      colors: [Color(0xFF064E3B), Color(0xFF14532D)],
                      begin: Alignment.topLeft,
                      end: Alignment.bottomRight,
                    ),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Row(children: [
                    const Expanded(
                      child: Text('Ex-Showroom Price',
                          style: TextStyle(
                              color: Colors.white60, fontSize: 12)),
                    ),
                    Text(
                      _fmtPrice(v['price']),
                      style: const TextStyle(
                          color: Colors.white,
                          fontSize: 22,
                          fontWeight: FontWeight.w900),
                    ),
                  ]),
                ),

              // ── Quick chips ────────────────────────────────
              if (v['rangeKm'] != null || v['batterySpecs'] != null)
                Padding(
                  padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
                  child: Wrap(spacing: 8, runSpacing: 6, children: [
                    if (v['rangeKm'] != null)
                      _chip('🛣 ${v['rangeKm']} km range',
                          AppColors.blue),
                    if (v['batterySpecs'] != null)
                      _chip('🔋 ${v['batterySpecs']}', AppColors.green),
                  ]),
                ),

              // ── Review bar ─────────────────────────────────
              _buildReviewBar(v),

              // ── GET EMI OFFERS BUTTON — COMMENTED OUT ─────
              // Padding(
              //   padding: const EdgeInsets.fromLTRB(16, 12, 16, 0),
              //   child: GestureDetector(
              //     onTap: () => setState(() => _emiModalOpen = true),
              //     child: Container(
              //       width: double.infinity,
              //       padding: const EdgeInsets.symmetric(vertical: 14),
              //       decoration: BoxDecoration(
              //         gradient: const LinearGradient(colors: [AppColors.navy, AppColors.navyMid]),
              //         borderRadius: BorderRadius.circular(12),
              //       ),
              //       child: const Row(
              //         mainAxisAlignment: MainAxisAlignment.center,
              //         children: [
              //           Icon(Icons.credit_card_outlined, color: Colors.white, size: 18),
              //           SizedBox(width: 8),
              //           Text('Get EMI Offers', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w800, fontSize: 15)),
              //         ],
              //       ),
              //     ),
              //   ),
              // ),

              // ── Specifications ─────────────────────────────
              if (specs.isNotEmpty) ...[
                const Padding(
                  padding: EdgeInsets.fromLTRB(16, 24, 16, 10),
                  child: Text('Specifications',
                      style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                          color: AppColors.gray7)),
                ),
                Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12),
                  child: GridView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      crossAxisSpacing: 8,
                      mainAxisSpacing: 8,
                      childAspectRatio: 2.5,
                    ),
                    itemCount: specs.length,
                    itemBuilder: (_, i) => _specCard(specs[i]),
                  ),
                ),
              ],

              // ── Great things ───────────────────────────────
              if (greatThings.isNotEmpty) ...[
                const Padding(
                  padding: EdgeInsets.fromLTRB(16, 24, 16, 10),
                  child: Text(
                      'Great things about this scooter',
                      style: TextStyle(
                          fontSize: 17,
                          fontWeight: FontWeight.w800,
                          color: AppColors.gray7)),
                ),
                ...greatThings.map((item) => _greatThingTile(item)),
              ],

              // ── Variants ───────────────────────────────────
              if (_variants.isNotEmpty) ...[
                Padding(
                  padding:
                      const EdgeInsets.fromLTRB(16, 24, 16, 10),
                  child: Row(children: [
                    const Text('Variants',
                        style: TextStyle(
                            fontSize: 17,
                            fontWeight: FontWeight.w800,
                            color: AppColors.gray7)),
                    const SizedBox(width: 8),
                    Text(
                      '${_variants.length} option(s)',
                      style: const TextStyle(
                          color: AppColors.gray4, fontSize: 12),
                    ),
                  ]),
                ),
                Padding(
                  padding:
                      const EdgeInsets.symmetric(horizontal: 12),
                  child: GridView.builder(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    gridDelegate:
                        const SliverGridDelegateWithFixedCrossAxisCount(
                      crossAxisCount: 2,
                      crossAxisSpacing: 10,
                      mainAxisSpacing: 10,
                      childAspectRatio: 2.2,
                    ),
                    itemCount: _variants.length,
                    itemBuilder: (_, i) {
                      final item = _variants[i];
                      final isSelected =
                          item['scootyId'] == widget.scootyId;
                      return GestureDetector(
                        onTap: isSelected
                            ? null
                            : () {
                                Navigator.pushReplacement(
                                  context,
                                  MaterialPageRoute(
                                    builder: (_) => VehicleDetailScreen(
                                      scootyId: item['scootyId'],
                                      pincode: widget.pincode,
                                      cityId: widget.cityId,
                                    ),
                                  ),
                                );
                              },
                        child: Container(
                          padding: const EdgeInsets.all(10),
                          decoration: BoxDecoration(
                            color: isSelected
                                ? AppColors.navy
                                : Colors.white,
                            borderRadius:
                                BorderRadius.circular(12),
                            border: Border.all(
                                color: isSelected
                                    ? AppColors.navy
                                    : AppColors.gray3,
                                width: isSelected ? 2 : 1),
                          ),
                          child: Column(
                              crossAxisAlignment:
                                  CrossAxisAlignment.start,
                              mainAxisAlignment:
                                  MainAxisAlignment.center,
                              children: [
                                Text(
                                    item['variantName'] ?? '',
                                    style: TextStyle(
                                        fontWeight: FontWeight.w800,
                                        fontSize: 13,
                                        color: isSelected
                                            ? Colors.white
                                            : AppColors.gray7)),
                                Text(
                                    _fmtPrice(item['price']),
                                    style: TextStyle(
                                        fontSize: 11,
                                        color: isSelected
                                            ? Colors.white70
                                            : AppColors.gray5)),
                                Text(
                                    item['stockAvailable'] == true
                                        ? 'Available now'
                                        : 'Unavailable',
                                    style: TextStyle(
                                        fontSize: 10,
                                        color: isSelected
                                            ? Colors.white60
                                            : (item['stockAvailable'] == true
                                                ? AppColors.green
                                                : AppColors.red))),
                              ]),
                        ),
                      );
                    },
                  ),
                ),
              ],

              const SizedBox(height: 40),
            ],
          ),
        ),
      ],
    );
  }

  // ── Review bar ──────────────────────────────────────────────
  Widget _buildReviewBar(Map<String, dynamic> v) {
    return Padding(
      padding: const EdgeInsets.fromLTRB(16, 14, 16, 0),
      child: Container(
        padding: const EdgeInsets.symmetric(
            horizontal: 14, vertical: 10),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppColors.gray3),
        ),
        child: Row(children: [
          if (_reviewSummary != null) ...[
            Text(
              '${(_reviewSummary!['averageRating'] as num).toStringAsFixed(1)}/5',
              style: const TextStyle(
                  fontWeight: FontWeight.w900,
                  fontSize: 16,
                  color: AppColors.gray7),
            ),
            const SizedBox(width: 6),
            Row(
              children: List.generate(5, (i) {
                final avg = (_reviewSummary!['averageRating'] as num)
                    .toDouble();
                return Icon(
                  i < avg.round()
                      ? Icons.star
                      : Icons.star_border,
                  color: AppColors.goldDark,
                  size: 14,
                );
              }),
            ),
            const SizedBox(width: 6),
            GestureDetector(
              onTap: () {},
              child: Text(
                '${_reviewSummary!['totalReviews']} review(s)',
                style: const TextStyle(
                    color: AppColors.blue,
                    fontSize: 12,
                    decoration: TextDecoration.underline),
              ),
            ),
          ] else
            const Text('No reviews yet',
                style: TextStyle(
                    color: AppColors.gray4, fontSize: 13)),
          const Spacer(),
          GestureDetector(
            onTap: () => _showReviewModal(context, v),
            child: Container(
              padding: const EdgeInsets.symmetric(
                  horizontal: 10, vertical: 6),
              decoration: BoxDecoration(
                color: AppColors.navy,
                borderRadius: BorderRadius.circular(8),
              ),
              child: const Text('✏️ Write Review',
                  style: TextStyle(
                      color: Colors.white,
                      fontSize: 11,
                      fontWeight: FontWeight.w700)),
            ),
          ),
        ]),
      ),
    );
  }

  void _showReviewModal(BuildContext context, Map<String, dynamic> v) {
    int rating = 0;
    final titleCtrl = TextEditingController();
    final textCtrl  = TextEditingController();
    String msg = '';
    bool submitting = false;

    showModalBottomSheet(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (_) => StatefulBuilder(
        builder: (ctx, setModal) => Container(
          height: MediaQuery.of(context).viewInsets.bottom +
              MediaQuery.of(context).size.height * 0.85,
          decoration: const BoxDecoration(
            color: Colors.white,
            borderRadius:
                BorderRadius.vertical(top: Radius.circular(20)),
          ),
          child: Column(children: [
            Container(
              margin: const EdgeInsets.only(top: 12),
              width: 40, height: 4,
              decoration: BoxDecoration(
                  color: AppColors.gray3,
                  borderRadius: BorderRadius.circular(2)),
            ),
            Padding(
              padding: const EdgeInsets.symmetric(
                  horizontal: 20, vertical: 14),
              child: Row(children: [
                const Text('Write Your Review',
                    style: TextStyle(
                        fontSize: 18,
                        fontWeight: FontWeight.w800,
                        color: AppColors.gray7)),
                const Spacer(),
                GestureDetector(
                    onTap: () => Navigator.pop(ctx),
                    child: const Icon(Icons.close,
                        color: AppColors.gray5)),
              ]),
            ),
            const Divider(height: 1),
            Expanded(
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  const Text('Overall Rating *',
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: AppColors.gray7)),
                  const SizedBox(height: 8),
                  // ── FIXED: Rating stars use StatefulBuilder so they update ─
                  Row(
                    children: List.generate(5, (i) {
                      return GestureDetector(
                        onTap: () => setModal(() => rating = i + 1),
                        child: Padding(
                          padding:
                              const EdgeInsets.only(right: 4),
                          child: Icon(
                            i < rating
                                ? Icons.star
                                : Icons.star_border,
                            color: AppColors.goldDark,
                            size: 36,
                          ),
                        ),
                      );
                    }),
                  ),
                  const SizedBox(height: 14),
                  const Text('Review Title *',
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: AppColors.gray7,
                          fontSize: 13)),
                  const SizedBox(height: 6),
                  TextField(
                    controller: titleCtrl,
                    decoration: InputDecoration(
                      hintText: 'e.g. Innovative and Powerful',
                      hintStyle: const TextStyle(
                          color: AppColors.gray4, fontSize: 13),
                      border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(10),
                          borderSide: const BorderSide(
                              color: AppColors.gray3)),
                      filled: true,
                      fillColor: AppColors.gray1,
                      isDense: true,
                      contentPadding:
                          const EdgeInsets.symmetric(
                              horizontal: 14, vertical: 13),
                    ),
                  ),
                  const SizedBox(height: 14),
                  const Text('Your Review *',
                      style: TextStyle(
                          fontWeight: FontWeight.w700,
                          color: AppColors.gray7,
                          fontSize: 13)),
                  const SizedBox(height: 6),
                  TextField(
                    controller: textCtrl,
                    maxLines: 4,
                    decoration: InputDecoration(
                      hintText: 'Share your experience…',
                      hintStyle: const TextStyle(
                          color: AppColors.gray4, fontSize: 13),
                      border: OutlineInputBorder(
                          borderRadius: BorderRadius.circular(10),
                          borderSide: const BorderSide(
                              color: AppColors.gray3)),
                      filled: true,
                      fillColor: AppColors.gray1,
                      isDense: true,
                      contentPadding: const EdgeInsets.all(14),
                    ),
                  ),
                  if (msg.isNotEmpty)
                    Container(
                      margin: const EdgeInsets.only(top: 10),
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: msg.startsWith('✅')
                            ? const Color(0xFFDCFCE7)
                            : const Color(0xFFFEE2E2),
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Text(msg,
                          style: TextStyle(
                              color: msg.startsWith('✅')
                                  ? AppColors.green
                                  : AppColors.red,
                              fontSize: 13)),
                    ),
                  const SizedBox(height: 16),
                  Row(children: [
                    Expanded(
                      child: OutlinedButton(
                        onPressed: () => Navigator.pop(ctx),
                        style: OutlinedButton.styleFrom(
                          side: const BorderSide(
                              color: AppColors.gray3),
                          padding:
                              const EdgeInsets.symmetric(
                                  vertical: 14),
                          shape: RoundedRectangleBorder(
                              borderRadius:
                                  BorderRadius.circular(12)),
                        ),
                        child: const Text('Cancel',
                            style: TextStyle(
                                color: AppColors.gray5)),
                      ),
                    ),
                    const SizedBox(width: 12),
                    Expanded(
                      child: ElevatedButton(
                        onPressed: submitting
                            ? null
                            : () async {
                                if (rating == 0) {
                                  setModal(() => msg =
                                      'Please select a rating.');
                                  return;
                                }
                                if (titleCtrl.text.trim().length < 5) {
                                  setModal(() => msg =
                                      'Title must be ≥ 5 chars.');
                                  return;
                                }
                                if (textCtrl.text.trim().length < 10) {
                                  setModal(() => msg =
                                      'Review must be ≥ 10 chars.');
                                  return;
                                }
                                setModal(() => submitting = true);
                                try {
                                  await ApiService.submitReview({
                                    'scootyId': widget.scootyId,
                                    'userId':   _username,
                                    'title':    titleCtrl.text.trim(),
                                    'reviewText': textCtrl.text.trim(),
                                    'rating':   rating,
                                  });
                                  setModal(() =>
                                      msg = '✅ Review submitted!');
                                  await Future.delayed(
                                      const Duration(
                                          milliseconds: 1200));
                                  if (ctx.mounted)
                                    Navigator.pop(ctx);
                                  _load();
                                } catch (e) {
                                  setModal(() => msg = e
                                      .toString()
                                      .replaceFirst(
                                          'Exception: ', ''));
                                } finally {
                                  setModal(() => submitting = false);
                                }
                              },
                        style: ElevatedButton.styleFrom(
                          backgroundColor: AppColors.navy,
                          padding:
                              const EdgeInsets.symmetric(
                                  vertical: 14),
                          shape: RoundedRectangleBorder(
                              borderRadius:
                                  BorderRadius.circular(12)),
                        ),
                        child: submitting
                            ? const SizedBox(
                                width: 20, height: 20,
                                child: CircularProgressIndicator(
                                    strokeWidth: 2,
                                    color: Colors.white))
                            : const Text('Submit →',
                                style: TextStyle(
                                    color: Colors.white,
                                    fontWeight: FontWeight.w700)),
                      ),
                    ),
                  ]),
                ],
              ),
            ),
          ]),
        ),
      ),
    );
  }

  Widget _buildAreaStockBanner(Map<String, dynamic> area) {
    final available = area['stockAvailable'] as bool? ?? false;
    final qty = area['stockQuantity'] as int? ?? 0;
    return Container(
      margin: const EdgeInsets.fromLTRB(16, 12, 16, 0),
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: available
            ? const Color(0xFFDCFCE7)
            : const Color(0xFFFEE2E2),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(
            color: (available ? AppColors.green : AppColors.red)
                .withValues(alpha: 0.3)),
      ),
      child: Row(children: [
        Icon(Icons.location_on_outlined,
            color: available ? AppColors.green : AppColors.red,
            size: 16),
        const SizedBox(width: 8),
        Expanded(
          child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  '${area['cityName']} · ${area['areaName']}',
                  style: TextStyle(
                      color: available
                          ? AppColors.green
                          : AppColors.red,
                      fontWeight: FontWeight.w700,
                      fontSize: 13),
                ),
                Text(
                  available
                      ? '$qty unit${qty != 1 ? 's' : ''} available'
                      : 'Out of stock in this area',
                  style: TextStyle(
                      color: (available
                              ? AppColors.green
                              : AppColors.red)
                          .withValues(alpha: 0.7),
                      fontSize: 11),
                ),
              ]),
        ),
      ]),
    );
  }

  Widget _placeholderImage() => Container(
        color: AppColors.gray2,
        child: const Center(
          child: Icon(Icons.electric_scooter,
              color: AppColors.gray4, size: 80),
        ),
      );

  Widget _chip(String label, Color color) => Container(
        padding: const EdgeInsets.symmetric(
            horizontal: 12, vertical: 6),
        decoration: BoxDecoration(
          color: color.withValues(alpha: 0.1),
          borderRadius: BorderRadius.circular(20),
          border: Border.all(color: color.withValues(alpha: 0.2)),
        ),
        child: Text(label,
            style: TextStyle(
                color: color,
                fontWeight: FontWeight.w600,
                fontSize: 13)),
      );

  Widget _specCard(Map<String, String> spec) => Container(
        padding: const EdgeInsets.symmetric(
            horizontal: 12, vertical: 8),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: AppColors.gray3),
        ),
        child: Row(children: [
          Text(spec['icon']!, style: const TextStyle(fontSize: 16)),
          const SizedBox(width: 8),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                Text(spec['label']!,
                    style: const TextStyle(
                        fontSize: 10,
                        color: AppColors.gray4,
                        fontWeight: FontWeight.w600)),
                Text(spec['value']!,
                    style: const TextStyle(
                        fontSize: 12,
                        color: AppColors.gray7,
                        fontWeight: FontWeight.w700),
                    maxLines: 1,
                    overflow: TextOverflow.ellipsis),
              ],
            ),
          ),
        ]),
      );

  Widget _greatThingTile(Map<String, String> item) => Container(
        margin: const EdgeInsets.fromLTRB(16, 0, 16, 8),
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(12),
          border: Border.all(color: AppColors.gray3),
        ),
        child: Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Container(
                width: 36, height: 36,
                decoration: BoxDecoration(
                    color: AppColors.gray2,
                    borderRadius: BorderRadius.circular(10)),
                alignment: Alignment.center,
                child: Text(item['icon']!,
                    style: const TextStyle(fontSize: 18)),
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(item['name']!,
                          style: const TextStyle(
                              fontWeight: FontWeight.w800,
                              fontSize: 14,
                              color: AppColors.gray7)),
                      const SizedBox(height: 2),
                      Text(item['desc']!,
                          style: const TextStyle(
                              fontSize: 12,
                              color: AppColors.gray5,
                              height: 1.4)),
                    ]),
              ),
            ]),
      );

  List<Map<String, String>> _buildGreatThings(
      Map<String, dynamic> v) {
    final list = <Map<String, String>>[];
    if (v['maxPowerKw'] != null) {
      list.add({
        'icon': '⚡',
        'name': 'Max Power',
        'desc':
            'Delivers ${v['maxPowerKw']} kW of powerful, silent electric performance.',
      });
    }
    if (v['batterySpecs'] != null) {
      list.add({
        'icon': '🔋',
        'name': 'Battery',
        'desc':
            'Equipped with a ${v['batterySpecs']} battery for long-lasting rides.',
      });
    }
    if (v['rangeKm'] != null) {
      list.add({
        'icon': '📍',
        'name': 'Range',
        'desc':
            'Ride up to ${v['rangeKm']} km on a single full charge.',
      });
    }
    if (v['chargingTimeHrs'] != null) {
      list.add({
        'icon': '⏱️',
        'name': 'Charging Time',
        'desc':
            'Fully charges in just ${v['chargingTimeHrs']} — plug in overnight and go.',
      });
    }
    final front     = v['brakeFront']  as String?;
    final rear      = v['brakeRear']   as String?;
    final brakeType = v['brakingType'] as String?;
    String brakeDesc;
    if (front != null && rear != null && brakeType != null) {
      brakeDesc =
          '$front front & $rear rear with $brakeType for superior safety.';
    } else if (front != null && rear != null) {
      brakeDesc =
          '$front front & $rear rear brakes for confident stopping.';
    } else if (brakeType != null) {
      brakeDesc =
          '$brakeType braking system for reliable, safe stopping power.';
    } else {
      brakeDesc = 'Advanced braking system for safe, confident rides.';
    }
    list.add({'icon': '🛑', 'name': 'Braking System', 'desc': brakeDesc});

    final size = v['wheelSize'] as String?;
    final type = v['wheelType'] as String?;
    String wheelDesc;
    if (size != null && type != null) {
      wheelDesc = '$size $type wheels for a stable, comfortable ride.';
    } else if (size != null) {
      wheelDesc = '$size wheels built for urban comfort and grip.';
    } else if (type != null) {
      wheelDesc = '$type wheels for improved handling and style.';
    } else {
      wheelDesc = 'Purpose-built wheels for urban riding comfort.';
    }
    list.add({'icon': '🛞', 'name': 'Wheels', 'desc': wheelDesc});
    return list;
  }
}