"""Rebuild only Urban LODs from shipped native source geometry, without UV rebaking."""
import json,re
from pathlib import Path
import numpy as np
from source_lods import cluster
from import_urban_sources import DEST,refresh_material_constants

def entries_from_obj(path):
 entries=[];labels=[];p=[];n=[];uv=[];f=[];start=1;label=None;material=None
 def finish():
  if label is not None:entries.append((material,np.array(p),np.array(n),np.array(uv),np.array(f)));labels.append(label)
 for line in path.read_text().splitlines():
  s=line.split()
  if not s:continue
  if s[0]=='o':
   finish();start+=len(p);p=[];n=[];uv=[];f=[];label=s[1];match=re.search(r'_m(\d+)$',label);assert match;material=int(match[1])
  elif s[0]=='v':p.append(list(map(float,s[1:])))
  elif s[0]=='vt':uv.append(list(map(float,s[1:])))
  elif s[0]=='vn':n.append(list(map(float,s[1:])))
  elif s[0]=='f':
   face=[]
   for token in s[1:]:
    a,b,c=map(int,token.split('/'));assert a==b==c;face.append(a-start)
   f.append(face)
 finish();return entries,labels

def main():
 catalog=json.loads((DEST/'catalog.json').read_text())
 for model in catalog['models']:
  name=model['id'];entries,labels=entries_from_obj(DEST/'Models'/(name+'.obj'));lo=np.array(model['minimum']);hi=np.array(model['maximum'])
  for level,grid,uv_grid in ([(1,32,16),(2,14,8)] if name=='U_CarConcept' else [(1,32,16),(2,12,8)]):
   groups=cluster(entries,grid,uv_grid,lo,hi,True);path=DEST/'Models'/(name+'_LOD'+str(level)+'.obj');start=1
   with path.open('w') as out:
    out.write(f'# LOD derived from the same licensed source; hard-normal seams retained\nmtllib {name}.mtl\n')
    for label,(material,p,n,t,faces) in zip(labels,groups):
     out.write(f'o {label}\nusemtl {name}_m{material}\n')
     for v in p:out.write('v '+' '.join(f'{x:.8g}' for x in v)+'\n')
     for v in t:out.write('vt '+' '.join(f'{x:.8g}' for x in v)+'\n')
     for v in n:out.write('vn '+' '.join(f'{x:.8g}' for x in v)+'\n')
     for face in faces:out.write('f '+' '.join(f'{i+start}/{i+start}/{i+start}' for i in face)+'\n')
     start+=len(p)
   model['lods'][level-1]['triangles']=sum(len(g[4]) for g in groups)
  (DEST/(name+'.json')).write_text(json.dumps(model,indent=2)+'\n');print(name,'LODs',[x['triangles'] for x in model['lods']],flush=True)
 (DEST/'catalog.json').write_text(json.dumps(catalog,indent=2)+'\n');refresh_material_constants()
if __name__=='__main__':main()
