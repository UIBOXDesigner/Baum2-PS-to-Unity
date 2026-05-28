# Baum2 Photoshop Plugin - Installation Guide

## Overview

Baum2 is a Photoshop plugin that converts PSD designs into Unity UI prefabs. This guide explains how to install and use the plugin.

---

## System Requirements

| Requirement | Minimum Version |
|------------|----------------|
| Adobe Photoshop | 2020 (21.0) or later |
| Unity | 2019.4 LTS or later |
| Operating System | Windows 10 / macOS 10.14 |
| Disk Space | 50 MB |

---

## Installation Methods

### Method 1: Manual Script Installation (Recommended)

#### Step 1: Locate Photoshop Scripts Folder

**Windows:**
```
C:\Program Files\Adobe\Adobe Photoshop 202X\Presets\Scripts
```

**macOS:**
```
/Applications/Adobe Photoshop 202X/Presets/Scripts/
```

> If the Scripts folder doesn't exist, create it manually.

#### Step 2: Copy Plugin Files

Copy the following files from `PhotoshopPlugin/` (or `PhotoshopScript/`) to the Photoshop Scripts folder:

```
Baum.js          - Main export script
Baum.coffee      - Source CoffeeScript file
BaumMapping.json - Control type mapping configuration
icon.png         - Plugin icon
```

#### Step 3: Restart Photoshop

Close and reopen Photoshop to load the new scripts.

#### Step 4: Verify Installation

1. Open Photoshop
2. Go to `File > Scripts`
3. You should see `Baum.js` in the menu

---

### Method 2: Quick Run (No Installation)

You can run the plugin without installing it permanently:

1. Open your PSD file in Photoshop
2. Go to `File > Scripts > Browse...`
3. Select `Baum.js` from the `PhotoshopPlugin/` folder
4. The plugin will run immediately

> **Tip:** Use this method for testing or one-time exports.

---

### Method 3: Install as Photoshop Extension (CEP Panel)

For a more integrated experience, you can install Baum2 as a Photoshop extension panel.

#### Prerequisites

- Enable Photoshop CEP debugging (see [Adobe CEP Guide](https://github.com/Adobe-CEP/CEP-Resources))

#### Installation Steps

1. Copy the `Baum2Extension/` folder to:
   - **Windows:** `C:\Program Files (x86)\Common Files\Adobe\CEP\extensions\`
   - **macOS:** `~/Library/Application Support/Adobe/CEP/extensions/`

2. Restart Photoshop

3. Open the panel: `Window > Extensions > Baum2`

> **Note:** The CEP extension is optional. The script-based workflow (Method 1 & 2) is the primary supported method.

---

## Unity Side Installation

### Step 1: Import Baum2 Unity Package

1. Open your Unity project
2. Go to `Assets > Import Package > Custom Package...`
3. Select `Baum2.unitypackage` from the project root
4. Click `Import` to import all files

### Step 2: Verify Unity Installation

After import, you should see:
```
Assets/
├── Baum2/
│   ├── Editor/
│   ├── Scripts/
│   └── ...
```

### Step 3: Test the Workflow

1. Export your PSD using the Photoshop plugin (see Usage below)
2. Copy the exported files (`.layout.txt` and image folder) to your Unity `Assets/` folder
3. Right-click the `.layout.txt` file in Unity
4. Select `Baum2 > Generate UI`
5. The UI prefab will be generated automatically

---

## Usage

### Basic Export

1. Open your UI design PSD in Photoshop
2. Name your layers according to the [UI Naming Convention](./UI_PS_to_Unity_Workflow.md)
3. Go to `File > Scripts > Baum.js`
4. Select the output directory
5. Click `OK`

The plugin will generate:
```
OutputFolder/
├── YourPSD.layout.txt    - UI hierarchy and properties (JSON)
└── YourPSD/              - All image assets (PNG)
    ├── btn_ok_normal.png
    ├── img_bg.png
    └── ...
```

### Naming Convention Quick Reference

| Layer Prefix | Unity Component |
|-------------|----------------|
| `ui_btn_` | Button |
| `ui_txt_` | Text |
| `ui_img_` | Image |
| `ui_icon_` | Image |
| `ui_input_` | InputField |
| `ui_slider_` | Slider |
| `ui_tog_` | Toggle |
| `ui_panel_` | Panel |

**State Suffixes:** `_normal`, `_hover`, `_pressed`, `_disabled`, `_selected`, `_highlighted`

**Parameter Syntax:** Add `@` followed by parameters:
- `ui_btn_ok_normal@center` - Button with center pivot
- `ui_img_bg@stretchxy` - Image with XY stretch
- `ui_panel_main@bottom` - Panel anchored to bottom

---

## Troubleshooting

### Plugin doesn't appear in Scripts menu
- **Cause:** Scripts folder path is incorrect
- **Solution:** Verify the scripts folder location for your Photoshop version

### Export fails with "Undefined layers"
- **Cause:** Layer names don't follow the naming convention
- **Solution:** Rename layers with proper `ui_` prefix

### Unity import fails
- **Cause:** Baum2.unitypackage not imported
- **Solution:** Import the Unity package first

### Images are low quality
- **Cause:** PSD resolution is too low
- **Solution:** Design at 2x or 4x resolution, then scale down in Unity

---

## Uninstallation

### Remove Photoshop Script
1. Go to the Photoshop Scripts folder
2. Delete `Baum.js`, `Baum.coffee`, `BaumMapping.json`

### Remove Unity Package
1. In Unity, delete the `Assets/Baum2/` folder
2. Reopen Unity to complete cleanup

---

## Support

- **GitHub Issues:** [https://github.com/UIBOXDesigner/Baum2-PS-to-Unity/issues](https://github.com/UIBOXDesigner/Baum2-PS-to-Unity/issues)
- **Documentation:** See `UI_PS_to_Unity_Workflow.md`
- **Email:** fangjianglin@vip.qq.com

---

## License

MIT License - see `LICENSE` file for details.
