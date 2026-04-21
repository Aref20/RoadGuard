# Speed Alert Testing Guide

## 1. Unit Testing (Backend)
The `OverspeedValidationEngine` is the most critical logic piece.
- **Test scenario 1**: Valid points below limit -> returns `false`.
- **Test scenario 2**: One spike above limit (e.g. GPS bounce) -> smoothed average returns `false`.
- **Test scenario 3**: Sustained speed above (limit + tolerance) for `AlertDelaySeconds` -> returns `true`.

```bash
cd backend
dotnet test
```

## 2. Unit Testing (Mobile)
Test `OverspeedEngine` (Dart) in isolation without Geolocator. Feed it mock `Position` objects. Validate it triggers alerting only when conditions are met and clears the alert when speed drops.

## 3. Manual QA Scenarios
### Hands-Free Flow
1. Install app, grant permissions.
2. Put phone in pocket, begin driving.
3. Observe Android logcat:
   - `IN_VEHICLE` detected.
   - Foreground service started.
   - GPS initialized.
   - API limit checked.
4. Stop driving for 5 minutes.
   - Foreground service stops. Mode returns to `Passive Readiness`.

### Alert Scenarios
1. Drive at `Limit + Tolerance - 1`. -> No alert.
2. Drive at `Limit + Tolerance + 2` for 1 second, then drop speed. -> No alert (filtered).
3. Drive at `Limit + Tolerance + 2` for 5 seconds. -> Alert triggers.
4. Drop speed. -> Alert ends after 1 second.
