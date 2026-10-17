"""Deterministic native vertex clustering; optional hard-normal seam preservation."""
import numpy as np

def compact(entries):
 """Weld only identical native position/normal/UV tuples, retaining all faces."""
 output=[]
 for material,p,n,t,faces in entries:
  native=np.column_stack((p,n,t));unique,inverse=np.unique(native,axis=0,return_inverse=True)
  result=(material,unique[:,:3],unique[:,3:6],unique[:,6:8],inverse[faces])
  # Triangle attributes, ordering, winding and UV/normal seams remain exact.
  assert np.array_equal(native[faces],unique[result[4]])
  output.append(result)
 return output

def cluster(entries,grid,uv_grid,lo,hi,preserve_normals=False):
 groups=[];epsilon=float((hi-lo).max())/grid
 for material,positions,normals,uv,indices in entries:
  columns=[np.rint((positions-lo)/epsilon),np.floor(uv*uv_grid)]
  if preserve_normals:
   # Six dominant-axis hemispheres retain opposing/hard faces without retaining
   # hundreds of tiny normal buckets that made distant LODs almost as expensive
   # as their source. A bucket's signed dominant component cannot cancel to zero.
   axis=np.argmax(np.abs(normals),axis=1);sign=(normals[np.arange(len(normals)),axis]>=0).astype(int)
   columns.append((axis*2+sign)[:,None])
  key=np.column_stack(columns).astype(np.int64);_,inverse=np.unique(key,axis=0,return_inverse=True)
  count=int(inverse.max())+1;weight=np.bincount(inverse)
  def average(a):return np.stack([np.bincount(inverse,weights=a[:,i],minlength=count)/weight for i in range(a.shape[1])],axis=1)
  p=average(positions);n=average(normals);n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-10);t=average(uv)
  faces=inverse[indices];keep=(faces[:,0]!=faces[:,1])&(faces[:,1]!=faces[:,2])&(faces[:,0]!=faces[:,2]);faces=faces[keep]
  area=np.linalg.norm(np.cross(p[faces[:,1]]-p[faces[:,0]],p[faces[:,2]]-p[faces[:,0]]),axis=1);faces=faces[area>1e-14]
  if preserve_normals:assert np.all(np.linalg.norm(n[np.unique(faces)],axis=1)>.99),'LOD cancelled an authored shading normal'
  groups.append((material,p,n,t,faces))
 return groups
