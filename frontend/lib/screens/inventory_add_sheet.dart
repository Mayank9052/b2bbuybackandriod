// lib/screens/inventory_add_sheet.dart
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class InventoryAddSheet extends StatefulWidget {
  final VoidCallback onAdded;
  const InventoryAddSheet({super.key, required this.onAdded});
  @override
  State<InventoryAddSheet> createState() => _InventoryAddSheetState();
}

class _InventoryAddSheetState extends State<InventoryAddSheet> {
  List<Map<String, dynamic>> _models   = [];
  List<Map<String, dynamic>> _variants = [];
  List<Map<String, dynamic>> _colours  = [];

  int?    _modelId, _variantId, _colourId;
  final   _priceCtrl   = TextEditingController();
  final   _rangeCtrl   = TextEditingController();
  final   _batteryCtrl = TextEditingController();
  bool    _stockAvail  = true;
  bool    _submitting  = false;
  String  _msg         = '';

  @override
  void initState() {
    super.initState();
    _loadModels();
  }

  @override
  void dispose() {
    _priceCtrl.dispose(); _rangeCtrl.dispose(); _batteryCtrl.dispose();
    super.dispose();
  }

  Future<void> _loadModels() async {
    try {
      final m = await ApiService.getModels();
      setState(() => _models = m);
    } catch (_) {}
  }

  Future<void> _loadVariants(int modelId) async {
    try {
      final v = await ApiService.getVariants(modelId);
      setState(() { _variants = v; _variantId = null; _colours = []; _colourId = null; });
    } catch (_) {}
  }

  Future<void> _loadColours(int modelId, int variantId) async {
    try {
      final c = await ApiService.getColours(modelId, variantId);
      setState(() { _colours = c; _colourId = null; });
    } catch (_) {}
  }

  Future<void> _submit() async {
    if (_modelId == null || _variantId == null) {
      setState(() => _msg = 'Model and Variant are required.');
      return;
    }
    setState(() { _submitting = true; _msg = ''; });
    try {
      await ApiService.addInventoryItem({
        'modelId':        _modelId,
        'variantId':      _variantId,
        if (_colourId != null) 'colourId': _colourId,
        if (_priceCtrl.text.isNotEmpty)   'price':        _priceCtrl.text,
        if (_rangeCtrl.text.isNotEmpty)   'rangeKm':      _rangeCtrl.text,
        if (_batteryCtrl.text.isNotEmpty) 'batterySpecs': _batteryCtrl.text,
        'stockAvailable': _stockAvail,
      });
      setState(() => _msg = '✅ Item added successfully!');
      widget.onAdded();
      await Future.delayed(const Duration(milliseconds: 1200));
      if (mounted) Navigator.pop(context);
    } catch (e) {
      setState(() => _msg = '❌ ${e.toString()}');
    } finally {
      setState(() => _submitting = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return DraggableScrollableSheet(
      initialChildSize: 0.9,
      maxChildSize: 0.95,
      minChildSize: 0.5,
      builder: (_, ctrl) => Container(
        decoration: const BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
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
            padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
            child: Row(children: [
              const Text('Add Inventory Item', style: TextStyle(fontSize: 18, fontWeight: FontWeight.w800, color: AppColors.gray7)),
              const Spacer(),
              GestureDetector(onTap: () => Navigator.pop(context), child: const Icon(Icons.close, color: AppColors.gray5)),
            ]),
          ),
          const Divider(height: 1),
          // Body
          Expanded(
            child: ListView(controller: ctrl, padding: const EdgeInsets.all(20), children: [
              _label('Model *'),
              _dropdown(
                value: _modelId,
                items: _models.map((m) => DropdownMenuItem(value: m['id'] as int, child: Text(m['modelName']))).toList(),
                hint: 'Select Model',
                onChanged: (v) { setState(() => _modelId = v); if (v != null) _loadVariants(v); },
              ),
              const SizedBox(height: 14),
              _label('Variant *'),
              _dropdown(
                value: _variantId,
                items: _variants.map((v) => DropdownMenuItem(value: v['id'] as int, child: Text(v['variantName']))).toList(),
                hint: 'Select Variant',
                enabled: _variants.isNotEmpty,
                onChanged: (v) {
                  setState(() => _variantId = v);
                  if (v != null && _modelId != null) _loadColours(_modelId!, v);
                },
              ),
              const SizedBox(height: 14),
              _label('Colour'),
              _dropdown(
                value: _colourId,
                items: _colours.map((c) => DropdownMenuItem(value: c['id'] as int, child: Text(c['colourName']))).toList(),
                hint: 'Select Colour',
                enabled: _colours.isNotEmpty,
                onChanged: (v) => setState(() => _colourId = v),
              ),
              const SizedBox(height: 14),
              Row(children: [
                Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  _label('Price (₹)'),
                  _textField(_priceCtrl, 'e.g. 120000', TextInputType.number),
                ])),
                const SizedBox(width: 12),
                Expanded(child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [
                  _label('Range (km)'),
                  _textField(_rangeCtrl, 'e.g. 120', TextInputType.number),
                ])),
              ]),
              const SizedBox(height: 14),
              _label('Battery Specs'),
              _textField(_batteryCtrl, 'e.g. 3.0 kWh Li-ion'),
              const SizedBox(height: 14),
              Row(children: [
                const Text('Stock Available', style: TextStyle(fontWeight: FontWeight.w600, color: AppColors.gray7)),
                const Spacer(),
                Switch(
                  value: _stockAvail,
                  onChanged: (v) => setState(() => _stockAvail = v),
                  activeThumbColor: AppColors.green,
                ),
              ]),
              if (_msg.isNotEmpty)
                Container(
                  margin: const EdgeInsets.only(top: 12),
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 10),
                  decoration: BoxDecoration(
                    color: _msg.startsWith('✅') ? const Color(0xFFDCFCE7) : const Color(0xFFFEE2E2),
                    borderRadius: BorderRadius.circular(10),
                  ),
                  child: Text(_msg, style: TextStyle(
                    color: _msg.startsWith('✅') ? AppColors.green : AppColors.red,
                    fontWeight: FontWeight.w600,
                  )),
                ),
              const SizedBox(height: 24),
              Row(children: [
                Expanded(
                  child: OutlinedButton(
                    onPressed: () => Navigator.pop(context),
                    style: OutlinedButton.styleFrom(
                      side: const BorderSide(color: AppColors.gray3),
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    child: const Text('Cancel', style: TextStyle(color: AppColors.gray5, fontWeight: FontWeight.w600)),
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: ElevatedButton(
                    onPressed: _submitting ? null : _submit,
                    style: ElevatedButton.styleFrom(
                      backgroundColor: AppColors.navy,
                      padding: const EdgeInsets.symmetric(vertical: 14),
                      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                    ),
                    child: _submitting
                        ? const SizedBox(width: 20, height: 20, child: CircularProgressIndicator(strokeWidth: 2, color: Colors.white))
                        : const Text('Submit', style: TextStyle(color: Colors.white, fontWeight: FontWeight.w700)),
                  ),
                ),
              ]),
              const SizedBox(height: 20),
            ]),
          ),
        ]),
      ),
    );
  }

  Widget _label(String t) => Padding(
    padding: const EdgeInsets.only(bottom: 6),
    child: Text(t, style: const TextStyle(fontSize: 13, fontWeight: FontWeight.w700, color: AppColors.gray7)),
  );

  Widget _dropdown<T>({
    required T? value,
    required List<DropdownMenuItem<T>> items,
    required String hint,
    required ValueChanged<T?> onChanged,
    bool enabled = true,
  }) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 2),
      decoration: BoxDecoration(
        color: enabled ? AppColors.gray1 : AppColors.gray2,
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: AppColors.gray3),
      ),
      child: DropdownButtonHideUnderline(
        child: DropdownButton<T>(
          value: value,
          items: items,
          hint: Text(hint, style: const TextStyle(color: AppColors.gray4, fontSize: 13)),
          onChanged: enabled ? onChanged : null,
          isExpanded: true,
          style: const TextStyle(color: AppColors.gray7, fontSize: 14),
          dropdownColor: Colors.white,
          borderRadius: BorderRadius.circular(12),
        ),
      ),
    );
  }

  Widget _textField(TextEditingController ctrl, String hint, [TextInputType? type]) {
    return TextField(
      controller: ctrl,
      keyboardType: type,
      style: const TextStyle(fontSize: 14, color: AppColors.gray7),
      decoration: InputDecoration(hintText: hint, hintStyle: const TextStyle(color: AppColors.gray4, fontSize: 13)),
    );
  }
}