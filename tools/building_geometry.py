"""Attribute-preserving offline clipping for imported architectural shells.

Box subtraction operates on triangles and interpolates positions, unit normals and
UVs at every cut. It never fills an opening with a collider or closed polygon.
"""
import numpy as np


def halfspace(poly,axis,bound,lower):
    if not poly:return []
    result=[]
    def signed(v):return (v[axis]-bound)*(1 if lower else -1)
    prev=poly[-1];a=signed(prev)
    for current in poly:
        b=signed(current)
        if (a>=0)!=(b>=0):
            t=a/(a-b);result.append(prev+(current-prev)*t)
        if b>=0:result.append(current)
        prev=current;a=b
    return result


def subtract_box(poly,minimum,maximum):
    # Keep the outside of each successive plane; only the remaining inside needs
    # processing by the next plane. The result has no overlapping area fragments.
    result=[];inside=poly
    for axis in range(3):
        for bound,lower in [(minimum[axis],True),(maximum[axis],False)]:
            outside=halfspace(inside,axis,bound,not lower)
            if len(outside)>=3:result.append(outside)
            inside=halfspace(inside,axis,bound,lower)
            if len(inside)<3:return result
    return result


def cut(entries,boxes):
    output=[]
    for material,positions,normals,uv,faces in entries:
        vertices=np.concatenate((positions,normals,uv),axis=1);result=[]
        for face in faces:
            fragments=[[v for v in vertices[face]]]
            for minimum,maximum in boxes:
                remaining=[]
                for poly in fragments:
                    points=np.array(poly)[:,:3]
                    if np.any(points.max(0)<minimum) or np.any(points.min(0)>maximum):remaining.append(poly)
                    else:remaining.extend(subtract_box(poly,minimum,maximum))
                fragments=remaining
                if not fragments:break
            for poly in fragments:
                for i in range(1,len(poly)-1):
                    tri=np.array([poly[0],poly[i],poly[i+1]])
                    if np.linalg.norm(np.cross(tri[1,:3]-tri[0,:3],tri[2,:3]-tri[0,:3]))>1e-9:result.extend(tri)
        if not result:continue
        values=np.array(result);norm=values[:,3:6];length=np.linalg.norm(norm,axis=1,keepdims=True);assert np.all(length>1e-8)
        output.append([material,values[:,:3],norm/length,values[:,6:8],np.arange(len(values),dtype=np.uint32).reshape(-1,3)])
    return output


def building_boxes():
    # Source shells use a 10m x 10m, four-storey adapter; physical interiors remain
    # owned by City11Architecture. Clearance is wider than visible door/window frames.
    boxes=[([-4.89,-.5,-4.89],[4.89,12.65,4.89]),([-1.2,-.5,-6],[1.2,2.9,-4.7])]
    for floor in range(4):
        y=.12+floor*3.2
        for side in range(4):
            if floor==0 and side==2:continue
            low=[-1.5,y+.8,-6];high=[1.5,y+2.55,-4.7]
            if side==0:low[2],high[2]=4.7,6
            elif side in [1,3]:
                low[0],low[2]=(-6 if side==3 else 4.7),-1.5
                high[0],high[2]=(-4.7 if side==3 else 6),1.5
            boxes.append((low,high))
    return [(np.array(a),np.array(b)) for a,b in boxes]


def read_obj(path,material_names):
    positions=[];normals=[];uv=[];groups={};material=None
    for line in path.read_text().splitlines():
        p=line.split()
        if not p:continue
        if p[0]=='v':positions.append(list(map(float,p[1:])))
        elif p[0]=='vn':normals.append(list(map(float,p[1:])))
        elif p[0]=='vt':uv.append(list(map(float,p[1:])))
        elif p[0]=='usemtl':material=material_names.index(p[1])
        elif p[0]=='f':
            assert len(p)==4
            result=groups.setdefault(material,[])
            for vertex in p[1:]:
                a,b,c=map(int,vertex.split('/'));result.append(positions[a-1]+normals[c-1]+uv[b-1])
    return [[m,np.array(v)[:,:3],np.array(v)[:,3:6],np.array(v)[:,6:8],np.arange(len(v),dtype=np.uint32).reshape(-1,3)] for m,v in groups.items()]


def write_obj(path,entries,material_names):
    with path.open('w') as f:
        f.write('# CC BY 4.0 Bistro building adaptation; see Buildings/Licenses\nmtllib '+path.stem.split('_LOD')[0]+'.mtl\n');start=1
        for material,p,n,uv,faces in entries:
            f.write('o '+path.stem+'_m'+str(material)+'\nusemtl '+material_names[material]+'\n')
            for prefix,values in [('v',p),('vt',uv),('vn',n)]:
                for v in values:f.write(prefix+' '+' '.join(f'{x:.9g}' for x in v)+'\n')
            for face in faces:f.write('f '+' '.join(f'{i+start}/{i+start}/{i+start}' for i in face)+'\n')
            start+=len(p)
