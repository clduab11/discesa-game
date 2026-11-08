# Discesa ship - original low-poly authored mesh (Blender)
# Run headless:
#   blender --background --factory-startup --python make_ship_graybox_v1.py
# Output (next to this script): ship_graybox_v1.glb (glTF Binary, Y-up)
# Regenerate any time; the script is the source of truth, the .blend is optional.

import bpy
import math
import os

OUT_DIR = os.path.dirname(os.path.abspath(__file__))
GLB_PATH = os.path.join(OUT_DIR, "..", "..", "Models", "ship_graybox_v1.glb")
BLEND_PATH = os.path.join(OUT_DIR, "ship_graybox_v1.blend")

# --- clean scene (keep low - no external assets) ---
bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)
for blk in (bpy.data.meshes, bpy.data.curves):
    pass  # orphan cleanup via factory startup is enough

# --- helpers ---
def make_principled(name, color, metallic=0.0, roughness=0.5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return m

def add_mesh(name, verts, faces, material):
    me = bpy.data.meshes.new(name + "Mesh")
    me.from_pydata(verts, [], faces)
    me.update()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    if material:
        me.materials.append(material)
    # smooth low-poly shading (flat-ish but not faceted noise)
    for p in me.polygons:
        p.use_smooth = True
    return ob

def prism(name, x, y, z, w, h, d, material, taper=1.0):
    # axis-aligned box centred at (x,y,z), size (w,h,d). Blender: X right, Y forward, Z up.
    hw, hh, hd = w/2, h/2, d/2
    verts = [
        (x-hw, y-hd, z-hh), (x+hw, y-hd, z-hh), (x+hw, y+hd, z-hh), (x-hw, y+hd, z-hh),
        (x-hw*taper, y-hd, z+hh), (x+hw*taper, y-hd, z+hh), (x+hw*taper, y+hd, z+hh), (x-hw*taper, y+hd, z+hh),
    ]
    faces = [
        (0,3,2,1), (4,5,6,7), (0,1,5,4), (1,2,6,5), (2,3,7,6), (3,0,4,7)
    ]
    return add_mesh(name, verts, faces, material)

def cylinder(name, x, y, z, r, h, material, verts=12, axis='Z'):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=(x, y, z))
    ob = bpy.context.active_object
    ob.name = name
    if axis == 'X':
        ob.rotation_euler = (0, math.radians(90), 0)
    elif axis == 'Y':
        ob.rotation_euler = (math.radians(90), 0, 0)
    if material:
        ob.data.materials.append(material)
    return ob

# --- materials (vertex-colour-free; export as glTF metallic-roughness) ---
hullMat = make_principled("Hull", (0.95, 0.75, 0.25), metallic=0.35, roughness=0.45)
darkMat = make_principled("DarkTrim", (0.16, 0.17, 0.22), metallic=0.5, roughness=0.5)
canopyMat = make_principled("Canopy", (0.10, 0.55, 0.95), metallic=0.1, roughness=0.15)
glowMat = make_principled("Glow", (0.5, 1.0, 0.4), metallic=0.0, roughness=0.4)

# --- ship: fits ~0.7 wide x 0.45 tall x 1.3 long (Unity envelope), nose +Y in Blender (+Z forward) ---
# hull body
prism("Hull", 0, 0, 0.0, 0.62, 0.34, 1.10, hullMat, taper=0.85)
# nose wedge (tapered forward)
prism("Nose", 0, 0.62, 0.02, 0.34, 0.30, 0.28, hullMat, taper=0.55)
# canopy dome (flattened UV sphere)
bpy.ops.mesh.primitive_uv_sphere_add(segments=16, ring_count=8, radius=0.22, location=(0, 0.12, 0.22))
canopy = bpy.context.active_object
canopy.name = "Canopy"
canopy.scale = (1.0, 1.25, 0.72)
if canopyMat:
    canopy.data.materials.append(canopyMat)
# wing stubs (left/right)
prism("WingL", -0.34, -0.15, -0.02, 0.24, 0.26, 0.06, darkMat, taper=0.6)
prism("WingR", 0.34, -0.15, -0.02, 0.24, 0.26, 0.06, darkMat, taper=0.6)
# engine block + glow strip
prism("Engine", 0, -0.58, 0.0, 0.30, 0.22, 0.14, darkMat)
cylinder("GlowStrip", 0, -0.70, 0.0, 0.07, 0.26, glowMat, verts=10, axis='X')
# twin gun barrels (forward, +Y)
cylinder("GunL", -0.16, 0.75, 0.0, 0.03, 0.30, darkMat, verts=8, axis='Y')
cylinder("GunR", 0.16, 0.75, 0.0, 0.03, 0.30, darkMat, verts=8, axis='Y')

# --- origin at ship centre, Y-up export will rotate Z-up -> Y-up (glTF) ---
# ensure all objects selected for export
bpy.ops.object.select_all(action='DESELECT')
for ob in bpy.data.objects:
    if ob.type == 'MESH':
        ob.select_set(True)
bpy.context.view_layer.objects.active = bpy.data.objects.get("Hull")

# --- save .blend (source) ---
os.makedirs(OUT_DIR, exist_ok=True)
bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)

# --- export GLB (Y-up, +Z forward in Blender -> glTF Y-up; Unity import keeps forward -Z via import rotation) ---
os.makedirs(os.path.dirname(GLB_PATH), exist_ok=True)
bpy.ops.export_scene.gltf(
    filepath=GLB_PATH,
    export_format='GLB',
    export_yup=True,
    export_apply=True,
    export_texcoords=True,
    export_normals=True,
    export_materials='EXPORT',
    export_cameras=False,
    export_lights=False,
)
print(f"[Blender] Wrote {os.path.abspath(GLB_PATH)}")
