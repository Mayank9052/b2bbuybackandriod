import 'dart:async';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class OtpScreen extends StatefulWidget {
  final String mobile;
  final String dealerCode;
  const OtpScreen({super.key, required this.mobile, required this.dealerCode});

  @override
  State<OtpScreen> createState() => _OtpScreenState();
}

class _OtpScreenState extends State<OtpScreen> {
  final List<TextEditingController> _ctrs = List.generate(6, (_) => TextEditingController());
  final List<FocusNode> _nodes = List.generate(6, (_) => FocusNode());

  bool    _loading   = false;
  bool    _resending = false;
  String? _error;
  int     _countdown = 30;
  Timer?  _timer;

  @override
  void initState() {
    super.initState();
    _startTimer();
    WidgetsBinding.instance.addPostFrameCallback((_) => _nodes[0].requestFocus());
  }

  void _startTimer() {
    _timer?.cancel();
    setState(() => _countdown = 30);
    _timer = Timer.periodic(const Duration(seconds: 1), (t) {
      if (!mounted) { t.cancel(); return; }
      setState(() { if (_countdown > 0) _countdown--; else t.cancel(); });
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    for (final c in _ctrs) c.dispose();
    for (final n in _nodes) n.dispose();
    super.dispose();
  }

  String get _otpValue => _ctrs.map((c) => c.text).join();

  void _onDigitChanged(int index, String value) {
    if (value.length == 6) {
      for (int i = 0; i < 6; i++) _ctrs[i].text = value[i];
      _nodes[5].requestFocus();
      _verify();
      return;
    }
    if (value.isNotEmpty && index < 5) _nodes[index + 1].requestFocus();
    if (value.isEmpty && index > 0)    _nodes[index - 1].requestFocus();
    setState(() {});
    if (_otpValue.length == 6) _verify();
  }

  Future<void> _verify() async {
    if (_loading || _otpValue.length != 6) return;
    setState(() { _loading = true; _error = null; });
    try {
      await ApiService.verifyOtp(
        mobile: widget.mobile, dealerCode: widget.dealerCode, otp: _otpValue,
      );
      if (!mounted) return;
      Navigator.pushNamedAndRemoveUntil(context, '/dashboard', (_) => false);
    } catch (e) {
      setState(() {
        _error = e.toString();
        for (final c in _ctrs) c.clear();
      });
      _nodes[0].requestFocus();
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _resend() async {
    if (_countdown > 0 || _resending) return;
    setState(() { _resending = true; _error = null; });
    try {
      final result = await ApiService.sendOtp(
        mobile: widget.mobile, dealerCode: widget.dealerCode,
      );
      _startTimer();
      if (!mounted) return;
      if (result['devOtp'] != null) {
        ScaffoldMessenger.of(context).showSnackBar(SnackBar(
          content: Text('🔧 Dev OTP: ${result['devOtp']}'),
          backgroundColor: AppColors.amber,
          duration: const Duration(seconds: 8),
        ));
      }
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _resending = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final maskedMobile = '+91 ${widget.mobile.substring(0, 5)}XXXXX';
    return Scaffold(
      backgroundColor: AppColors.navy,
      body: SafeArea(
        child: Column(
          children: [
            // Back button
            Align(
              alignment: Alignment.centerLeft,
              child: IconButton(
                icon: const Icon(Icons.arrow_back_ios_new, color: Colors.white, size: 20),
                onPressed: () => Navigator.pop(context),
              ),
            ),

            Expanded(
              child: SingleChildScrollView(
                padding: const EdgeInsets.symmetric(horizontal: 20),
                child: Column(
                  children: [
                    const SizedBox(height: 16),

                    // Logo
                    Container(
                      width: 64, height: 64,
                      decoration: const BoxDecoration(color: AppColors.gold, shape: BoxShape.circle),
                      alignment: Alignment.center,
                      child: const Text('BG', style: TextStyle(
                        fontSize: 22, fontWeight: FontWeight.w900, color: AppColors.navy,
                      )),
                    ),
                    const SizedBox(height: 24),

                    // Card
                    Container(
                      padding: const EdgeInsets.all(24),
                      decoration: BoxDecoration(
                        color: Colors.white,
                        borderRadius: BorderRadius.circular(20),
                        boxShadow: [BoxShadow(
                          color: Colors.black.withValues(alpha:0.15),
                          blurRadius: 20, offset: const Offset(0, 6),
                        )],
                      ),
                      child: Column(
                        children: [
                          const Text('Verify OTP', style: TextStyle(
                            fontSize: 20, fontWeight: FontWeight.w900, color: AppColors.gray7,
                          )),
                          const SizedBox(height: 8),
                          RichText(
                            textAlign: TextAlign.center,
                            text: TextSpan(
                              style: const TextStyle(fontSize: 13, color: AppColors.gray5),
                              children: [
                                const TextSpan(text: 'OTP sent to '),
                                TextSpan(text: maskedMobile, style: const TextStyle(
                                  fontWeight: FontWeight.w800, color: AppColors.gray7,
                                )),
                              ],
                            ),
                          ),
                          const SizedBox(height: 24),

                          // OTP boxes
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: List.generate(6, (i) => _OtpBox(
                              controller: _ctrs[i],
                              focusNode: _nodes[i],
                              isFilled: _ctrs[i].text.isNotEmpty,
                              maxLength: i == 0 ? 6 : 1,
                              onChanged: (v) => _onDigitChanged(i, v),
                            )),
                          ),

                          // Error
                          if (_error != null) ...[
                            const SizedBox(height: 12),
                            Container(
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                              decoration: BoxDecoration(
                                color: const Color(0xFFFEE2E2),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: const Color(0xFFFCA5A5)),
                              ),
                              child: Row(children: [
                                const Icon(Icons.error_outline, color: AppColors.red, size: 16),
                                const SizedBox(width: 8),
                                Expanded(child: Text(_error!, style: const TextStyle(
                                  fontSize: 12, color: AppColors.red, fontWeight: FontWeight.w600,
                                ))),
                              ]),
                            ),
                          ],

                          const SizedBox(height: 20),

                          // Verify button
                          SizedBox(
                            width: double.infinity,
                            child: ElevatedButton(
                              onPressed: (_loading || _otpValue.length < 6) ? null : _verify,
                              child: _loading
                                  ? const SizedBox(width: 20, height: 20,
                                      child: CircularProgressIndicator(strokeWidth: 2.5, color: AppColors.navy))
                                  : const Text('VERIFY & LOGIN'),
                            ),
                          ),

                          const SizedBox(height: 16),

                          // Resend
                          Row(
                            mainAxisAlignment: MainAxisAlignment.center,
                            children: [
                              Text("Didn't receive OTP? ", style: TextStyle(
                                fontSize: 13, color: AppColors.gray5,
                              )),
                              _countdown > 0
                                  ? Text('Resend in ${_countdown}s', style: const TextStyle(
                                      fontSize: 13, color: AppColors.gray4, fontWeight: FontWeight.w600,
                                    ))
                                  : GestureDetector(
                                      onTap: _resend,
                                      child: _resending
                                          ? const SizedBox(width: 14, height: 14,
                                              child: CircularProgressIndicator(strokeWidth: 2, color: AppColors.gold))
                                          : const Text('Resend', style: TextStyle(
                                              fontSize: 13, color: AppColors.goldDark, fontWeight: FontWeight.w800,
                                            )),
                                    ),
                            ],
                          ),
                        ],
                      ),
                    ),

                    const SizedBox(height: 20),

                    // Dealer pill
                    Container(
                      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 7),
                      decoration: BoxDecoration(
                        color: Colors.white.withValues(alpha: 0.08),
                        borderRadius: BorderRadius.circular(999),
                        border: Border.all(color: Colors.white.withValues(alpha: 0.12)),
                      ),
                      child: Row(mainAxisSize: MainAxisSize.min, children: [
                        const Icon(Icons.store_outlined, color: AppColors.gold, size: 14),
                        const SizedBox(width: 6),
                        Text(widget.dealerCode, style: const TextStyle(
                          fontSize: 12, fontWeight: FontWeight.w700,
                          color: Colors.white, fontFamily: 'monospace',
                        )),
                      ]),
                    ),
                    const SizedBox(height: 24),
                  ],
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OtpBox extends StatelessWidget {
  final TextEditingController controller;
  final FocusNode focusNode;
  final bool isFilled;
  final int maxLength;
  final ValueChanged<String> onChanged;

  const _OtpBox({
    required this.controller, required this.focusNode,
    required this.isFilled, required this.maxLength, required this.onChanged,
  });

  @override
  Widget build(BuildContext context) {
    return Container(
      width: 44, height: 52,
      margin: const EdgeInsets.symmetric(horizontal: 3),
      child: TextFormField(
        controller: controller,
        focusNode: focusNode,
        textAlign: TextAlign.center,
        maxLength: maxLength,
        keyboardType: TextInputType.number,
        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
        style: const TextStyle(fontSize: 20, fontWeight: FontWeight.w900, color: AppColors.gray7),
        decoration: InputDecoration(
          counterText: '',
          filled: true,
          fillColor: isFilled ? const Color(0xFFFEF3C7) : AppColors.gray1,
          contentPadding: const EdgeInsets.symmetric(vertical: 10),
          border: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: BorderSide(color: isFilled ? AppColors.gold : AppColors.gray3,
                width: isFilled ? 2 : 1),
          ),
          enabledBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: BorderSide(color: isFilled ? AppColors.gold : AppColors.gray3,
                width: isFilled ? 2 : 1),
          ),
          focusedBorder: OutlineInputBorder(
            borderRadius: BorderRadius.circular(10),
            borderSide: const BorderSide(color: AppColors.gold, width: 2),
          ),
        ),
        onChanged: onChanged,
      ),
    );
  }
}