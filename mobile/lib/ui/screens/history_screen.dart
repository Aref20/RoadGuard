import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({Key? key}) : super(key: key);

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  List<dynamic> _sessions = [];
  bool _isLoading = true;

  @override
  void initState() {
    super.initState();
    _fetchHistory();
  }

  Future<void> _fetchHistory() async {
    try {
      final res = await ApiClient().dio.get('/sessions');
      setState(() {
        _sessions = res.data;
      });
    } catch (e) {
      // Fallback or error state
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Driving History')),
      body: _isLoading 
        ? const Center(child: CircularProgressIndicator())
        : _sessions.isEmpty
          ? const Center(child: Text('No driving sessions recorded yet.'))
          : ListView.builder(
              itemCount: _sessions.length,
              itemBuilder: (context, index) {
                final session = _sessions[index];
                return ListTile(
                  leading: Icon(session['wasAutoStarted'] ? Icons.settings_remote : Icons.play_arrow),
                  title: Text('Session: ${session['id'].toString().substring(0,8)}'),
                  subtitle: Text('Started: ${session['startedAt']}'),
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
