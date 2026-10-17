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

# Uniform glass without an atlas and degenerate source AO coordinates must not
# force fake texture coordinates or change the original sampling of that AO.
from copy import deepcopy
zero=array([[0,0]]*4,'VEC2')
d2=deepcopy(d);d2['buffers'][0]['byteLength']=len(binary);d2['meshes'][0]['primitives'][0]['attributes']['TEXCOORD_1']=zero
d2['materials'].append({'name':'Constant glass','pbrMetallicRoughness':{'metallicFactor':0,'roughnessFactor':0}})
uniform=deepcopy(d2['meshes'][0]['primitives'][0]);uniform['material']=1;d2['meshes'][0]['primitives'].append(uniform)
def encode(doc):
 payload=json.dumps(doc,separators=(',',':')).encode();payload+=b' '*((-len(payload))%4)
 return struct.pack('<III',0x46546c67,2,28+len(payload)+len(binary))+struct.pack('<II',len(payload),0x4e4f534a)+payload+struct.pack('<II',len(binary),0x004e4942)+bytes(binary)
output,reports=bake(encode(d2),64,skip_uniform=True,weld_charts=True);n=struct.unpack_from('<I',output,12)[0];doc=json.loads(output[20:20+n]);data=output[28+n:]
assert doc['meshes'][0]['primitives'][1]['attributes']['POSITION']==positions,'Uniform geometry changed'
assert doc['meshes'][0]['primitives'][1]['attributes']['TEXCOORD_1']==zero,'Uniform source UV changed'
assert all(r['material']==0 and r['bakedPixels']>0 for r in reports)
ao=doc['textures'][doc['materials'][0]['occlusionTexture']['index']];view=doc['bufferViews'][doc['images'][ao['source']]['bufferView']]
ao_image=np.asarray(Image.open(BytesIO(data[view['byteOffset']:view['byteOffset']+view['byteLength']])));covered=ao_image[:,:,3]>0
assert covered.any() and np.all(ao_image[:,:,0][covered]==255),'Constant source AO sampled from the new target UV by mistake'
print('UV baker: degenerate AO target fallback, original constant AO sampling and untouched uniform glass passed.')

# Authored glTF tangents are authoritative when UV0 is degenerate, and vertex
# colours multiply linear albedo before encoding the new colour texture as sRGB.
colour=array([[.25,.5,.75,1]]*4,'VEC4');authored=array([[0,1,0,-1]]*4,'VEC4')
d3=deepcopy(d2);d3['bufferViews']=deepcopy(views);d3['accessors']=deepcopy(accessors);d3['buffers'][0]['byteLength']=len(binary)
d3['meshes'][0]['primitives'][0]['attributes'].update(COLOR_0=colour,TANGENT=authored)
output,reports=bake(encode(d3),64,skip_uniform=True,weld_charts=True);n=struct.unpack_from('<I',output,12)[0];doc=json.loads(output[20:20+n]);data=output[28+n:]
def mapped(info):
 tex=doc['textures'][info['index']];view=doc['bufferViews'][doc['images'][tex['source']]['bufferView']]
 return np.asarray(Image.open(BytesIO(data[view['byteOffset']:view['byteOffset']+view['byteLength']])))
albedo=mapped(doc['materials'][0]['pbrMetallicRoughness']['baseColorTexture']);inside=albedo[:,:,3]>0
expected=np.array([.5370987,.735357,.880825])*255
assert inside.any() and np.max(np.abs(albedo[:,:,:3][inside].astype(float)-expected))<1.1
normal=mapped(doc['materials'][0]['normalTexture']);inside=normal[:,:,3]>0
assert np.all(normal[:,:,1][inside]>210) and np.all(abs(normal[:,:,0][inside].astype(float)-128)<2),'Authored tangent frame ignored'
assert any(r.get('vertexColoursApplied') for r in reports)
print('UV baker: authored tangent handedness and linear vertex-colour albedo baking passed.')

# The real wood/leather source embeds WebP via EXT_texture_webp. Decode the
# extension's actual image and emit ordinary PNG bindings for native Unity.
img=BytesIO();Image.new('RGBA',(2,2),(255,128,255,255)).save(img,format='WEBP',lossless=True);payload=img.getvalue()
while len(binary)%4:binary.append(0)
webp_view=len(views);views.append({'buffer':0,'byteOffset':len(binary),'byteLength':len(payload)});binary.extend(payload)
while len(binary)%4:binary.append(0)
d4=deepcopy(d3);d4['bufferViews']=deepcopy(views);d4['buffers'][0]['byteLength']=len(binary);d4['images'][0]={'bufferView':webp_view,'mimeType':'image/webp'};d4['textures'][0]={'sampler':0,'extensions':{'EXT_texture_webp':{'source':0}}};d4['extensionsRequired']=['EXT_texture_webp']
output,reports=bake(encode(d4),64,skip_uniform=True,weld_charts=True);n=struct.unpack_from('<I',output,12)[0];doc=json.loads(output[20:20+n])
assert 'EXT_texture_webp' not in doc.get('extensionsRequired',[]) and all('source' in doc['textures'][doc['materials'][0][key]['index']] for key in ('normalTexture','occlusionTexture'))
print('UV baker: embedded EXT_texture_webp decoded and live material bindings replaced with native PNG passed.')

# Opposite faces of a thin piece must retain opposite lighting normals after
# simplification, rather than averaging into the zero normals found in Car LOD2.
from source_lods import cluster
p=np.array([[0,0,0],[1,0,0],[1,1,0],[0,1,0]]*2,dtype=float)
n=np.array([[0,0,1]]*4+[[0,0,-1]]*4,dtype=float);u=np.array([[0,0],[1,0],[1,1],[0,1]]*2,dtype=float)
f=np.array([[0,1,2],[0,2,3],[4,6,5],[4,7,6]])
low=cluster([(0,p,n,u,f)],12,8,p.min(0),p.max(0),preserve_normals=True)[0]
assert len(low[4])==4 and np.allclose(np.linalg.norm(low[2],axis=1),1)
assert (low[2][:,2]>.99).any() and (low[2][:,2]<-.99).any()
print('Native LOD clustering: thin opposite faces preserve distinct unit shading normals passed.')
