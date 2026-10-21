# Changelog

## 1.2.1

- Added a prominent independent/unofficial project notice and trademark clarification.
- Added `DISCLAIMER.md` and expanded third-party attribution.
- Included the full MIT notice for the `rogauracore` protocol research reference.
- Release packages now include the disclaimer and third-party notices.
- Cleaned remaining German UI strings and reduced unnecessary product branding in internal labels.
- No hardware protocol behavior was intentionally changed.

## 1.2.0

- Public release now uses an English-only user interface.
- Updated all UI, tray, status, diagnostics and error messages to English.
- Kept the hardware-verified RGB/HID control path unchanged.
- Includes tray support, four-zone control, brightness and Windows-login auto-apply.

## 1.1.0

- Added Windows notification-area / system-tray icon.
- Closing or minimizing the window now hides the app to the tray.
- Double-click tray icon to reopen the UI.
- Tray menu: Open, Apply last setting, Exit.
- Auto-apply at Windows login now keeps the controller running quietly in the tray.
- Uses the same hardware-verified HID/RGB control code from 1.0.x.

## 1.0.1

- Fixed the application icon in the title bar and taskbar.
- No HID/RGB control code changed.

## 1.0.0

- First hardware-verified release.
- Direct HID control for legacy ASUS Aura Core keyboards.
- Static RGB, breathing, color cycle and rainbow effects.
- Four-zone control and three animation speeds.
- Hardware brightness levels 0-3.
- Persistent settings and Windows-login auto-apply.
- Diagnostic HID output.
- Confirmed on ASUS ROG GU501GM (`VID_0B05 / PID_1866`).
