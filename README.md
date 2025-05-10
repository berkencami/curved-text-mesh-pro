Curved TextMeshPro Tool for Unity
=================================

![ScreenShot](https://github.com/berkencami/curved-text-mesh-pro/blob/main/Movie_001.gif)

This tool allows you to render TextMeshPro text along a customizable arc (curved) path in Unity.

Features
--------
- **Single parameter control:** Adjust the arc angle from 0° (flat text) to 180° (half circle) with a single slider.
- **Automatic radius calculation:** The tool automatically calculates the required radius based on the text width and arc angle.
- **Real-time updates:** Changes are visible instantly in the Unity Editor.
- **Multi-line support:** Works with multi-line TextMeshPro text.
- **Easy integration:** Just add the script to any GameObject with a TextMeshPro component.

How to Use
----------
1. **Add a TextMeshPro component** to your GameObject (UI or 3D).
2. **Attach the `CurvedTextMeshPro` script** to the same GameObject.
3. In the Inspector, you will see the **Arc Degrees** slider:
   - Set to `0` for flat (normal) text.
   - Increase the value to curve the text into an arc.
   - `180` will create a perfect half-circle.
4. Edit your text as usual in the TextMeshPro component.

Parameter
---------
- **Arc Degrees:**
  - Range: `0` to `180`
  - Controls the curvature of the text.
  - `0` = flat, `180` = half circle.

How It Works
------------
- The script calculates the required radius for the arc based on the current text width and the selected arc angle.
- Each character is positioned and rotated along the arc using a transformation matrix.
- When the arc angle is `0`, the text is rendered flat, preserving the original TextMeshPro layout.

Compatibility
-------------
- Unity 2020.3+
- TextMeshPro package required
