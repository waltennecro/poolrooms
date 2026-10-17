"""Build original, seamless PBR surfaces. Requires Pillow and NumPy; no photos/assets downloaded.

Textures use a 2.4 m repeat for the floor and 1.2 m for the remaining surfaces.
HDRP mask channels: metallic=0, occlusion, detail=1, smoothness. Albedo has no
baked directional lighting. Normal maps derive from metre-scale height fields.
"""
from pathlib import Path
import hashlib
import json
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures"
SIZE = 1024


def save_set(name, colour, height, smooth, ao, metres):
    pixel = metres / SIZE
    dx = (np.roll(height, -1, axis=1) - np.roll(height, 1, axis=1)) / (2 * pixel)
    dy = (np.roll(height, -1, axis=0) - np.roll(height, 1, axis=0)) / (2 * pixel)
    normal = np.stack((-dx, dy, np.ones_like(height)), axis=-1)
    normal /= np.linalg.norm(normal, axis=-1, keepdims=True)
    mask = np.stack((np.zeros_like(height), ao, np.ones_like(height), smooth), axis=-1)
    for suffix, data in (("Albedo", colour), ("Normal", normal * .5 + .5), ("Mask", mask)):
        Image.fromarray(np.uint8(np.clip(data, 0, 1) * 255 + .5)).save(OUT / f"{name}_{suffix}.png")


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    y, x = np.mgrid[0:SIZE, 0:SIZE].astype(np.float64) / SIZE
    # Integer frequencies keep all maps periodic, including their derivatives.
    grain = (np.sin(2*np.pi*(x*173+y*97)) * np.sin(2*np.pi*(x*79-y*131))
             + .45*np.cos(2*np.pi*(x*307+y*251))) / 1.45
    pores = np.maximum(0, np.sin(2*np.pi*x*211) * np.cos(2*np.pi*y*197)) ** 6
    colours = {"Green":(.19,.60,.36), "Yellow":(.94,.73,.17),
               "Red":(.81,.20,.29), "Blue":(.16,.48,.78)}
    for name, rgb in colours.items():
        # Subtle vinyl grain and shallow stitched sleeve seam: no chrome/plastic metallic values.
        seam = np.exp(-((np.minimum(x,1-x))/.0018)**2)
        height = grain*.000035 - pores*.000045 - seam*.00016
        colour = np.array(rgb)[None,None,:] * (1+grain[...,None]*.012)
        smooth = np.clip(.45 + grain*.025 - pores*.06,0,1)
        save_set("PlayFinish"+name, colour, height, smooth, 1-seam*.12, 1.2)

    # Four EVA squares per axis; opposing edges use the same periodic jigsaw wave.
    tx = x*4 + .022*np.sin(2*np.pi*y*40)
    ty = y*4 + .022*np.sin(2*np.pi*x*40)
    ix, iy = np.floor(tx).astype(int), np.floor(ty).astype(int)
    ux, uy = tx-np.floor(tx), ty-np.floor(ty)
    edge = np.minimum(np.minimum(ux,1-ux),np.minimum(uy,1-uy))
    groove = np.exp(-(edge/.009)**2)
    palette=np.array([(.21,.58,.39),(.13,.43,.73),(.93,.68,.19),(.80,.36,.21)])
    colour=palette[(ix+iy*3)%4]*(1+grain[...,None]*.02)
    height=grain*.000065-pores*.00009-groove*.0018
    save_set("PlayFinishFloor",colour,height,np.clip(.20+grain*.025-groove*.05,0,1),1-groove*.25,2.4)
    height=grain*.000018
    colour=np.ones((SIZE,SIZE,3))*np.array([.87,.86,.81])*(1+grain[...,None]*.008)
    save_set("PlayFinishWall",colour,height,np.full_like(x,.22),np.ones_like(x),1.2)
    weave=np.sin(2*np.pi*(x*64+y*128))*np.sin(2*np.pi*(x*128-y*64))
    height=weave*.000018
    colour=np.ones((SIZE,SIZE,3))*np.array([.055,.063,.067])*(1+weave[...,None]*.05)
    save_set("PlayFinishNet",colour,height,np.full_like(x,.15),np.ones_like(x),1.2)
    files=sorted(OUT.glob("PlayFinish*_*.png"))
    report={"source":"Original generated surfaces; no reference photographs embedded", "size":SIZE,
            "hdrpMask":"R metallic, G ambient occlusion, B detail mask, A smoothness",
            "files":{str(p.relative_to(ROOT)):{"bytes":p.stat().st_size,"sha256":hashlib.sha256(p.read_bytes()).hexdigest()} for p in files}}
    (ROOT/"Verification/play_finish_textures.json").write_text(json.dumps(report,indent=2)+"\n")
    print(f"Generated {len(files)} original 1024px PBR maps")


if __name__ == "__main__":
    main()
