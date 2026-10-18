# Supported hardware

## Confirmed

### ASUS ROG GU501GM

| Property | Value |
|---|---|
| Vendor ID | `0B05` |
| Product ID | `1866` |
| Usage Page | `0xFF31` |
| RGB Usage | `0x0079` |
| Output Report | `64 bytes` |
| Feature Report | `64 bytes` |
| Input Report | `32 bytes` |

The GU501GM exposes three HID collections with the same VID/PID:

```text
#1  OUT=64 FEATURE=64 INPUT=32  UsagePage=0xFF31 Usage=0x0079
#2  OUT=0  FEATURE=64 INPUT=6   UsagePage=0xFF31 Usage=0x0080
#3  OUT=0  FEATURE=64 INPUT=6   UsagePage=0xFF31 Usage=0x0076
```

The RGB controller is the collection with the output report and Usage `0x0079`.

## Experimental IDs

Legacy Aura Control also recognizes:

- `0B05:1854`
- `0B05:1869`

These IDs are associated with legacy ASUS Aura Core hardware in public implementations, but v1.1.0 does not claim them as confirmed until a real user report verifies operation.

## Reporting another model

Open a **Hardware compatibility report** and include:

- exact laptop model
- Windows version
- copied Legacy Aura Control diagnostics
- whether Static works
- whether Breathing works
- whether Color Cycle works
- whether four-zone mode works
- whether brightness 0–3 works
