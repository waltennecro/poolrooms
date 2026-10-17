"""Bake a static glTF's texture transforms and second UV set to unique UV1 charts.

Original source bytes remain untouched. Requires NumPy/Pillow. Uses bilinear
sampling, source sampler wrapping, UV1 AO and tangent-frame normal conversion.
Intended for verified static sources with an existing unique UV1 unwrap.
"""
from copy import deepcopy
from io import BytesIO
import json,struct
import numpy as np
from PIL import Image


def transform_uv(uv,info):
    t=info.get('extensions',{}).get('KHR_texture_transform',{})
    angle=t.get('rotation',0);co,si=np.cos(angle),np.sin(angle)
    return (uv*np.array(t.get('scale',[1,1])))@np.array([[co,si],[-si,co]])+np.array(t.get('offset',[0,0]))


def sample(image,uv,sampler,srgb=False):
    if srgb:
        image=image.copy();rgb=image[:,:,:3];image[:,:,:3]=np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
    h,w=image.shape[:2];point=uv*np.array([w,h])-.5;lo=np.floor(point).astype(int);weight=point-lo
    def index(i,size,mode):
        if mode==10497:return i%size
        if mode==33071:return np.clip(i,0,size-1)
        if mode==33648:
            i=i%(2*size);return np.where(i<size,i,2*size-i-1)
        raise ValueError('Unsupported sampler wrapping')
    result=np.zeros((len(uv),4),dtype=float)
    for y in range(2):
        for x in range(2):
            xx=index(lo[:,0]+x,w,sampler.get('wrapS',10497));yy=index(lo[:,1]+y,h,sampler.get('wrapT',10497))
            factor=(weight[:,0] if x else 1-weight[:,0])*(weight[:,1] if y else 1-weight[:,1])
            result+=image[yy,xx]*factor[:,None]
    if srgb:
        rgb=result[:,:3];result[:,:3]=np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)
    return result


def tangent(positions,uv,normals):
    e1,e2=positions[1]-positions[0],positions[2]-positions[0];a,b=uv[1]-uv[0],uv[2]-uv[0];den=a[0]*b[1]-a[1]*b[0]
    if abs(den)<1e-12:raise ValueError('Degenerate tangent UV chart')
    t=(e1*b[1]-e2*a[1])/den;bitangent=(-e1*b[0]+e2*a[0])/den
    t=t-normals*np.sum(normals*t,axis=1)[:,None];t/=np.maximum(np.linalg.norm(t,axis=1,keepdims=True),1e-12)
    b=np.cross(normals,t);b*=np.where(np.sum(b*bitangent,axis=1)>=0,1,-1)[:,None]
    return t,b


def disjoint_charts(triangles):
    """Separate overlapping source AO islands, preserving non-overlapping neighbors."""
    triangles=np.asarray(triangles,dtype=float);groups=[]
    def overlap(a,b):
        edges=np.concatenate((np.roll(a,-1,axis=0)-a,np.roll(b,-1,axis=0)-b));axes=np.column_stack((-edges[:,1],edges[:,0]));pa=a@axes.T;pb=b@axes.T
        return np.all(np.minimum(pa.max(axis=0),pb.max(axis=0))-np.maximum(pa.min(axis=0),pb.min(axis=0))>np.linalg.norm(axes,axis=1)*1e-9)
    for i,triangle in enumerate(triangles):
        edge_a,edge_b=triangle[1]-triangle[0],triangle[2]-triangle[0]
        if abs(edge_a[0]*edge_b[1]-edge_a[1]*edge_b[0])<1e-18:
            # A zero-area source UV triangle cannot overlap positive area.
            # Do not insert it into bins and scan all earlier collapsed triangles.
            if not groups:groups.append(([],{}))
            groups[0][0].append(i);continue
        low=np.floor(triangle.min(axis=0)*128).astype(int);high=np.floor(triangle.max(axis=0)*128).astype(int)
        cells=[(x,y) for y in range(low[1],high[1]+1) for x in range(low[0],high[0]+1)]
        for indices,bins in groups:
            candidates=set(j for cell in cells for j in bins.get(cell,[]))
            if not any(overlap(triangle,triangles[j]) for j in candidates):break
        else:
            indices=[];bins={};groups.append((indices,bins))
        indices.append(i)
        for cell in cells:bins.setdefault(cell,[]).append(i)
    return [np.array(indices) for indices,bins in groups]


def bake(raw,size=2048,skip_uniform=False,weld_charts=False):
    n=struct.unpack_from('<I',raw,12)[0];doc=deepcopy(json.loads(raw[20:20+n]));binary=bytearray(raw[28+n:])
    assert not doc.get('skins') and not doc.get('animations')
    def accessor(i):
        a=doc['accessors'][i];assert not a.get('sparse') and not a.get('normalized');v=doc['bufferViews'][a['bufferView']]
        dtype={5126:'<f4',5125:'<u4',5123:'<u2',5121:'u1'}[a['componentType']];count={'SCALAR':1,'VEC2':2,'VEC3':3,'VEC4':4}[a['type']]
        return np.ndarray((a['count'],count),dtype=dtype,buffer=binary,offset=v.get('byteOffset',0)+a.get('byteOffset',0),strides=(v.get('byteStride',count*np.dtype(dtype).itemsize),np.dtype(dtype).itemsize)).copy()
    primitives=[p for m in doc['meshes'] for p in m['primitives']]
    used=sorted({p.get('material',0) for p in primitives});materials=doc['materials'];doc['materials']=[materials[i] for i in used]
    for p in primitives:
        p['material']=used.index(p.get('material',0));p.pop('extensions',None)
    if skip_uniform:
        # Constant glass/emissive parts have no texture coordinates in some
        # sources. They need no atlas; keep their original geometry untouched.
        def textured(p):
            m=doc['materials'][p['material']];pbr=m.get('pbrMetallicRoughness',{})
            return 'COLOR_0' in p['attributes'] or any(k in pbr for k in ('baseColorTexture','metallicRoughnessTexture')) or any(k in m for k in ('normalTexture','occlusionTexture','emissiveTexture'))
        primitives=[p for p in primitives if textured(p)]
    for p in primitives:
        assert 'TEXCOORD_1' in p['attributes'],'Source has no atlas UV1'
    original_uv={id(p):accessor(p['attributes']['TEXCOORD_1']) for p in primitives}
    native_faces={};chart_counts={}
    for material in range(len(doc['materials'])):
        charts=[]
        for primitive in primitives:
            if primitive['material']!=material:continue
            key=id(primitive);uv=original_uv[key];faces=accessor(primitive['indices']).reshape((-1,3));native_faces[key]=np.zeros((len(faces),3,2))
            chart_vertices=None
            if weld_charts:
                # Hard-normal/export seams can duplicate a vertex without making
                # a UV chart boundary. Weld classification keys only, preserving
                # every original position, normal, UV and triangle for baking.
                values=np.column_stack((np.round(accessor(primitive['attributes']['POSITION']),7),np.round(uv,7)))
                _,chart_vertices=np.unique(values,axis=0,return_inverse=True)
            # A mirrored UV fold cannot share an atlas chart. Connect actual edges only,
            # retaining separate charts across UV orientation flips and point contacts.
            parents=list(range(len(faces)));edges={}
            triangle_uv=uv[faces];a=triangle_uv[:,1]-triangle_uv[:,0];b=triangle_uv[:,2]-triangle_uv[:,0];sign=np.sign(a[:,0]*b[:,1]-a[:,1]*b[:,0])
            def find(x):
                while parents[x]!=x:parents[x]=parents[parents[x]];x=parents[x]
                return x
            for i,face in enumerate(faces):
                if chart_vertices is not None:face=chart_vertices[face]
                for x,y in [(face[0],face[1]),(face[1],face[2]),(face[2],face[0])]:
                    edge=(min(x,y),max(x,y));prior=edges.get(edge)
                    if prior is not None and sign[i]==sign[prior]:parents[find(i)]=find(prior)
                    else:edges[edge]=i
            components={}
            for i in range(len(faces)):components.setdefault(find(i),[]).append(i)
            for indices in components.values():
                indices=np.array(indices)
                for group in disjoint_charts(triangle_uv[indices]):
                    selected=indices[group];points=triangle_uv[selected];lo=points.min(axis=(0,1));extent=points.max(axis=(0,1))-lo
                    if weld_charts and np.any(extent<=1e-8):
                        # A constant/line AO coordinate is valid source sampling,
                        # but cannot seed an atlas chart. Seed only the target UVs
                        # from UV0 (or a small triangle); retain original_uv above
                        # so AO still samples the exact original constant/line.
                        uv0=accessor(primitive['attributes']['TEXCOORD_0'])
                        for face_index in selected:
                            target=uv0[faces[face_index]].copy();target_lo=target.min(0);target_extent=target.max(0)-target_lo
                            if np.any(target_extent<=1e-8):target=np.array([[0.,0.],[.02,0.],[0.,.02]]);target_lo=target.min(0);target_extent=target.max(0)-target_lo
                            charts.append((key,np.array([face_index]),target_lo,target_extent,target[None,:,:]))
                        continue
                    assert np.all(extent>1e-8),'Degenerate UV1 chart'
                    charts.append((key,selected,lo,extent,points))
        chart_counts[material]=len(charts)
        order=sorted(range(len(charts)),key=lambda i:(-charts[i][3][1],-charts[i][3][0],i))
        padding=min(8,max(1,size//16))
        def pack(scale):
            x=y=row=0;placed={}
            for i in order:
                w,h=np.ceil(charts[i][3]*size*scale).astype(int)+padding*2
                if w>size or h>size:return None
                if x+w>size:x=0;y+=row;row=0
                if y+h>size:return None
                placed[i]=(x+padding,y+padding);x+=int(w);row=max(row,int(h))
            return placed
        low,high=0.,1.
        for iteration in range(32):
            middle=(low+high)*.5
            if pack(middle) is None:high=middle
            else:low=middle
        places=pack(low);assert places is not None and low>0,('Atlas chart budget exceeded',material,len(charts),size)
        if weld_charts:print('UV atlas material',material,'charts',len(charts),flush=True)
        for i,(key,indices,lo,extent,points) in enumerate(charts):native_faces[key][indices]=(points-lo)*low+np.array(places[i])/size
    def image_for(info):
        tex=doc['textures'][info['index']];source=tex.get('extensions',{}).get('EXT_texture_webp',{}).get('source',tex.get('source'))
        assert source is not None,'Unsupported compressed texture source'
        im=doc['images'][source];view=doc['bufferViews'][im['bufferView']];offset=view.get('byteOffset',0)
        return np.asarray(Image.open(BytesIO(binary[offset:offset+view['byteLength']])).convert('RGBA'),dtype=float)/255,doc.get('samplers',[{}])[tex.get('sampler',0)]
    def add_image(array):
        output=BytesIO();Image.fromarray(np.uint8(np.clip(array,0,1)*255+.5)).save(output,format='PNG');data=output.getvalue()
        while len(binary)%4:binary.append(0)
        view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':len(data)});binary.extend(data)
        index=len(doc['images']);doc['images'].append({'bufferView':view,'mimeType':'image/png'});texture=len(doc['textures']);doc['textures'].append({'source':index,'sampler':0});return texture
    reports=[]
    for index,mat in enumerate(doc['materials']):
        if weld_charts and any(p['material']==index and 'COLOR_0' in p['attributes'] for p in primitives):
            pbr=mat.setdefault('pbrMetallicRoughness',{})
            if 'baseColorTexture' not in pbr:pbr['baseColorTexture']={'index':add_image(np.ones((2,2,4))),'texCoord':0}
        pbr=mat.get('pbrMetallicRoughness',{});bindings=[(pbr,k) for k in ['baseColorTexture','metallicRoughnessTexture'] if k in pbr]+[(mat,k) for k in ['normalTexture','occlusionTexture','emissiveTexture'] if k in mat]
        for owner,key in bindings:
            info=owner[key];image,sampler=image_for(info);srgb=key in ('baseColorTexture','emissiveTexture')
            if srgb:
                rgb=image[:,:,:3];image[:,:,:3]=np.where(rgb<=.04045,rgb/12.92,((rgb+.055)/1.055)**2.4)
            tr=info.get('extensions',{}).get('KHR_texture_transform',{});uv_set=tr.get('texCoord',info.get('texCoord',0));assert uv_set in (0,1)
            atlas=np.zeros((size,size,4),dtype=float);covered=np.zeros((size,size),dtype=bool);confidence=np.zeros((size,size));pixels=0
            for primitive in primitives:
                if primitive['material']!=index:continue
                attr=primitive['attributes'];uv0=accessor(attr['TEXCOORD_0']);uv1=native_faces[id(primitive)];source_uv=uv0 if uv_set==0 else original_uv[id(primitive)]
                assert np.all(uv1>=-1e-5) and np.all(uv1<=1.00001),'UV1 is not a bounded atlas'
                positions=accessor(attr['POSITION']);normals=accessor(attr['NORMAL']);faces=accessor(primitive['indices']).reshape((-1,3))
                source_tangents=accessor(attr['TANGENT']) if weld_charts and uv_set==0 and 'TANGENT' in attr else None
                colours=accessor(attr['COLOR_0']) if weld_charts and 'COLOR_0' in attr and key=='baseColorTexture' else None
                for face_index,face in enumerate(faces):
                    target_uv=uv1[face_index];target=target_uv*size-.5;low=np.maximum(0,np.ceil(target.min(axis=0)).astype(int));high=np.minimum(size-1,np.floor(target.max(axis=0)).astype(int))
                    if np.any(high<low):continue
                    x,y=np.meshgrid(np.arange(low[0],high[0]+1),np.arange(low[1],high[1]+1));points=np.column_stack((x.ravel(),y.ravel()))
                    a,b,c=target;den=(b[1]-c[1])*(a[0]-c[0])+(c[0]-b[0])*(a[1]-c[1]);
                    if abs(den)<1e-10:continue
                    u=((b[1]-c[1])*(points[:,0]-c[0])+(c[0]-b[0])*(points[:,1]-c[1]))/den
                    v=((c[1]-a[1])*(points[:,0]-c[0])+(a[0]-c[0])*(points[:,1]-c[1]))/den
                    keep=(u>=-1e-7)&(v>=-1e-7)&(u+v<=1.0000001);points=points[keep]
                    if not len(points):continue
                    weights=np.column_stack((u[keep],v[keep],1-u[keep]-v[keep]));src=weights@source_uv[face];rgba=sample(image,transform_uv(src,info),sampler)
                    if colours is not None:
                        colour=weights@colours[face];rgba[:,:3]*=colour[:,:3]
                        if colour.shape[1]==4:rgba[:,3]*=colour[:,3]
                    if srgb:
                        rgb=rgba[:,:3];rgba[:,:3]=np.where(rgb<=.0031308,rgb*12.92,1.055*np.maximum(rgb,0)**(1/2.4)-.055)
                    if key=='normalTexture':
                        n=weights@normals[face];n/=np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-12)
                        if source_tangents is not None:
                            authored=weights@source_tangents[face];t0=authored[:,:3]-n*np.sum(authored[:,:3]*n,axis=1)[:,None];length=np.linalg.norm(t0,axis=1,keepdims=True)
                            assert np.all(length>1e-9),'Invalid authored tangent basis'
                            t0/=length;b0=np.cross(n,t0)*np.where(authored[:,3]>=0,1,-1)[:,None]
                        else:
                            edges=source_uv[face][1:]-source_uv[face][0];det=edges[0,0]*edges[1,1]-edges[0,1]*edges[1,0]
                            if weld_charts and abs(det)<1e-12 and np.linalg.norm(np.cross(positions[face][1]-positions[face][0],positions[face][2]-positions[face][0]))<1e-12:continue
                            t0,b0=tangent(positions[face],source_uv[face],n)
                        t1,b1=tangent(positions[face],target_uv,n)
                        mapped=rgba[:,:3]*2-1;mapped[:,:2]*=info.get('scale',1)
                        world=t0*mapped[:,0,None]+b0*mapped[:,1,None]+n*mapped[:,2,None];world/=np.maximum(np.linalg.norm(world,axis=1,keepdims=True),1e-12)
                        rgba[:,:3]=np.column_stack((np.sum(world*t1,axis=1),np.sum(world*b1,axis=1),np.sum(world*n,axis=1)))*.5+.5
                    xx,yy=points[:,0],points[:,1];prior=covered[yy,xx]
                    if np.any(prior) and np.max(np.abs(atlas[yy[prior],xx[prior]]-rgba[prior]))>.12:raise ValueError(f'UV1 charts overlap with conflicting texels: material {index}, {key}, face {face.tolist()}, {int(prior.sum())} pixels, error {float(np.max(np.abs(atlas[yy[prior],xx[prior]]-rgba[prior])))}, new depth {weights[prior].min(axis=1).tolist()}, old depth {confidence[yy[prior],xx[prior]].tolist()}')
                    atlas[yy,xx]=rgba;covered[yy,xx]=True;confidence[yy,xx]=weights.min(axis=1);pixels+=len(points)
            assert pixels>0,'No baked texels'
            coverage=int(covered.sum())
            # Nearest chart dilation: eight pixels of seam padding for compressed mipmaps.
            for iteration in range(8):
                previous=covered.copy();original=atlas.copy()
                for dx,dy in [(-1,0),(1,0),(0,-1),(0,1),(-1,-1),(1,1),(-1,1),(1,-1)]:
                    ys=slice(max(0,dy),min(size,size+dy));xs=slice(max(0,dx),min(size,size+dx));sy=slice(max(0,-dy),min(size,size-dy));sx=slice(max(0,-dx),min(size,size-dx))
                    take=previous[sy,sx]&~covered[ys,xs];atlas[ys,xs][take]=original[sy,sx][take];covered[ys,xs][take]=True
            owner[key]={'index':add_image(atlas),'texCoord':0}
            if key=='occlusionTexture':owner[key]['strength']=info.get('strength',1)
            if key=='normalTexture':owner[key]['scale']=1
            reports.append({'material':index,'texture':key,'sourceUV':uv_set,'bakedPixels':coverage,'resolution':size,'uniqueCharts':chart_counts[index]})
            if key=='baseColorTexture' and weld_charts and any(p['material']==index and 'COLOR_0' in p['attributes'] for p in primitives):reports[-1]['vertexColoursApplied']=True
    def add_accessor(array,kind,component):
        array=np.asarray(array,dtype='<f4' if component==5126 else '<u4');payload=array.tobytes()
        while len(binary)%4:binary.append(0)
        view=len(doc['bufferViews']);doc['bufferViews'].append({'buffer':0,'byteOffset':len(binary),'byteLength':len(payload)});binary.extend(payload)
        index=len(doc['accessors']);doc['accessors'].append({'bufferView':view,'componentType':component,'count':len(array),'type':kind});return index
    for p in primitives:
        faces=accessor(p['indices']).reshape((-1,3));attr=p['attributes'];positions=accessor(attr['POSITION'])[faces].reshape((-1,3));normals=accessor(attr['NORMAL'])[faces].reshape((-1,3))
        # Split chart seams; the native importer can weld identical position/normal/UV tuples.
        p['attributes']={'POSITION':add_accessor(positions,'VEC3',5126),'NORMAL':add_accessor(normals,'VEC3',5126),'TEXCOORD_0':add_accessor(native_faces[id(p)].reshape((-1,2)),'VEC2',5126)}
        p['indices']=add_accessor(np.arange(len(positions)),'SCALAR',5125)
    doc['extensionsRequired']=[x for x in doc.get('extensionsRequired',[]) if x not in ('KHR_texture_transform','EXT_texture_webp')];doc.pop('extensions',None)
    doc['buffers'][0]['byteLength']=len(binary)
    while len(binary)%4:binary.append(0)
    data=json.dumps(doc,separators=(',',':')).encode();data+=b' '*((-len(data))%4)
    total=12+8+len(data)+8+len(binary)
    return struct.pack('<III',0x46546c67,2,total)+struct.pack('<II',len(data),0x4e4f534a)+data+struct.pack('<II',len(binary),0x004e4942)+bytes(binary),reports
