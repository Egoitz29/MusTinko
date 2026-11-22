# Carta 3D de Mus TINKO hecha con Blender: esquinas redondeadas, canto biselado y grosor de cartulina.
# Tres partes: anverso (el dibujo), reverso y canto.
#
# Se exporta como datos de malla (JSON) ya en el sistema de Unity, para no tener lios de ejes:
#   - la carta mide 1 de alto, ancho = 486/762 (como los dibujos de la baraja)
#   - el anverso mira hacia -Z (como un Quad de Unity) y el reverso hacia +Z
#   - UV del anverso y del reverso de 0 a 1, sin espejo, mirando cada cara de frente
#
# Uso (pip install bpy):
#   python3 carta3d.py exportar <salida.json>
#   python3 carta3d.py render <atlas_png> <reverso_png> <salida.png>

import json
import math
import sys

import bpy  # primero bpy: es quien deja disponible bmesh
import bmesh
from mathutils import Vector

ALTO = 1.0
ANCHO = 486.0 / 762.0
GROSOR = 0.008
RADIO = 0.047           # algo mas que el redondeo de los dibujos (34 px de 762)
SEGMENTOS_ESQUINA = 10
BISEL = 0.0028          # canto redondeado
SEGMENTOS_BISEL = 3


def contorno():
    pts = []
    centros = [(ANCHO / 2 - RADIO, ALTO / 2 - RADIO, 0), (-ANCHO / 2 + RADIO, ALTO / 2 - RADIO, 90),
               (-ANCHO / 2 + RADIO, -ALTO / 2 + RADIO, 180), (ANCHO / 2 - RADIO, -ALTO / 2 + RADIO, 270)]
    for cx, cy, a0 in centros:
        for i in range(SEGMENTOS_ESQUINA + 1):
            a = math.radians(a0 + 90 * i / SEGMENTOS_ESQUINA)
            pts.append((cx + RADIO * math.cos(a), cy + RADIO * math.sin(a)))
    return pts


def construir():
    bm = bmesh.new()
    abajo = [bm.verts.new((x, y, -GROSOR / 2)) for x, y in contorno()]
    cara = bm.faces.new(abajo)
    res = bmesh.ops.extrude_face_region(bm, geom=[cara])
    arriba = [e for e in res["geom"] if isinstance(e, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, verts=arriba, vec=(0, 0, GROSOR))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])

    # Bisel en los dos bordes del canto (cartulina con el borde suave).
    bordes = [e for e in bm.edges if all(abs(abs(v.co.z) - GROSOR / 2) < 1e-6 for v in e.verts)
              and abs(e.verts[0].co.z - e.verts[1].co.z) < 1e-6]
    bmesh.ops.bevel(bm, geom=bordes, offset=BISEL, segments=SEGMENTOS_BISEL, profile=0.5, affect="EDGES")
    bmesh.ops.triangulate(bm, faces=bm.faces[:], quad_method="BEAUTY", ngon_method="BEAUTY")
    bm.normal_update()
    return bm


def parte(cara):
    # 0 anverso (Blender +Z), 1 reverso (Blender -Z), 2 canto
    if cara.normal.z > 0.999:
        return 0
    if cara.normal.z < -0.999:
        return 1
    return 2


def exportar(ruta):
    bm = construir()
    vertices, normales, uvs = [], [], []
    tris = [[], [], []]
    indice = {}

    # Normal suave en el canto (media de las caras del canto que tocan el vertice).
    suave = {}
    for v in bm.verts:
        n = Vector()
        for f in v.link_faces:
            if parte(f) == 2:
                n += f.normal
        suave[v.index] = n.normalized() if n.length > 0 else Vector((0, 0, 1))

    for f in bm.faces:
        p = parte(f)
        esquinas = []
        for v in f.verts:
            x, y, z = v.co
            n = f.normal if p != 2 else suave[v.index]
            if p == 0:
                uv = ((x + ANCHO / 2) / ANCHO, (y + ALTO / 2) / ALTO)
            elif p == 1:
                uv = ((ANCHO / 2 - x) / ANCHO, (y + ALTO / 2) / ALTO)
            else:
                uv = (0.5, 0.5)
            # De Blender (mano derecha) a Unity (mano izquierda): z cambia de signo.
            clave = (round(x, 6), round(y, 6), round(-z, 6), round(n.x, 4), round(n.y, 4), round(-n.z, 4), round(uv[0], 5), round(uv[1], 5), p)
            if clave not in indice:
                indice[clave] = len(vertices) // 3
                vertices += [x, y, -z]
                normales += [n.x, n.y, -n.z]
                uvs += [uv[0], uv[1]]
            esquinas.append(indice[clave])
        # Al cambiar de mano hay que dar la vuelta al orden de los triangulos.
        tris[p] += [esquinas[0], esquinas[2], esquinas[1]]

    datos = {"v": [round(c, 6) for c in vertices], "n": [round(c, 4) for c in normales], "uv": [round(c, 5) for c in uvs],
             "anverso": tris[0], "reverso": tris[1], "canto": tris[2]}
    with open(ruta, "w") as f:
        json.dump(datos, f, separators=(",", ":"))
    print("Exportado:", ruta, len(vertices) // 3, "vertices,", sum(len(t) for t in tris) // 3, "triangulos")


def material(nombre, imagen=None, color=(1, 1, 1)):
    m = bpy.data.materials.new(nombre)
    m.use_nodes = True
    bsdf = m.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.42
    bsdf.inputs["Base Color"].default_value = (*color, 1)
    if imagen:
        tex = m.node_tree.nodes.new("ShaderNodeTexImage")
        tex.image = bpy.data.images.load(imagen)
        m.node_tree.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    return m


def render(atlas, reverso, salida):
    import os
    import tempfile
    from PIL import Image

    bpy.ops.wm.read_factory_settings(use_empty=True)
    tmp = tempfile.mkdtemp()
    im = Image.open(atlas)
    caras = []
    for i in (0, 9, 21, 33, 14):
        col, fila = i % 10, i // 10
        ruta = os.path.join(tmp, f"c{i}.png")
        im.crop((col * 486, fila * 762, col * 486 + 486, fila * 762 + 762)).save(ruta)
        caras.append(ruta)

    def carta(nombre, frente):
        bm = construir()
        me = bpy.data.meshes.new(nombre)
        bm.to_mesh(me)
        uv = me.uv_layers.new()
        for poly in me.polygons:
            p = 0 if poly.normal.z > 0.999 else 1 if poly.normal.z < -0.999 else 2
            poly.material_index = p
            poly.use_smooth = p == 2
            for li in poly.loop_indices:
                co = me.vertices[me.loops[li].vertex_index].co
                uv.data[li].uv = ((co.x + ANCHO / 2) / ANCHO if p == 0 else (ANCHO / 2 - co.x) / ANCHO, (co.y + ALTO / 2) / ALTO)
        o = bpy.data.objects.new(nombre, me)
        bpy.context.collection.objects.link(o)
        me.materials.append(material("A" + nombre, frente))
        me.materials.append(material("R" + nombre, reverso))
        me.materials.append(material("C" + nombre, None, (0.93, 0.9, 0.82)))
        return o

    # Cuatro cartas boca arriba sobre la mesa y un abanico de reversos.
    for i, f in enumerate(caras[:4]):
        o = carta(f"Mesa{i}", f)
        o.location = (-1.05 + i * 0.7, -0.45, 0.004)
        o.rotation_euler = (0, 0, math.radians((i - 1.5) * 3))
    for i in range(4):
        o = carta(f"Mano{i}", caras[4])
        o.location = (0.2 + 0.05 * i, 0.9, 0.35)
        o.rotation_euler = (math.radians(-70), 0, math.radians((1.5 - i) * 11))
    bpy.ops.mesh.primitive_plane_add(size=8, location=(0, 0, -0.001))
    mesa = bpy.context.active_object
    mesa.data.materials.append(material("Mesa", None, (0.33, 0.18, 0.09)))

    escena = bpy.context.scene
    cam = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    escena.collection.objects.link(cam)
    cam.location = (0.1, -3.4, 2.6)
    cam.rotation_euler = (math.radians(52), 0, 0)
    escena.camera = cam
    luz = bpy.data.objects.new("Luz", bpy.data.lights.new("Luz", "AREA"))
    luz.data.energy = 300
    luz.data.size = 3
    luz.location = (1.5, -2, 3.5)
    escena.collection.objects.link(luz)
    mundo = bpy.data.worlds.new("Mundo")
    mundo.use_nodes = True
    mundo.node_tree.nodes["Background"].inputs[0].default_value = (0.3, 0.24, 0.18, 1)
    escena.world = mundo
    escena.render.engine = "CYCLES"
    escena.cycles.samples = 32
    escena.render.resolution_x = 1200
    escena.render.resolution_y = 760
    escena.render.filepath = salida
    bpy.ops.render.render(write_still=True)
    print("Render:", salida)


if __name__ == "__main__":
    a = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    if a and a[0] == "exportar":
        exportar(a[1])
    elif a and a[0] == "render":
        render(a[1], a[2], a[3])
