import 'package:flutter/material.dart';
import '../core/api/api_client.dart';
import 'package:hive/hive.dart';

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
      setState(() => _error = 'Login failed. Check credentials.');
    } finally {
      setState(() => _isLoading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Speed Alert - Sign In')),
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
              decoration: const InputDecoration(labelText: 'Email', border: OutlineInputBorder()),
              keyboardType: TextInputType.emailAddress,
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _passCtrl,
              decoration: const InputDecoration(labelText: 'Password', border: OutlineInputBorder()),
              obscureText: true,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _isLoading ? null : _login,
              child: _isLoading ? const CircularProgressIndicator() : const Text('Login / Register'),
            )
          ],
        ),
      ),
    );
  }
}
