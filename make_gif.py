# Visualization/make_gif.py

from PIL import Image
import os

frames_folder = "../output/frames"
output_folder = "../output"

def create_gif(prefix, output_name):
    images = []

    files = sorted([
        file for file in os.listdir(frames_folder)
        if file.startswith(prefix) and file.endswith(".png")
    ])

    for file in files:
        path = os.path.join(frames_folder, file)
        images.append(Image.open(path))

    if len(images) == 0:
        print("No se encontraron imágenes para", prefix)
        return

    gif_path = os.path.join(output_folder, output_name)

    images[0].save(
        gif_path,
        save_all=True,
        append_images=images[1:],
        duration=80,
        loop=0
    )

    print("GIF creado:", gif_path)


create_gif("sequential_day_", "sequential.gif")
create_gif("parallel_day_", "parallel.gif")