// lib/main.dart
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'screens/splash_screen.dart';
import 'screens/login_screen.dart';
import 'screens/otp_screen.dart';
import 'screens/dashboard_screen.dart';   // ← screens/ not features/
import 'utils/app_theme.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations([DeviceOrientation.portraitUp]);
  runApp(const BGaussPIApp());
}

class BGaussPIApp extends StatelessWidget {
  const BGaussPIApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title:        'BGauss PI App',
      theme:        AppTheme.theme,
      debugShowCheckedModeBanner: false,
      home:         const SplashScreen(),
      routes: {
        '/login':     (_) => const LoginScreen(),
        '/dashboard': (_) => const DashboardScreen(),
      },
      onGenerateRoute: (settings) {
        if (settings.name == '/otp') {
          final args = settings.arguments as Map<String, String>;
          return MaterialPageRoute(
            builder: (_) => OtpScreen(
              mobile:     args['mobile']!,
              dealerCode: args['dealerCode']!,
            ),
          );
        }
        return null;
      },
    );
  }
}