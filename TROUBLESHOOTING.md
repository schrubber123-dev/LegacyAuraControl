# Troubleshooting

## Device not found

- Close other RGB programs.
- Click **Neu suchen**.
- Check Device Manager for an ASUS HID / N-KEY device.
- Reboot once after removing an old Aura/Armoury lighting stack.

## Device is found but colors do not change

Do not run multiple RGB controllers against the same keyboard at once.

On the confirmed GU501GM the expected RGB collection is:

```text
VID_0B05 PID_1866
UsagePage 0xFF31
Usage 0x0079
OUT 64
FEATURE 64
```

## Brightness does not change

The application sends the legacy ASUS keyboard brightness feature command for levels 0–3.

The ASUS Fn brightness keys can still be used independently.

## Profile changes after login

Another ASUS or RGB utility may be applying its own profile. Disable its lighting startup behavior or use only one controller.

## Build fails

The included build script expects the Windows .NET Framework C# compiler.

If it is missing, install/enable Microsoft .NET Framework 4.x and retry.
