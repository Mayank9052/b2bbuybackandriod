// lib/screens/refurbishment_screen.dart
// Matches Section 3.4 in BRD screenshots — checklist with cost cap
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

const _autoItems = [
  {'component': 'Battery',     'action': 'Health check, repair/replace',        'trigger': 'Score < 6', 'mandatory': false},
  {'component': 'Tyres',       'action': 'Replace if tread <2mm or visible damage', 'trigger': 'Score < 5', 'mandatory': false},
  {'component': 'Body & Paint','action': 'Dent removal, scratch repair, panel alignment', 'trigger': 'Score < 6', 'mandatory': false},
  {'component': 'Electricals', 'action': 'Repair non-functional lights, display, charger port', 'trigger': 'Score < 7', 'mandatory': false},
  {'component': 'Brakes',      'action': 'Inspect and service pads & cables',   'trigger': 'All vehicles', 'mandatory': true},
  {'component': 'Cleaning',    'action': 'Full interior + exterior wash and polish', 'trigger': 'All vehicles', 'mandatory': true},
];

const double _costCap = 3000;

class RefurbishmentScreen extends StatefulWidget {
  final int caseId;
  final String caseNumber;
  final String vehicleModel;
  final String vehicleVariant;
  final String registrationNo;
  final double totalScore;

  const RefurbishmentScreen({
    super.key,
    required this.caseId,
    required this.caseNumber,
    required this.vehicleModel,
    required this.vehicleVariant,
    required this.registrationNo,
    required this.totalScore,
  });

  @override
  State<RefurbishmentScreen> createState() => _RefurbishmentScreenState();
}

class _RefurbishmentScreenState extends State<RefurbishmentScreen> {
  Map<String, bool>   _checked   = {};
  Map<String, double> _itemCosts = {};
  bool   _loading = false;
  bool   _submitting = false;
  String _error   = '';
  int    _daysLeft = 7;

  // Cost override
  final _costCtrl = TextEditingController();
  bool  _costOverrideNeeded = false;
  bool  _costOverrideSubmitted = false;

  @override
  void initState() {
    super.initState();
    _loadData();
  }

  @override
  void dispose() { _costCtrl.dispose(); super.dispose(); }

  Future<void> _loadData() async {
    setState(() => _loading = true);
    try {
      final data = await ApiService.getRefurbishmentDetail(widget.caseId);
      final items = List<Map<String, dynamic>>.from(data['items'] ?? []);
      final costs = Map<String, double>.from(
        (data['costs'] as Map<String, dynamic>? ?? {}).map((k, v) => MapEntry(k, (v as num).toDouble()))
      );
      final checked = <String, bool>{};
      for (final item in items) {
        checked[item['component'] as String] = item['done'] as bool? ?? false;
      }
      // Fill defaults for auto items not in response
      for (final a in _autoItems) {
        checked.putIfAbsent(a['component'] as String, () => false);
      }
      setState(() {
        _checked   = checked;
        _itemCosts = costs;
        _daysLeft  = data['daysLeft'] as int? ?? 7;
        _loading   = false;
      });
    } catch (_) {
      // Use defaults
      final checked = <String, bool>{};
      for (final a in _autoItems) { checked[a['component'] as String] = false; }
      setState(() { _checked = checked; _loading = false; });
    }
  }

  double get _totalCost => _itemCosts.values.fold(0, (s, v) => s + v);

  bool get _overCap => _totalCost > _costCap;

  int get _doneCount => _checked.values.where((v) => v).length;
  int get _totalCount => _autoItems.length;
  int get _pendingCount => _totalCount - _doneCount;

  Future<void> _toggleItem(String component, bool value) async {
    setState(() => _checked[component] = value);
    try {
      await ApiService.updateRefurbItem(widget.caseId, component, value);
    } catch (_) {
      setState(() => _checked[component] = !value); // revert
    }
  }

  Future<void> _submitForBgService() async {
    if (_pendingCount > 0) {
      setState(() => _error = 'Please complete all $_pendingCount remaining items first.');
      return;
    }
    if (_overCap && !_costOverrideSubmitted) {
      setState(() => _error = 'Total cost ₹${_totalCost.toStringAsFixed(0)} exceeds cap of ₹${_costCap.toStringAsFixed(0)}. Submit a Cost Override Request first.');
      return;
    }
    setState(() { _submitting = true; _error = ''; });
    try {
      await ApiService.submitForBgService(widget.caseId, _totalCost);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('✅ Submitted for BG Service review!'), backgroundColor: AppColors.green),
        );
        Navigator.pop(context, true);
      }
    } catch (e) {
      setState(() => _error = e.toString().replaceFirst('Exception: ', ''));
    } finally {
      setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    if (_loading) return const Scaffold(body: Center(child: CircularProgressIndicator(color: AppColors.gold)));
    return Scaffold(
      backgroundColor: AppColors.gray1,
      appBar: AppBar(
        backgroundColor: AppColors.navy,
        title: const Text('Refurbishment Tracker', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w800)),
        iconTheme: const IconThemeData(color: Colors.white),
        elevation: 0,
      ),
      body: Column(children: [
        Expanded(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(16),
            child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
              // Vehicle header card
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14), border: Border.all(color: AppColors.gray3)),
                child: Row(children: [
                  Container(width: 56, height: 48, decoration: BoxDecoration(color: AppColors.gray2, borderRadius: BorderRadius.circular(10)),
                      child: const Icon(Icons.electric_scooter, color: AppColors.gray4, size: 28)),
                  const SizedBox(width: 12),
                  Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                    Text('${widget.vehicleModel} ${widget.vehicleVariant}', style: const TextStyle(fontWeight: FontWeight.w800, fontSize: 14, color: AppColors.gray7)),
                    Text('${widget.registrationNo} · Score ${widget.totalScore.toStringAsFixed(1)}', style: const TextStyle(color: AppColors.gray5, fontSize: 12)),
                    const SizedBox(height: 4),
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 8, vertical: 3),
                      decoration: BoxDecoration(color: const Color(0xFFEDE9FE), borderRadius: BorderRadius.circular(8)),
                      child: const Text('REFURBISHMENT', style: TextStyle(color: Color(0xFF7C3AED), fontSize: 9, fontWeight: FontWeight.w800)),
                    ),
                  ])),
                ]),
              ),
              const SizedBox(height: 10),
              // Deadline warning
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: const Color(0xFFFEF3C7), borderRadius: BorderRadius.circular(10), border: Border.all(color: AppColors.gold.withValues(alpha:0.3))),
                child: Row(children: [
                  const Text('⏰', style: TextStyle(fontSize: 14)),
                  const SizedBox(width: 8),
                  Text('$_daysLeft days remaining (of 7-day deadline)', style: const TextStyle(color: AppColors.goldDark, fontWeight: FontWeight.w600, fontSize: 12)),
                ]),
              ),
              const SizedBox(height: 14),
              // Checklist header
              const Text('Refurbishment Checklist', style: TextStyle(fontSize: 16, fontWeight: FontWeight.w800, color: AppColors.gray7)),
              const SizedBox(height: 4),
              const Text('Tick each item as you complete it.', style: TextStyle(color: AppColors.gray5, fontSize: 12)),
              const SizedBox(height: 12),

              // Items
              ..._autoItems.map((item) {
                final comp    = item['component'] as String;
                final action  = item['action']    as String;
                final trigger = item['trigger']   as String;
                final done    = _checked[comp] == true;
                return Container(
                  margin: const EdgeInsets.only(bottom: 8),
                  decoration: BoxDecoration(
                    color: done ? const Color(0xFFDCFCE7) : Colors.white,
                    borderRadius: BorderRadius.circular(12),
                    border: Border.all(color: done ? AppColors.green.withValues(alpha: 0.4) : AppColors.gray3),
                  ),
                  child: CheckboxListTile(
                    value: done,
                    onChanged: (v) => _toggleItem(comp, v ?? false),
                    activeColor: AppColors.green,
                    title: Text(comp, style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14, color: done ? AppColors.green : AppColors.gray7)),
                    subtitle: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                      Text(action, style: const TextStyle(fontSize: 12, color: AppColors.gray5)),
                      const SizedBox(height: 2),
                      Row(children: [
                        Container(
                          padding: const EdgeInsets.symmetric(horizontal: 6, vertical: 2),
                          decoration: BoxDecoration(color: AppColors.gray2, borderRadius: BorderRadius.circular(4)),
                          child: Text(trigger, style: const TextStyle(fontSize: 10, color: AppColors.gray5, fontWeight: FontWeight.w600)),
                        ),
                        const Spacer(),
                        Text(done ? 'Done' : 'Pending', style: TextStyle(color: done ? AppColors.green : AppColors.goldDark, fontSize: 11, fontWeight: FontWeight.w600)),
                      ]),
                    ]),
                    controlAffinity: ListTileControlAffinity.leading,
                    contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 4),
                  ),
                );
              }),
              const SizedBox(height: 16),
              // Cost section
              Container(
                padding: const EdgeInsets.all(14),
                decoration: BoxDecoration(color: Colors.white, borderRadius: BorderRadius.circular(14), border: Border.all(color: AppColors.gray3)),
                child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  const Text('Total Refurb Cost', style: TextStyle(fontWeight: FontWeight.w700, fontSize: 14, color: AppColors.gray7)),
                  const SizedBox(height: 6),
                  Row(children: [
                    Text('₹ ${_totalCost.toStringAsFixed(0)}',
                        style: TextStyle(fontSize: 22, fontWeight: FontWeight.w900, color: _overCap ? AppColors.red : AppColors.green)),
                    const Spacer(),
                    Text('Cap: ₹${_costCap.toStringAsFixed(0)}', style: const TextStyle(color: AppColors.gray4, fontSize: 12)),
                  ]),
                  if (_overCap) ...[
                    const SizedBox(height: 8),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(color: const Color(0xFFFEE2E2), borderRadius: BorderRadius.circular(8)),
                      child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                        const Text('Cost exceeds ₹3,000 cap. Submit a Cost Override Request.', style: TextStyle(color: AppColors.red, fontSize: 12, fontWeight: FontWeight.w600)),
                        const SizedBox(height: 8),
                        Row(children: [
                          Expanded(
                            child: TextField(
                              controller: _costCtrl,
                              keyboardType: TextInputType.number,
                              decoration: const InputDecoration(
                                hintText: 'Enter itemised cost breakdown',
                                isDense: true, contentPadding: EdgeInsets.symmetric(horizontal: 10, vertical: 10),
                                border: OutlineInputBorder(), filled: true, fillColor: Colors.white,
                              ),
                            ),
                          ),
                          const SizedBox(width: 8),
                          ElevatedButton(
                            onPressed: () {
                              setState(() => _costOverrideSubmitted = true);
                              ScaffoldMessenger.of(context).showSnackBar(
                                const SnackBar(content: Text('Cost Override Request submitted'), backgroundColor: AppColors.blue),
                              );
                            },
                            style: ElevatedButton.styleFrom(backgroundColor: AppColors.red, padding: const EdgeInsets.symmetric(vertical: 12, horizontal: 14)),
                            child: const Text('Submit', style: TextStyle(color: Colors.white, fontSize: 12)),
                          ),
                        ]),
                      ]),
                    ),
                  ],
                ]),
              ),
              const SizedBox(height: 8),
              // Cost cap behaviour note
              Container(
                padding: const EdgeInsets.all(12),
                decoration: BoxDecoration(color: const Color(0xFFF0F9FF), borderRadius: BorderRadius.circular(10), border: Border.all(color: const Color(0xFFBAE6FD))),
                child: const Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  Text('Cost Cap Behaviour', style: TextStyle(color: AppColors.blue, fontWeight: FontWeight.w700, fontSize: 12)),
                  SizedBox(height: 4),
                  Text('• Hard cap at ₹3,000 per vehicle\n• If dealer needs to exceed the cap, they must submit a "Cost Override Request"\n• Without admin pre-approval, any cost above ₹3,000 will not be reimbursed at settlement',
                      style: TextStyle(color: AppColors.blue, fontSize: 11, height: 1.5)),
                ]),
              ),
              if (_error.isNotEmpty) ...[
                const SizedBox(height: 12),
                Container(
                  padding: const EdgeInsets.all(12),
                  decoration: BoxDecoration(color: const Color(0xFFFEE2E2), borderRadius: BorderRadius.circular(10)),
                  child: Text(_error, style: const TextStyle(color: AppColors.red, fontSize: 13)),
                ),
              ],
              const SizedBox(height: 80),
            ]),
          ),
        ),
        // Bottom submit bar
        Container(
          padding: const EdgeInsets.all(16),
          decoration: const BoxDecoration(color: Colors.white, border: Border(top: BorderSide(color: AppColors.gray3))),
          child: Column(children: [
            Row(children: [
              Text('$_doneCount/$_totalCount items done', style: const TextStyle(color: AppColors.gray5, fontSize: 12)),
              const Spacer(),
              Text('$_pendingCount remaining', style: TextStyle(color: _pendingCount > 0 ? AppColors.goldDark : AppColors.green, fontWeight: FontWeight.w600, fontSize: 12)),
            ]),
            const SizedBox(height: 10),
            ElevatedButton(
              onPressed: (_submitting || _pendingCount > 0) ? null : _submitForBgService,
              style: ElevatedButton.styleFrom(
                backgroundColor: _pendingCount == 0 ? AppColors.green : AppColors.gray3,
                minimumSize: const Size(double.infinity, 52),
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
              ),
              child: _submitting
                  ? const CircularProgressIndicator(strokeWidth: 2, color: Colors.white)
                  : Text(
                      _pendingCount > 0
                          ? 'Complete $_pendingCount Items to Submit for BG Service'
                          : 'COMPLETE $_totalCount ITEMS TO SUBMIT FOR BG SERVICE',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: _pendingCount == 0 ? Colors.white : AppColors.gray5,
                        fontWeight: FontWeight.w800, fontSize: 13,
                      ),
                    ),
            ),
          ]),
        ),
      ]),
    );
  }
}