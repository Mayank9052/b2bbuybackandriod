// lib/screens/login_screen.dart
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey    = GlobalKey<FormState>();
  final _mobileCtr  = TextEditingController();
  final _codeCtr    = TextEditingController();

  bool _loading = false;
  String? _error;

  @override
  void dispose() {
    _mobileCtr.dispose();
    _codeCtr.dispose();
    super.dispose();
  }

  Future<void> _sendOtp() async {
    if (!_formKey.currentState!.validate()) return;
    setState(() { _loading = true; _error = null; });

    try {
      final result = await ApiService.sendOtp(
        mobile:     _mobileCtr.text.trim(),
        dealerCode: _codeCtr.text.trim(),
      );

      if (!mounted) return;

      // Show dev OTP hint if present
      if (result['devOtp'] != null) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text('🔧 Dev OTP: ${result['devOtp']}'),
            backgroundColor: AppColors.amber,
            duration: const Duration(seconds: 8),
          ),
        );
      }

      Navigator.pushNamed(context, '/otp', arguments: {
        'mobile':     _mobileCtr.text.trim(),
        'dealerCode': _codeCtr.text.trim().toUpperCase(),
      });
    } catch (e) {
      setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.navy,
      body: SafeArea(
        child: SingleChildScrollView(
          child: Column(
            children: [
              // ── Header ──────────────────────────────────────────
              const SizedBox(height: 56),
              Container(
                width: 80, height: 80,
                decoration: const BoxDecoration(
                  color: AppColors.gold,
                  shape: BoxShape.circle,
                ),
                alignment: Alignment.center,
                child: const Text(
                  'BG',
                  style: TextStyle(
                    fontSize: 28, fontWeight: FontWeight.w900,
                    color: AppColors.navy,
                  ),
                ),
              ),
              const SizedBox(height: 20),
              const Text(
                'BGauss PI App',
                style: TextStyle(
                  fontSize: 24, fontWeight: FontWeight.w900,
                  color: Colors.white,
                ),
              ),
              const SizedBox(height: 6),
              Text(
                'Procurement & Inspection',
                style: TextStyle(
                  fontSize: 13, color: Colors.white.withValues(alpha:0.55),
                ),
              ),
              const SizedBox(height: 48),

              // ── Login Card ───────────────────────────────────────
              Container(
                margin: const EdgeInsets.symmetric(horizontal: 20),
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: Colors.white,
                  borderRadius: BorderRadius.circular(20),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withValues(alpha: 0.18),
                      blurRadius: 24, offset: const Offset(0, 8),
                    )
                  ],
                ),
                child: Form(
                  key: _formKey,
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Dealer Login',
                        style: TextStyle(
                          fontSize: 20, fontWeight: FontWeight.w900,
                          color: AppColors.gray7,
                        ),
                      ),
                      const SizedBox(height: 6),
                      Text(
                        'Enter your registered mobile number and dealer code',
                        style: TextStyle(fontSize: 13, color: AppColors.gray5),
                      ),
                      const SizedBox(height: 24),

                      // Mobile number
                      _FieldLabel('Mobile Number'),
                      const SizedBox(height: 6),
                      TextFormField(
                        controller:  _mobileCtr,
                        keyboardType: TextInputType.phone,
                        maxLength:   10,
                        inputFormatters: [FilteringTextInputFormatter.digitsOnly],
                        decoration: InputDecoration(
                          counterText: '',
                          hintText: '10-digit mobile number',
                          border: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(10),
                            borderSide: const BorderSide(color: AppColors.gray3),
                          ),
                          enabledBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(10),
                            borderSide: const BorderSide(color: AppColors.gray3),
                          ),
                          focusedBorder: OutlineInputBorder(
                            borderRadius: BorderRadius.circular(10),
                            borderSide: const BorderSide(color: AppColors.gold, width: 2),
                          ),
                          prefixText: '+91 ',
                          prefixStyle: const TextStyle(
                            fontWeight: FontWeight.w700,
                            color: AppColors.gray6, fontSize: 14,
                          ),
                          contentPadding: const EdgeInsets.symmetric(horizontal: 14, vertical: 14),
                        ),
                        validator: (v) {
                          if (v == null || v.length != 10) {
                            return 'Enter a valid 10-digit number';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 16),

                      // Dealer code
                      _FieldLabel('Dealer Code'),
                      const SizedBox(height: 6),
                      TextFormField(
                        controller: _codeCtr,
                        textCapitalization: TextCapitalization.characters,
                        decoration: const InputDecoration(
                          hintText: 'e.g. BG-PUN-001',
                          prefixIcon: Icon(Icons.store_outlined,
                              color: AppColors.gray4),
                        ),
                        validator: (v) {
                          if (v == null || v.trim().isEmpty) {
                            return 'Dealer code is required';
                          }
                          return null;
                        },
                      ),
                      const SizedBox(height: 8),

                      // Error
                      if (_error != null) ...[
                        const SizedBox(height: 12),
                        _ErrorBanner(_error!),
                      ],

                      const SizedBox(height: 24),

                      // Send OTP button
                      SizedBox(
                        width: double.infinity,
                        child: ElevatedButton(
                          onPressed: _loading ? null : _sendOtp,
                          child: _loading
                              ? const SizedBox(
                                  width: 22, height: 22,
                                  child: CircularProgressIndicator(
                                    strokeWidth: 2.5,
                                    color: AppColors.navy,
                                  ),
                                )
                              : const Text('SEND OTP'),
                        ),
                      ),

                      const SizedBox(height: 16),
                      Center(
                        child: Text(
                          'Need help? Contact BGauss Support\nhelp@bgauss.com',
                          textAlign: TextAlign.center,
                          style: TextStyle(fontSize: 12, color: AppColors.gray4),
                        ),
                      ),
                    ],
                  ),
                ),
              ),

              const SizedBox(height: 32),
              Text(
                'v 1.0 | BGauss Auto Pvt. Ltd.',
                style: TextStyle(fontSize: 11, color: Colors.white.withValues(alpha: 0.3)),
              ),
              const SizedBox(height: 24),
            ],
          ),
        ),
      ),
    );
  }
}

class _FieldLabel extends StatelessWidget {
  final String text;
  const _FieldLabel(this.text);
  @override
  Widget build(BuildContext context) => Text(
        text,
        style: const TextStyle(
          fontSize: 12, fontWeight: FontWeight.w700,
          color: AppColors.gray6,
        ),
      );
}

class _ErrorBanner extends StatelessWidget {
  final String message;
  const _ErrorBanner(this.message);
  @override
  Widget build(BuildContext context) => Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: const Color(0xFFFEE2E2),
          border: Border.all(color: const Color(0xFFFCA5A5)),
          borderRadius: BorderRadius.circular(10),
        ),
        child: Row(
          children: [
            const Icon(Icons.error_outline, color: AppColors.red, size: 18),
            const SizedBox(width: 8),
            Expanded(
              child: Text(
                message,
                style: const TextStyle(
                  fontSize: 13, color: AppColors.red, fontWeight: FontWeight.w600,
                ),
              ),
            ),
          ],
        ),
      );
}