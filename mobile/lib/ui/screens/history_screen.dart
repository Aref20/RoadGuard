import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import '../../l10n/app_localizations.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  final CancelToken _cancelToken = CancelToken();
  List<dynamic> _sessions = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchHistory();
  }

  @override
  void dispose() {
    _cancelToken.cancel('History screen disposed');
    super.dispose();
  }

  Future<void> _fetchHistory() async {
    try {
      final res = await ApiClient().dio.get(
            '/sessions',
            cancelToken: _cancelToken,
          );
      if (!mounted) {
        return;
      }

      setState(() {
        _sessions = res.data;
      });
    } on DioException catch (e) {
      if (CancelToken.isCancel(e)) {
        return;
      }

      // Fallback or error state
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text(context.tr('drivingHistory'))),
      body: _isLoading
          ? const Center(child: CircularProgressIndicator())
          : _sessions.isEmpty
              ? Center(child: Text(context.tr('noHistoryFound')))
              : ListView.builder(
                  itemCount: _sessions.length,
                  itemBuilder: (context, index) {
                    final session = _sessions[index];
                    return ListTile(
                      leading: Icon(session['wasAutoStarted']
                          ? Icons.settings_remote
                          : Icons.play_arrow),
                      title: Text(
                          '${context.tr('session')}: ${session['id'].toString().substring(0, 8)}'),
                      subtitle: Text(
                          '${context.tr('startedAt')}: ${session['startedAt']}'),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () {
                        // Navigate to detail view
                      },
                    );
                  },
                ),
    );
  }
}
