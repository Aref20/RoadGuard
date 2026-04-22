import 'package:flutter/material.dart';
import '../../core/api/api_client.dart';
import 'package:hive/hive.dart';
import '../../l10n/app_localizations.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({Key? key}) : super(key: key);

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  bool _isLoading = false;
  String? _error;

  Future<void> _login() async {
    setState(() { _isLoading = true; _error = null; });
    try {
      final res = await ApiClient().dio.post('/auth/login', data: {
        'email': _emailCtrl.text.trim(),
        'password': _passCtrl.text.trim(),
      });
      final token = res.data['token'];
      await Hive.box('settings').put('jwt_token', token);
      if (mounted) Navigator.pushReplacementNamed(context, '/');
    } catch (e) {
      if (mounted) setState(() => _error = context.tr('loginFailed') ?? 'Login failed. Check credentials.');
    } finally {
      if (mounted) setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text('${context.tr('title')} - ${context.tr('signIn')}'),
        actions: [
          IconButton(
            icon: const Icon(Icons.language),
            onPressed: () {
              final box = Hive.box('settings');
              final curr = box.get('language', defaultValue: 'ar');
              box.put('language', curr == 'ar' ? 'en' : 'ar');
            },
          )
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(24.0),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(Icons.speed, size: 80, color: Colors.blueAccent),
            const SizedBox(height: 32),
            if (_error != null) 
              Text(_error!, style: const TextStyle(color: Colors.red)),
            TextField(
              controller: _emailCtrl,
              decoration: InputDecoration(labelText: context.tr('email'), border: const OutlineInputBorder()),
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _passCtrl,
              decoration: InputDecoration(labelText: context.tr('password'), border: const OutlineInputBorder()),
              obscureText: true,
              textDirection: TextDirection.ltr,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _isLoading ? null : _login,
              child: _isLoading ? const CircularProgressIndicator() : Text(context.tr('login')),
            )
          ],
        ),
      ),
    );
  }
}
