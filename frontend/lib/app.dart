// lib/app.dart
import 'package:b2b_buyback/core/theme/app_theme.dart';
import 'package:b2b_buyback/features/dashboard/data/dashboard_api.dart';
import 'package:b2b_buyback/features/dashboard/presentation/dashboard_screen.dart';
import 'package:flutter/material.dart';

class B2BBuybackApp extends StatelessWidget {
  const B2BBuybackApp({super.key, DashboardApi? api}) : _api = api;

  final DashboardApi? _api;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'B2B Buyback',
      debugShowCheckedModeBanner: false,
      theme: AppTheme.light(),
      home: DashboardScreen(api: _api),
    );
  }
}
