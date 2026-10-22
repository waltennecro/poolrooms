"""Technical renders of exported Core geometry, never presented as Unity screenshots.

Run the verification exporter first: --courtyard-atlas /tmp/hotel-atlas.
Requires Blender; optional and outside all game/build dependencies.
"""
import bpy,json,math
from pathlib import Path
from mathutils import Vector
import sys
args=sys.argv[sys.argv.index('--')+1:]
if len(args)!=2:raise SystemExit('Usage: blender -b --python tools/render_courtyard_preview.py -- <atlas directory> <new image directory>')
atlas=Path(args[0]).resolve();output=Path(args[1]).resolve()
output.mkdir(parents=True,exist_ok=False)
root=Path(__file__).resolve().parents[1]/'Assets/PoolroomsProcedural'
bpy.ops.object.select_all(action='SELECT');bpy.ops.object.delete(use_global=False)
scene=bpy.context.scene
scene.render.engine='CYCLES';scene.cycles.device='CPU';scene.cycles.samples=64
scene.cycles.use_denoising=False
scene.render.resolution_x=1200;scene.render.resolution_y=800;scene.render.resolution_percentage=100
scene.world.color=(.35,.39,.45)
scene.world.use_nodes=True;scene.world.node_tree.nodes.get('Background').inputs['Color'].default_value=(.6,.7,.85,1);scene.world.node_tree.nodes.get('Background').inputs['Strength'].default_value=.4
scene.view_settings.view_transform='AgX';scene.view_settings.exposure=.1
cache={}
def material(theme,slot,tex):
 key=(theme,slot,tex)
 if key in cache:return cache[key]
 m=bpy.data.materials.new(str(key));m.use_nodes=True;n=m.node_tree.nodes;p=n.get('Principled BSDF');p.inputs['Roughness'].default_value=.55
 if theme=='Glass':
  p.inputs['Base Color'].default_value=(.93,.97,1,1);p.inputs['Transmission Weight'].default_value=.92;p.inputs['Roughness'].default_value=.08;p.inputs['IOR'].default_value=1.45
 elif theme=='SpecialMirror':p.inputs['Base Color'].default_value=(.8,.8,.8,1);p.inputs['Metallic'].default_value=1;p.inputs['Roughness'].default_value=.08
 elif tex:
  folder=root/'Resources'/('PoolroomsTerrainTextures' if theme=='CourtyardLawn' else 'PoolroomsBiomeTextures')
  for suffix in ['Albedo','Normal']:
   f=folder/(tex+'_'+suffix+'.png')
   if not f.exists():continue
   t=n.new('ShaderNodeTexImage');t.image=bpy.data.images.load(str(f),check_existing=True)
   if suffix=='Albedo':m.node_tree.links.new(t.outputs['Color'],p.inputs['Base Color'])
   else:
    t.image.colorspace_settings.name='Non-Color';normal=n.new('ShaderNodeNormalMap');normal.inputs['Strength'].default_value=.35;m.node_tree.links.new(t.outputs['Color'],normal.inputs['Color']);m.node_tree.links.new(normal.outputs['Normal'],p.inputs['Normal'])
  if slot==3:p.inputs['Metallic'].default_value=.8;p.inputs['Roughness'].default_value=.36
 else:p.inputs['Base Color'].default_value=(.8,.76,.67,1)
 cache[key]=m;return m
with open(atlas/'courtyard-mesh.json') as f:data=json.load(f)
for i,d in enumerate(data['meshes']):
 verts=[(v[0],v[2],v[1]) for v in d['p']];faces=[];slots=[]
 # Swapping Y/Z changes handedness: reverse each triangle once.
 for slot,indices in enumerate(d['indices']):
  for k in range(0,len(indices),3):faces.append((indices[k],indices[k+2],indices[k+1]));slots.append(slot)
 mesh=bpy.data.meshes.new(d['name']);mesh.from_pydata(verts,[],faces);mesh.update()
 obj=bpy.data.objects.new(d['name']+' '+str(i),mesh);scene.collection.objects.link(obj)
 for slot in range(6):mesh.materials.append(material(d['theme'],slot,d['tex'][slot]))
 for face,slot in zip(mesh.polygons,slots):face.material_index=slot
 uv=mesh.uv_layers.new(name='UVMap')
 for face in mesh.polygons:
  for loop in face.loop_indices:uv.data[loop].uv=d['uv'][mesh.loops[loop].vertex_index]
 print('Loaded',i,len(faces),flush=True)
# These are technical architecture renders. Runtime doors, imported furniture,
# HDRP exposure, ray tracing and post-processing are not represented or certified.
bpy.ops.object.light_add(type='SUN',location=(15,-15,60));sun=bpy.context.object;sun.rotation_euler=(math.radians(35),math.radians(-25),math.radians(-35));sun.data.energy=2;sun.data.angle=math.radians(4)
def view(location,target,name,lens=27):
 bpy.ops.object.camera_add(location=location);camera=bpy.context.object;camera.rotation_euler=(Vector(target)-camera.location).to_track_quat('-Z','Y').to_euler();camera.data.lens=lens;scene.camera=camera
 scene.render.filepath=str(output/(name+'.png'));bpy.ops.render.render(write_still=True)
view((29,26,2.2),(77,79,7),'patio',24)
view((110,-6,65),(52,56,3),'conjunto',35)
