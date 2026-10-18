"""Analytical checks of UV-preserving architectural subtraction; no Unity required."""
from pathlib import Path
import numpy as np
from building_geometry import cut
p=np.array([[-2.,-2.,0.],[2.,-2.,0.],[0.,2.,0.]])
n=np.tile([0.,0.,1.],(3,1));uv=(p[:,:2]+2)*.25
original=[[0,p,n,uv,np.array([[0,1,2]])]]
result=cut(original,[(np.array([-.5,-.5,-.1]),np.array([.5,.5,.1]))])
area=0
for _,points,normals,coords,faces in result:
 t=points[faces];area+=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1).sum()*.5
 assert np.allclose(coords,(points[:,:2]+2)*.25),'UV discontinuity at a clipped edge'
 assert np.allclose(normals,[0,0,1]),'Clipping changed the authored normal'
assert abs(area-7)<1e-8,(area,'A centred 1m2 opening must remove exactly 1 from an 8m2 triangle')
assert cut(original,[(np.array([-3.,-3.,-.1]),np.array([3.,3.,.1]))])==[],'Contained triangle survived subtraction'
uncut=cut(original,[(np.array([8.,8.,8.]),np.array([9.,9.,9.]))]);assert np.allclose(uncut[0][1],p)
repeated=cut(result,[(np.array([-.5,-.5,-.1]),np.array([.5,.5,.1]))]);second=0
for _,points,_,_,faces in repeated:
 t=points[faces];second+=np.linalg.norm(np.cross(t[:,1]-t[:,0],t[:,2]-t[:,0]),axis=1).sum()*.5
assert abs(second-area)<1e-8,'LOD re-clipping duplicates surface area'
print('Building cutter: exact analytical areas, interpolated UVs/normals, full removal, disjoint preservation and repeated clipping verified. Not a Unity test.')
