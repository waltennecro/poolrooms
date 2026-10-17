"""Separate the four disconnected wheel assemblies in the pinned CC0 ToyCar.

No triangles, UVs or normals are invented. Components are welded only for
classification; the original per-corner attributes remain unchanged.
"""
import numpy as np


def split(entries):
    output=[];names=[];wheel_points={name:[] for name in ('Wheel_RL','Wheel_RR','Wheel_FL','Wheel_FR')}
    for material,positions,normals,uv,faces in entries:
        if material!=0:
            output.append((material,positions,normals,uv,faces));names.append('Body');continue
        parent=np.arange(len(positions));weld={}
        def find(i):
            while parent[i]!=i:
                parent[i]=parent[parent[i]];i=int(parent[i])
            return i
        for i,point in enumerate(positions):
            key=tuple(np.round(point,9))
            if key in weld:parent[find(i)]=find(weld[key])
            else:weld[key]=i
        for a,b,c in faces:parent[find(int(b))]=parent[find(int(c))]=find(int(a))
        components={}
        for i,face in enumerate(faces):components.setdefault(find(int(face[0])),[]).append(i)
        labels=np.full(len(faces),'Body',dtype='<U8')
        for indices in components.values():
            points=positions[np.unique(faces[indices])];lo,hi=points.min(0),points.max(0)
            # The source is pinned. Both tyres and separate hub/rim components fit
            # wholly inside these four measured wheel volumes; body panels do not.
            if lo[1]>=.00098 and hi[1]<=.00627 and (lo[0]>.0057 or hi[0]<-.0057):
                front=lo[2]>.01065 and hi[2]<.01606
                rear=lo[2]>-.01330 and hi[2]<-.00790
                if front or rear:
                    name='Wheel_'+('F' if front else 'R')+('L' if hi[0]<0 else 'R')
                    labels[indices]=name;wheel_points[name].append(points)
        for name in ('Body',*wheel_points):
            selected=faces[labels==name]
            if not len(selected):continue
            used,inverse=np.unique(selected,return_inverse=True)
            output.append((material,positions[used],normals[used],uv[used],inverse.reshape(-1,3)));names.append(name)
    wheels=[]
    for name,parts in wheel_points.items():
        assert parts,'Missing native wheel: '+name
        points=np.concatenate(parts);lo,hi=points.min(0),points.max(0)
        center=(lo+hi)*.5;radius=float(max(hi[1]-lo[1],hi[2]-lo[2])*.5)
        assert .0025<radius<.0028 and hi[0]-lo[0]>.0028,(name,lo,hi)
        wheels.append({'part':name,'center':center.tolist(),'radius':radius,'front':name.startswith('Wheel_F')})
    assert sum(len(e[4]) for e in entries)==sum(len(e[4]) for e in output)
    return output,names,wheels
