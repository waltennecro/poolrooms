"""Pack the attributed ambientCG Ground037 maps for HDRP without changing geometry.

Usage: /usr/bin/python3 tools/build_conservatory_ground.py /path/to/four/source/maps
The sources are identified by immutable URLs and SHA-256 in the shipped manifest.
"""
import argparse,hashlib,json
from pathlib import Path
from PIL import Image,ImageChops
ROOT=Path(__file__).resolve().parents[1]
DEST=ROOT/'Assets/PoolroomsProcedural/Resources/PoolroomsBiomeTextures'
LICENSES=ROOT/'Assets/PoolroomsProcedural/Resources/ConservatorySources'
REV='2d246e69a4da33c6a33b07fdf77ae1aec14b15db'
BASE=f'https://raw.githubusercontent.com/BastiaanOlij/godot_vr_climbing_demo/{REV}/assets/ambientcg.com/Ground037/'
def main():
 p=argparse.ArgumentParser(description=__doc__);p.add_argument('source',type=Path);args=p.parse_args()
 names=['Color','NormalGL','Roughness','AmbientOcclusion'];images={};sources={}
 for suffix in names:
  name=f'Ground037_2K_{suffix}.jpg';data=(args.source/name).read_bytes();i=Image.open(args.source/name)
  assert i.size==(2048,2048)
  images[suffix]=i.convert('RGB' if suffix in ['Color','NormalGL'] else 'L')
  sources[name]={'url':BASE+name,'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
 DEST.mkdir(exist_ok=True);LICENSES.mkdir(exist_ok=True)
 images['Color'].save(DEST/'ConservatoryGround_Albedo.png')
 images['NormalGL'].save(DEST/'ConservatoryGround_Normal.png')
 zero=Image.new('L',(2048,2048),0);one=Image.new('L',(2048,2048),255)
 mask=Image.merge('RGBA',(zero,images['AmbientOcclusion'],one,ImageChops.invert(images['Roughness'])))
 mask.save(DEST/'ConservatoryGround_Mask.png')
 files={}
 for suffix in ['Albedo','Normal','Mask']:
  f=DEST/f'ConservatoryGround_{suffix}.png';data=f.read_bytes();files['PoolroomsBiomeTextures/'+f.name]={'bytes':len(data),'sha256':hashlib.sha256(data).hexdigest()}
 record={'id':'Ground037','author':'ambientCG (Lennart Demes)','source':'https://ambientcg.com/a/Ground037','license':'CC0-1.0','licenseUrl':'https://creativecommons.org/publicdomain/zero/1.0/','mirrorRevision':REV,'sources':sources,'derivedFiles':files,'dimensions':[2048,2048],'normalConvention':'OpenGL (+Y), imported as Unity NormalMap','hdrpMaskChannels':['metallic=0','ambient occlusion','detail=255','smoothness=255-roughness'],'newExternalModels':0,'newExternalTextureSets':1,'displacementClaimed':False,'adaptation':'Lossless decoded PNG colour/normal; AO and inverted roughness packed into HDRP mask. No tessellation/displacement promised.'}
 (LICENSES/'Ground037.json').write_text(json.dumps(record,indent=2)+'\n')
 print(json.dumps({'derivedFiles':len(files),'bytes':sum(v['bytes'] for v in files.values())}))
if __name__=='__main__':main()
