// lib/utils/app_theme.dart
import 'package:flutter/material.dart';

class AppColors {
  static const navy     = Color(0xFF0B1929);
  static const navy2    = Color(0xFF0F2236);
  static const gold     = Color(0xFFFBBF24);
  static const goldDark = Color(0xFFF59E0B);
  static const green    = Color(0xFF16A34A);
  static const amber    = Color(0xFFD97706);
  static const red      = Color(0xFFDC2626);
  static const blue     = Color(0xFF2563EB);
  static const gray1    = Color(0xFFF8FAFC);
  static const gray2    = Color(0xFFF1F5F9);
  static const gray3    = Color(0xFFE2E8F0);
  static const gray4    = Color(0xFF94A3B8);
  static const gray5    = Color(0xFF64748B);
  static const gray6    = Color(0xFF334155);
  static const gray7    = Color(0xFF0F172A);
}

class AppTheme {
  static ThemeData get theme => ThemeData(
    fontFamily:  'Inter',
    colorScheme: ColorScheme.fromSeed(seedColor: AppColors.navy),
    scaffoldBackgroundColor: AppColors.gray1,
    appBarTheme: const AppBarTheme(
      backgroundColor: AppColors.navy,
      foregroundColor: Colors.white,
      elevation: 0,
    ),
    elevatedButtonTheme: ElevatedButtonThemeData(
      style: ElevatedButton.styleFrom(
        backgroundColor: AppColors.gold,
        foregroundColor: AppColors.navy,
        padding: const EdgeInsets.symmetric(vertical: 16, horizontal: 24),
        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
        textStyle: const TextStyle(fontSize: 15, fontWeight: FontWeight.w800),
      ),
    ),
    inputDecorationTheme: InputDecorationTheme(
      filled: true,
      fillColor: Colors.white,
      border: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.gray3),
      ),
      enabledBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.gray3),
      ),
      focusedBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.gold, width: 2),
      ),
      errorBorder: OutlineInputBorder(
        borderRadius: BorderRadius.circular(12),
        borderSide: const BorderSide(color: AppColors.red),
      ),
      contentPadding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
    ),
  );
}