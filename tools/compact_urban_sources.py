"""Compact native sources losslessly; triangle attribute hashes must remain equal."""
import hashlib,json
import numpy as np
from import_urban_sources import DEST,refresh_material_constants
from rebuild_urban_lods import entries_from_obj,main as rebuild_lods
from source_lods import compact

def geometry_hash(entries):
 h=hashlib.sha256()
 for m,p,n,t,f in entries:
  h.update(str(m).encode());h.update(np.column_stack((p,n,t))[f].astype('<f8').tobytes())
 return h.hexdigest()

def main():
 catalog=json.loads((DEST/'catalog.json').read_text());report=[]
 for model in catalog['models']:
  name=model['id'];path=DEST/'Models'/(name+'.obj');old_size=path.stat().st_size;entries,labels=entries_from_obj(path);old_hash=geometry_hash(entries);packed=compact(entries);start=1
  with path.open('w') as out:
   out.write(f'# Adapted licensed glTF; identical native attributes welded, no faces removed; see Urban/Licenses\nmtllib {name}.mtl\n')
   for label,(material,p,n,t,faces) in zip(labels,packed):
    out.write(f'o {label}\nusemtl {name}_m{material}\n')
    for a in p:out.write('v '+' '.join(f'{x:.8g}' for x in a)+'\n')
    for a in t:out.write('vt '+' '.join(f'{x:.8g}' for x in a)+'\n')
    for a in n:out.write('vn '+' '.join(f'{x:.8g}' for x in a)+'\n')
    for face in faces:out.write('f '+' '.join(f'{i+start}/{i+start}/{i+start}' for i in face)+'\n')
    start+=len(p)
  check,_=entries_from_obj(path);assert geometry_hash(check)==old_hash,'Compaction changed triangle attributes'
  model['losslessNativeCompaction']={'beforeVertices':sum(len(e[1]) for e in entries),'afterVertices':sum(len(e[1]) for e in packed),'perTriangleAttributeSha256':old_hash}
  (DEST/(name+'.json')).write_text(json.dumps(model,indent=2)+'\n');report.append({'source':name,'beforeBytes':old_size,'afterBytes':path.stat().st_size,**model['losslessNativeCompaction']})
 (DEST/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n');refresh_material_constants();rebuild_lods();print(json.dumps(report))
if __name__=='__main__':main()
