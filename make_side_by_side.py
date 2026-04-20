# Visualization/make_side_by_side.py

from PIL import Image
import os

frames_folder = "../output/frames"
output_path = "../output/comparison.gif"

sequential_files = sorted([
    file for file in os.listdir(frames_folder)
    if file.startswith("sequential_day_")
])

parallel_files = sorted([
    file for file in os.listdir(frames_folder)
    if file.startswith("parallel_day_")
])

combined_frames = []

for seq_file, par_file in zip(sequential_files, parallel_files):
    seq_image = Image.open(os.path.join(frames_folder, seq_file))
    par_image = Image.open(os.path.join(frames_folder, par_file))

    width = seq_image.width + par_image.width
    height = max(seq_image.height, par_image.height)

    combined = Image.new("RGB", (width, height))

    combined.paste(seq_image, (0, 0))
    combined.paste(par_image, (seq_image.width, 0))

    combined_frames.append(combined)

if len(combined_frames) == 0:
    print("No se encontraron frames.")
else:
    combined_frames[0].save(
        output_path,
        save_all=True,
        append_images=combined_frames[1:],
        duration=80,
        loop=0
    )

    print("Comparación creada:", output_path)