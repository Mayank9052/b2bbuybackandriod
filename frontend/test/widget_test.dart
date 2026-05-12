import 'package:b2b_buyback/app.dart';
import 'package:b2b_buyback/features/dashboard/data/dashboard_api.dart';
import 'package:b2b_buyback/features/dashboard/models/dashboard_models.dart';
import 'package:flutter_test/flutter_test.dart';

class FakeDashboardApi extends DashboardApi {
  @override
  Future<DashboardData> fetchDashboardData() async {
    return DashboardData.seed();
  }
}

void main() {
  testWidgets('renders buyback starter dashboard', (WidgetTester tester) async {
    await tester.pumpWidget(B2BBuybackApp(api: FakeDashboardApi()));
    await tester.pump();

    expect(find.text('B2B Buyback'), findsOneWidget);
    expect(find.text('Live buyback pipeline'), findsOneWidget);
    expect(find.text('Operations snapshot'), findsOneWidget);
    expect(find.text('Pending requests'), findsOneWidget);
  });
}
