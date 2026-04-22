import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:hive/hive.dart';
import '../../core/api/api_client.dart';
import '../../l10n/app_localizations.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _emailCtrl = TextEditingController();
  final _passCtrl = TextEditingController();
  bool _isLoading = false;
  String? _error;

  @override
  void dispose() {
    _emailCtrl.dispose();
    _passCtrl.dispose();
    super.dispose();
  }

  Future<void> _login() async {
    final localizations = AppLocalizations.of(context);

    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final response = await ApiClient().dio.post('/auth/login', data: {
        'email': _emailCtrl.text.trim(),
        'password': _passCtrl.text.trim(),
      });

      if ((response.data['role'] as String?) == 'Admin') {
        if (!mounted) {
          return;
        }

        setState(() => _error = localizations.translate('adminNotAllowedOnMobile'));
        return;
      }

      final token = response.data['token'] as String?;
      if (token == null || token.isEmpty) {
        if (!mounted) {
          return;
        }

        setState(() => _error = localizations.translate('loginFailed'));
        return;
      }

      await Hive.box('settings').put('jwt_token', token);
      if (mounted) {
        Navigator.pushReplacementNamed(context, '/');
      }
    } on DioException catch (error) {
      String message = localizations.translate('loginFailed');
      final code = error.response?.data is Map ? error.response?.data['code']?.toString() : null;

      if (code == 'AUTH_ACCOUNT_DISABLED') {
        message = localizations.translate('accountDisabled');
      } else if (code == 'AUTH_INVALID_CREDENTIALS') {
        message = localizations.translate('invalidCredentials');
      }

      if (mounted) {
        setState(() => _error = message);
      }
    } catch (_) {
      if (mounted) {
        setState(() => _error = localizations.translate('loginFailed'));
      }
    } finally {
      if (mounted) {
        setState(() => _isLoading = false);
      }
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
            onPressed: () async {
              final box = Hive.box('settings');
              final currentLanguage = box.get('language', defaultValue: 'ar');
              await box.put('language', currentLanguage == 'ar' ? 'en' : 'ar');
            },
          )
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          mainAxisAlignment: MainAxisAlignment.center,
          crossAxisAlignment: CrossAxisAlignment.stretch,
          children: [
            const Icon(Icons.speed, size: 80, color: Colors.blueAccent),
            const SizedBox(height: 32),
            if (_error != null)
              Padding(
                padding: const EdgeInsets.only(bottom: 16),
                child: Text(
                  _error!,
                  style: const TextStyle(color: Colors.red),
                  textAlign: TextAlign.center,
                ),
              ),
            TextField(
              controller: _emailCtrl,
              decoration: InputDecoration(
                labelText: context.tr('email'),
                border: const OutlineInputBorder(),
              ),
              keyboardType: TextInputType.emailAddress,
              textDirection: TextDirection.ltr,
            ),
            const SizedBox(height: 16),
            TextField(
              controller: _passCtrl,
              decoration: InputDecoration(
                labelText: context.tr('password'),
                border: const OutlineInputBorder(),
              ),
              obscureText: true,
              textDirection: TextDirection.ltr,
            ),
            const SizedBox(height: 24),
            ElevatedButton(
              onPressed: _isLoading ? null : _login,
              child: _isLoading
                  ? const SizedBox(
                      height: 18,
                      width: 18,
                      child: CircularProgressIndicator(strokeWidth: 2),
                    )
                  : Text(context.tr('login')),
            )
          ],
        ),
      ),
    );
  }
}
