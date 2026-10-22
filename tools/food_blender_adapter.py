"""Offline geometry-only Blender adapter invoked by import_food_sources.py."""
import bpy,bmesh,sys,json,zipfile,math,hashlib,io
from pathlib import Path
from mathutils import Vector
ROOT=Path(sys.argv[sys.argv.index('--')+1]); SOURCE=ROOT/'Sources/Food'; DEST=ROOT/'Assets/PoolroomsProcedural/Resources/FoodSources'
selection=json.loads((SOURCE/'selection.json').read_text()); z=zipfile.ZipFile(SOURCE/'kenney_food-kit.zip')
palette=bpy.data.images.load(str((lambda p:(p.write_bytes(z.read('Models/OBJ format/Textures/colormap.png')),p)[1])(Path('/tmp/food-source-palette.png'))))
pixels=list(palette.pixels);pw,ph=palette.size
(DEST/'Models').mkdir(parents=True,exist_ok=True)
def linear(v):return v/12.92 if v<=.04045 else ((v+.055)/1.055)**2.4

def export(obj,path,materials):
    mesh=obj.data;mesh.calc_loop_triangles();lines=['# CC0 Kenney Food Kit 2.0; adapted geometry, metric detail UVs.','mtllib '+path.stem+'.mtl']
    for v in mesh.vertices:lines.append('v '+' '.join(format(float(x),'.8g') for x in v.co))
    uv=[];norm=[];faces=[]
    seen=set()
    for t in mesh.loop_triangles:
        # Bevels at converging corners can contain sub-micron slivers. Remove
        # them in the exported precision domain, not merely in Blender memory.
        pts=[tuple(float(format(v,'.8g')) for v in mesh.vertices[i].co) for i in t.vertices]
        ab=[pts[1][a]-pts[0][a] for a in range(3)];ac=[pts[2][a]-pts[0][a] for a in range(3)]
        if sum((ab[(a+1)%3]*ac[(a+2)%3]-ab[(a+2)%3]*ac[(a+1)%3])**2 for a in range(3))<=1e-20:continue
        ids=tuple(tuple(p) for p in pts);key=min(ids[i:]+ids[:i] for i in range(3))
        if key in seen:continue
        seen.add(key)
        normal=t.normal;axis=max(range(3),key=lambda a:abs(normal[a]));axes=[a for a in range(3) if a!=axis]
        refs=[]
        for li in t.loops:
            loop=mesh.loops[li];p=mesh.vertices[loop.vertex_index].co
            uv.append((p[axes[0]]*7,p[axes[1]]*7));norm.append(mesh.corner_normals[li].vector[:]);refs.append((loop.vertex_index+1,len(uv),len(norm)))
        faces.append((t.material_index,refs))
    for p in uv:lines.append('vt '+' '.join(format(v,'.8g') for v in p))
    for p in norm:lines.append('vn '+' '.join(format(v,'.8g') for v in p))
    previous=-1
    for mi,refs in faces:
        if mi!=previous:lines.append('usemtl '+materials[mi]['name']);previous=mi
        lines.append('f '+' '.join('/'.join(map(str,r)) for r in refs))
    path.write_text('\n'.join(lines)+'\n')
    path.with_suffix('.mtl').write_text('\n'.join('newmtl '+m['name']+'\nKd 1 1 1\n' for m in materials))
    return len(faces)
for row in selection:
    for ob in list(bpy.data.objects):bpy.data.objects.remove(ob,do_unlink=True)
    raw=z.read('Models/OBJ format/'+row['model']+'.obj');positions=[];tex=[];faces=[];facecolors=[];seen_faces=set();removed_faces=0
    for line in raw.decode().splitlines():
        p=line.split()
        if not p:continue
        if p[0]=='v':positions.append(tuple(map(float,p[1:4])))
        elif p[0]=='vt':tex.append(tuple(map(float,p[1:3])))
        elif p[0]=='f':
            refs=[tuple(map(int,v.split('/'))) for v in p[1:]]
            ids=tuple(v[0]-1 for v in refs);key=min(ids[i:]+ids[:i] for i in range(len(ids)))
            if key in seen_faces:removed_faces+=1;continue
            seen_faces.add(key);faces.append(list(ids));u,v=tex[refs[0][1]-1];x=min(pw-1,max(0,int(u*pw)));y=min(ph-1,max(0,int(v*ph)));facecolors.append(tuple(round(pixels[(y*pw+x)*4+a]*255) for a in range(3)))
    mesh=bpy.data.meshes.new(row['resourceId']);mesh.from_pydata(positions,[],faces);mesh.update();obj=bpy.data.objects.new(row['resourceId'],mesh);bpy.context.collection.objects.link(obj);bpy.context.view_layer.objects.active=obj;obj.select_set(True)
    # Source units are already metric. Centre horizontally and rest at y=0.
    mins=[min(p[a] for p in positions) for a in range(3)];maxs=[max(p[a] for p in positions) for a in range(3)];longest=max(b-a for a,b in zip(mins,maxs));scale=min(.62/max(longest,1e-5),1.0)
    for vert in mesh.vertices:vert.co=Vector(((vert.co.x-(mins[0]+maxs[0])*.5)*scale,(vert.co.y-mins[1])*scale,(vert.co.z-(mins[2]+maxs[2])*.5)*scale))
    colors=sorted(set(facecolors));materials=[]
    for c in colors:
        name='Food_'+row['family']+'_'+'_'.join(map(str,c));m=bpy.data.materials.get(name) or bpy.data.materials.new(name);mesh.materials.append(m)
        materials.append({'name':name,'colour':[linear(v/255) for v in c]+[1],'metallic':0,'roughness':.55,'normalScale':.35,'albedo':'FoodSources/Textures/'+row['family']+'_albedo','normal':'FoodSources/Textures/'+row['family']+'_normal','mask':'FoodSources/Textures/'+row['family']+'_mask'})
    for p,c in zip(mesh.polygons,facecolors):p.material_index=colors.index(c);p.use_smooth=True
    # OBJ seam vertices need welding before bevel/decimation can operate across
    # their authored coplanar islands. Preserve face material assignments.
    bm=bmesh.new();bm.from_mesh(mesh);bmesh.ops.remove_doubles(bm,verts=list(bm.verts),dist=.0000005);bm.to_mesh(mesh);bm.free();mesh.update()
    # Small physical bevels preserve recognizable silhouettes, not wholesale subdivision.
    bevel=obj.modifiers.new('Metric edge rounding','BEVEL');bevel.width=.0015;bevel.segments=2;bevel.limit_method='ANGLE';bevel.angle_limit=math.radians(40)
    bpy.ops.object.modifier_apply(modifier=bevel.name);obj.data.set_sharp_from_angle(angle=math.radians(48))
    mins=[min(v.co[a] for v in obj.data.vertices) for a in range(3)];maxs=[max(v.co[a] for v in obj.data.vertices) for a in range(3)]
    triangles=export(obj,DEST/'Models'/(row['resourceId']+'.obj'),materials)
    dec=obj.modifiers.new('Bounded distant food','DECIMATE');dec.ratio=.42;dec.use_collapse_triangulate=True
    bpy.ops.object.modifier_apply(modifier=dec.name);obj.data.set_sharp_from_angle(angle=math.radians(48))
    low=export(obj,DEST/'Models'/(row['resourceId']+'_LOD1.obj'),materials)
    report=dict(row,id=row['resourceId'],resource='FoodSources/Models/'+row['resourceId'],minimum=mins,maximum=maxs,triangles=triangles,materials=materials,lods=[{'resource':'FoodSources/Models/'+row['resourceId']+'_LOD1','triangles':low}],removedDuplicateFaces=removed_faces,sourceSha256=hashlib.sha256(raw).hexdigest(),license='CC0-1.0',author='Kenney',sourceUrl='https://kenney.nl/assets/food-kit',nativeUnityValidated=False)
    (DEST/(row['resourceId']+'.json')).write_text(json.dumps(report,ensure_ascii=False,indent=2)+'\n')
    print(row['model'],triangles,low,flush=True)
