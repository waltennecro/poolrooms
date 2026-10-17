"""Analytic checks for the offline UV baker (NumPy/Pillow required)."""
from io import BytesIO
import json,struct
import numpy as np
from PIL import Image
from bake_gltf_uvs import bake,transform_uv,sample,tangent,disjoint_charts

uv=np.array([[0.,0.],[1.,0.],[0.,1.]])
info={'extensions':{'KHR_texture_transform':{'scale':[2,3],'rotation':np.pi/2,'offset':[-.5,.25]}}}
assert np.allclose(transform_uv(uv,info),[[-.5,.25],[-.5,2.25],[-3.5,.25]])
image=np.array([[[1.,0,0,1],[0,1,0,1]],[[0,0,1,1],[1,1,1,1]]])
assert np.allclose(sample(image,np.array([[.25,.25],[1.25,-.75]]),{}),[[1,0,0,1],[1,0,0,1]])
assert np.allclose(sample(image,np.array([[.5,.5]]),{}),[[.5,.5,.5,1]])
assert np.allclose(sample(image,np.array([[.5,.5]]),{},srgb=True)[0,:3],[.73535698]*3)
assert np.allclose(sample(image,np.array([[-1.,-1.]]),{'wrapS':33071,'wrapT':33071}),[[1,0,0,1]])

triangles=np.array([[[0,0],[1,0],[1,1]],[[0,0],[1,1],[0,1]],[[0,0],[1,0],[1,1]]],dtype=float)
charts=disjoint_charts(triangles)
assert len(charts)==2 and len(disjoint_charts(triangles[:2]))==1 and sum(map(len,charts))==3

binary=bytearray();views=[];accessors=[]
def array(data,kind,component=5126):
 a=np.array(data,dtype='<f4' if component==5126 else '<u2');payload=a.tobytes()
 while len(binary)%4:binary.append(0)
 view=len(views);views.append({'buffer':0,'byteOffset':len(binary),'byteLength':len(payload)});binary.extend(payload)
 index=len(accessors);accessors.append({'bufferView':view,'componentType':component,'count':len(a),'type':kind});return index
positions=array([[0,0,0],[1,0,0],[1,1,0],[0,1,0]],'VEC3');normals=array([[0,0,1]]*4,'VEC3')
primary=array([[0,0],[1,0],[1,1],[0,1]],'VEC2');atlas=array([[1,0],[1,1],[0,1],[0,0]],'VEC2');indices=array([0,1,2,0,2,3],'SCALAR',5123)
img=BytesIO();Image.new('RGBA',(2,2),(255,128,255,255)).save(img,format='PNG');payload=img.getvalue()
while len(binary)%4:binary.append(0)
image_view=len(views);views.append({'buffer':0,'byteOffset':len(binary),'byteLength':len(payload)});binary.extend(payload)
d={'asset':{'version':'2.0'},'buffers':[{'byteLength':len(binary)}],'bufferViews':views,'accessors':accessors,'images':[{'bufferView':image_view,'mimeType':'image/png'}],'textures':[{'source':0,'sampler':0}],'samplers':[{}],
'materials':[{'normalTexture':{'index':0,'scale':1},'occlusionTexture':{'index':0,'texCoord':1}}],
'meshes':[{'primitives':[{'attributes':{'POSITION':positions,'NORMAL':normals,'TEXCOORD_0':primary,'TEXCOORD_1':atlas},'indices':indices,'material':0}]}]}
raw=json.dumps(d).encode();raw+=b' '*((-len(raw))%4)
while len(binary)%4:binary.append(0)
source=struct.pack('<III',0x46546c67,2,28+len(raw)+len(binary))+struct.pack('<II',len(raw),0x4e4f534a)+raw+struct.pack('<II',len(binary),0x004e4942)+bytes(binary)
output,report=bake(source,32);n=struct.unpack_from('<I',output,12)[0];doc=json.loads(output[20:20+n]);data=output[28+n:]
assert doc['meshes'][0]['primitives'][0]['attributes']['TEXCOORD_0']!=primary
for prop in ['normalTexture','occlusionTexture']:assert doc['materials'][0][prop]['texCoord']==0
texture=doc['textures'][doc['materials'][0]['normalTexture']['index']];v=doc['bufferViews'][doc['images'][texture['source']]['bufferView']]
normal=np.asarray(Image.open(BytesIO(data[v['byteOffset']:v['byteOffset']+v['byteLength']])))[16,16,:3]/255
assert abs(normal[0]-.5)<.01 and normal[1]>.84 and normal[2]>.84,normal
assert report[0]['bakedPixels']>0 and report[1]['sourceUV']==1
print('UV baker: analytic transform/wrapping/bilinear tests and complete rotated-UV1 normal/AO GLB bake passed.')
