"""Render actual exported Core geometry for technical inspection, NOT gameplay.

blender -b --python tools/render_forest39_preview.py -- <atlas dir> <new image dir>
Native HDRP, ray tracing, source furniture and interactive behavior are untested.
"""
import json
import math
import sys
from pathlib import Path
import bpy
from mathutils import Vector

ROOT = Path(__file__).resolve().parents[1]
TEXTURES = ROOT / 'Assets/PoolroomsProcedural/Resources/Forest39/Textures'


def settings():
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)
    scene = bpy.context.scene
    scene.render.engine = 'CYCLES'
    scene.cycles.device = 'CPU'
    scene.cycles.samples = 24
    scene.cycles.use_denoising = False
    scene.render.resolution_x = 1100
    scene.render.resolution_y = 750
    scene.render.resolution_percentage = 100
    scene.world.use_nodes = True
    background = scene.world.node_tree.nodes.get('Background')
    background.inputs['Color'].default_value = (.4, .52, .68, 1)
    background.inputs['Strength'].default_value = .4
    scene.view_settings.view_transform = 'AgX'
    scene.view_settings.exposure = .3
    return scene


def texture(nodes, filename, non_colour=False):
    path = TEXTURES / filename
    if not path.exists():
        return None
    node = nodes.new('ShaderNodeTexImage')
    node.image = bpy.data.images.load(str(path), check_existing=True)
    if non_colour:
        node.image.colorspace_settings.name = 'Non-Color'
    return node


def material(theme, surface, cache):
    key = (theme, surface)
    if key in cache:
        return cache[key]
    result = bpy.data.materials.new(str(key))
    result.use_nodes = True
    nodes = result.node_tree.nodes
    links = result.node_tree.links
    shader = nodes.get('Principled BSDF')
    shader.inputs['Roughness'].default_value = .8
    if theme == 'water':
        shader.inputs['Base Color'].default_value = (.94, .98, 1, 1)
        shader.inputs['Transmission Weight'].default_value = 1
        shader.inputs['Roughness'].default_value = .025
        shader.inputs['IOR'].default_value = 1.333
        cache[key] = result
        return result
    role = 'Canopy' if theme == 'Forest39CanopyLeaves' else 'Leaf' if theme == 'Forest39Leaves' else 'Bark' if theme == 'Forest39Bark' else 'Stone' if theme == 'Forest39Stone' else 'Ground'
    albedo = texture(nodes, role + '_Albedo.png')
    if albedo:
        if role == 'Canopy':
            links.new(albedo.outputs['Alpha'], shader.inputs['Alpha'])
        if role == 'Ground' and surface == 1:
            multiply = nodes.new('ShaderNodeMixRGB')
            multiply.blend_type = 'MULTIPLY'
            multiply.inputs[0].default_value = 1
            multiply.inputs[2].default_value = (.7, .59, .42, 1)
            links.new(albedo.outputs['Color'], multiply.inputs[1])
            links.new(multiply.outputs[0], shader.inputs['Base Color'])
        else:
            links.new(albedo.outputs['Color'], shader.inputs['Base Color'])
    normal = texture(nodes, role + '_Normal.png', True)
    if normal:
        conversion = nodes.new('ShaderNodeNormalMap')
        conversion.inputs['Strength'].default_value = .7
        links.new(normal.outputs['Color'], conversion.inputs['Color'])
        links.new(conversion.outputs['Normal'], shader.inputs['Normal'])
    mask = texture(nodes, role + '_Mask.png', True)
    if mask:
        invert = nodes.new('ShaderNodeMath')
        invert.operation = 'SUBTRACT'
        invert.inputs[0].default_value = 1
        links.new(mask.outputs['Alpha'], invert.inputs[1])
        links.new(invert.outputs[0], shader.inputs['Roughness'])
    cache[key] = result
    return result


def geometry(record):
    cache = {}
    for part in record['meshes']:
        positions = [(p[0], p[2], p[1]) for p in part['p']]
        faces = []
        slots = []
        for surface, indices in enumerate(part['indices']):
            for index in range(0, len(indices), 3):
                faces.append((indices[index], indices[index + 2], indices[index + 1]))
                slots.append(surface)
        mesh = bpy.data.meshes.new(part['name'])
        mesh.from_pydata(positions, [], faces)
        mesh.update()
        for polygon in mesh.polygons:
            polygon.use_smooth = True
        mesh.normals_split_custom_set_from_vertices([(n[0],n[2],n[1]) for n in part['n']])
        obj = bpy.data.objects.new(part['name'], mesh)
        bpy.context.scene.collection.objects.link(obj)
        for slot in range(6):
            mesh.materials.append(material('water' if part['name'] == 'water' else part['theme'], slot, cache))
        uv = mesh.uv_layers.new(name='MetricUV')
        for face, slot in zip(mesh.polygons, slots):
            face.material_index = slot
            for loop in face.loop_indices:
                uv.data[loop].uv = part['uv'][mesh.loops[loop].vertex_index]


def main():
    args = sys.argv[sys.argv.index('--') + 1:]
    if len(args) != 2:
        raise SystemExit(__doc__)
    source, dest = map(Path, args)
    dest.mkdir(parents=True, exist_ok=False)
    scene = settings()
    record = json.loads((source / 'forest39-mesh.json').read_text())
    assert record['nativeUnityExecuted'] is False
    geometry(record)
    bpy.ops.object.light_add(type='SUN', location=(20, -30, 12))
    sun = bpy.context.object
    sun.rotation_euler = (math.radians(76), math.radians(-15), math.radians(35))
    sun.data.energy = 1.9
    sun.data.angle = math.radians(3)
    sun.data.color = (1, .83, .66)
    for name, point, target, lens in [
        ('forest-overview', (27, -34, 24), (0, 0, 5), 34),
        ('forest-path', (0, -10, 1.8), (3, 9, 3), 25),
    ]:
        bpy.ops.object.camera_add(location=point)
        camera = bpy.context.object
        camera.rotation_euler = (Vector(target) - camera.location).to_track_quat('-Z', 'Y').to_euler()
        camera.data.lens = lens
        scene.camera = camera
        scene.render.filepath = str(dest / (name + '.png'))
        bpy.ops.render.render(write_still=True)
    (dest / 'LIMITS.txt').write_text('Technical Core geometry preview. Not a Unity screenshot or native ray-tracing/FPS verification. Source furniture omitted.\n')


if __name__ == '__main__':
    main()
