from PIL import Image
import os

# Paths
source_png = r"C:\Users\MK Shaon\.gemini\antigravity\brain\c8805371-9016-43ec-9c3c-36fb5ee0e9d5\social_sentry_icon_1769476133996.png"
output_ico = r"e:\Cursor Play ground\Social-Sentry-PC-\Social Sentry\Images\app.ico"

# Open the source image
img = Image.open(source_png)

# Ensure it has an alpha channel
if img.mode != 'RGBA':
    img = img.convert('RGBA')

# Create multiple sizes for the ICO file
# Windows uses different sizes for different contexts
sizes = [(16, 16), (32, 32), (48, 48), (64, 64), (128, 128), (256, 256)]

# Create resized versions
icon_images = []
for size in sizes:
    resized = img.resize(size, Image.Resampling.LANCZOS)
    icon_images.append(resized)

# Save as ICO with all sizes
img.save(output_ico, format='ICO', sizes=sizes)

print(f"✓ Successfully created {output_ico}")
print(f"  Sizes included: {', '.join([f'{s[0]}x{s[1]}' for s in sizes])}")
