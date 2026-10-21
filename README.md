<div align="center">

<img src="assets/LegacyAura.png" width="128" alt="Legacy Aura Control icon">

# Legacy Aura Control

**Unofficial, independent RGB control for compatible legacy ASUS ROG laptop keyboards on modern Windows.**

> **Independent community project:** Legacy Aura Control is not affiliated with, authorized, sponsored, or endorsed by ASUS or ASUSTeK Computer Inc. Product and brand names are used only to identify compatible hardware.

[![Version](https://img.shields.io/badge/version-1.2.1-blue.svg)](#release)
[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4.svg)](#compatibility)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)
[![Hardware tested](https://img.shields.io/badge/tested-GU501GM-success.svg)](#confirmed-hardware)  
[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20Me%20a%20Coffee-support%20the%20project-FFDD00?logo=buymeacoffee&logoColor=000000)](https://buymeacoffee.com/schrubber)

</div>

---

## Why this exists

Older ASUS ROG laptops can still have fully working RGB keyboards even when the legacy **ROG Aura Core / Aura Service** software stack is unavailable or no longer works correctly on a modern Windows installation.

**Legacy Aura Control talks directly to the legacy ASUS HID interface.**

No Armoury Crate.  
No Aura Service.  
No old Microsoft Store app required.

## Features

- Static RGB color
- Breathing effect
- Color cycle
- Rainbow cycle
- Four-zone static colors
- Four-zone breathing
- Four-zone rainbow preset
- Three animation speeds
- Hardware brightness: Off / Low / Medium / High
- Saves the last configuration
- Optional auto-apply at Windows login
- Built-in HID diagnostics
- System tray / notification-area icon with quick actions
- ASUS Fn brightness keys can continue to work normally
- English-only user interface for a consistent public release

## Confirmed hardware

### ASUS ROG GU501GM

| Property | Value |
|---|---|
| USB VID | `0B05` |
| USB PID | `1866` |
| Usage Page | `0xFF31` |
| RGB Usage | `0x0079` |
| Output Report | `64 bytes` |
| Feature Report | `64 bytes` |
| Input Report | `32 bytes` |
| Tested OS | Windows 11 |

This hardware has been tested with **colors, effects, four-zone control, speed and brightness**.

See [SUPPORTED_HARDWARE.md](SUPPORTED_HARDWARE.md) for more information.

## Quick start

1. Open the [latest release](https://github.com/schrubber123-dev/LegacyAuraControl/releases/latest).
2. Download `LegacyAuraControl_1.2.1.zip`.
3. Extract the ZIP.
4. Close other RGB applications such as Aura Core, Armoury Crate lighting control or OpenRGB.
5. Run `Install.cmd`.
6. Start **Legacy Aura Control** from the Desktop or Start Menu.
7. Select an effect, color and brightness and press **APPLY**.

For portable use, run `Build_and_Start.cmd` instead.

## Compatibility

The controller currently recognizes these legacy Aura Core product IDs:

- `0B05:1854`
- `0B05:1866`
- `0B05:1869`

Only the **GU501GM / PID 1866** is claimed as hardware-confirmed in v1.2.1. Other IDs remain experimental until tested on real laptops.

## Coexistence with ASUS software

You can keep:

- ASUS Keyboard Hotkeys
- ROG Gaming Center
- normal ASUS hotkey/OSD components

During RGB control, avoid running another application that writes to the same Aura HID interface at the same time.

## Diagnostics

If your laptop is detected but does not react correctly, use **Copy diagnostics** inside the app and include the result in a hardware compatibility report.

## Building from source

Source:

`src/LegacyAura.cs`

The included Windows build script compiles the application with the .NET Framework C# compiler.

No proprietary ASUS SDK or ASUS binary is required.

## Release

### v1.2.1

Documentation, attribution and trademark/disclaimer cleanup. The hardware-verified HID control path is unchanged from v1.2.0. System-tray support, effects, four-zone control, brightness and auto-apply are included.

See [CHANGELOG.md](CHANGELOG.md).

## Support the project

If Legacy Aura Control helped you bring your old ROG keyboard RGB back to life and you would like to support further development, you can leave a tip here:

☕ **[Buy me a coffee](https://buymeacoffee.com/schrubber)**

Support is completely optional — the project remains free and open source.

## License

Legacy Aura Control is released under the [MIT License](LICENSE).

The project contains **no ASUS binaries, DLLs, SDKs, firmware, drivers, installers, logos or artwork**.

Compatibility names such as ASUS, ROG, Aura, Aura Core and Armoury Crate are used only to identify hardware/software interoperability. Those names and marks belong to their respective owners. Legacy Aura Control is an independent community project and is **not affiliated with, authorized, sponsored, or endorsed by ASUS or ASUSTeK Computer Inc.**

See [DISCLAIMER.md](DISCLAIMER.md) and [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).

## Tray behavior

Legacy Aura Control stays available in the Windows notification area. Closing or minimizing the main window hides it to the tray instead of terminating it. Double-click the tray icon to reopen the window. Right-click it for **Open**, **Apply last setting**, and **Exit**.

When auto-apply is enabled, the saved profile is applied at Windows login and the app remains quietly in the tray.
