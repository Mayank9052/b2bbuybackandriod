// lib/screens/splash_screen.dart
import 'package:flutter/material.dart';
import '../services/api_service.dart';
import '../utils/app_theme.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen>
    with SingleTickerProviderStateMixin {
  late AnimationController _ctrl;
  late Animation<double>   _fadeAnim;
  late Animation<double>   _scaleAnim;

  @override
  void initState() {
    super.initState();
    _ctrl = AnimationController(
      vsync:    this,
      duration: const Duration(milliseconds: 900),
    );
    _fadeAnim  = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _ctrl, curve: Curves.easeIn),
    );
    _scaleAnim = Tween<double>(begin: 0.7, end: 1).animate(
      CurvedAnimation(parent: _ctrl, curve: Curves.elasticOut),
    );
    _ctrl.forward();
    _checkSession();
  }

  Future<void> _checkSession() async {
    await Future.delayed(const Duration(seconds: 2));
    if (!mounted) return;
    final loggedIn = await ApiService.isLoggedIn();
    if (!mounted) return;
    Navigator.pushReplacementNamed(
      context,
      loggedIn ? '/dashboard' : '/login',
    );
  }

  @override
  void dispose() {
    _ctrl.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.navy,
      body: Center(
        child: FadeTransition(
          opacity: _fadeAnim,
          child: ScaleTransition(
            scale: _scaleAnim,
            child: Column(
              mainAxisSize: MainAxisSize.min,
              children: [
                // Logo circle
                Container(
                  width: 100, height: 100,
                  decoration: BoxDecoration(
                    color: AppColors.gold,
                    shape: BoxShape.circle,
                    boxShadow: [
                      BoxShadow(
                        color: AppColors.gold.withValues(alpha: 0.4),
                        blurRadius: 32, spreadRadius: 4,
                      )
                    ],
                  ),
                  alignment: Alignment.center,
                  child: const Text(
                    'BG',
                    style: TextStyle(
                      fontSize: 36, fontWeight: FontWeight.w900,
                      color: AppColors.navy,
                    ),
                  ),
                ),
                const SizedBox(height: 24),
                const Text(
                  'BGauss PI App',
                  style: TextStyle(
                    fontSize: 26, fontWeight: FontWeight.w900,
                    color: Colors.white, letterSpacing: -0.5,
                  ),
                ),
                const SizedBox(height: 8),
                Text(
                  'Procurement & Inspection',
                  style: TextStyle(
                    fontSize: 13, fontWeight: FontWeight.w500,
                    color: Colors.white.withValues(alpha: 0.55),
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  'Certified Exchange Program',
                  style: TextStyle(
                    fontSize: 12,
                    color: AppColors.gold.withValues(alpha: 0.8),
                    fontWeight: FontWeight.w600,
                  ),
                ),
                const SizedBox(height: 48),
                SizedBox(
                  width: 28, height: 28,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: AppColors.gold.withValues(alpha: 0.6),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}