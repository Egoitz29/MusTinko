# Genera los complementos de los personajes de Mus TINKO (bigote, barba, gafas, sombreros...).
# Las medidas van respecto a los controles de tus personajes (CONTROL_CABEZA y CONTROL_RAIZ),
# que son iguales en los cuatro modelos, asi que sirven para todos.
#
# Uso (con Blender instalado como modulo de Python: pip install bpy):
#   python3 complementos.py exportar <ruta_salida.fbx>
#   python3 complementos.py render <personaje_fbx_dir> <ruta_salida.png>
# O desde Blender: Scripting -> abrir este archivo -> Run (exporta junto al .blend).

import math
import sys

import bpy
from mathutils import Euler, Vector

# Centro de la cabeza respecto a CONTROL_RAIZ (igual en los cuatro modelos).
CABEZA = Vector((0.0, -0.03, 1.87))

COLORES = {
    "Pelo": (0.30, 0.18, 0.10),
    "Gafas": (0.08, 0.07, 0.07),
    "Puro": (0.42, 0.24, 0.12),
    "Brasa": (1.0, 0.45, 0.1),
    "Ceniza": (0.75, 0.73, 0.7),
    "Pajarita": (0.75, 0.1, 0.14),
    "Txapela": (0.1, 0.1, 0.12),
    "Sombrero": (0.07, 0.07, 0.08),
    "Cinta": (0.75, 0.1, 0.14),
    "Bufanda": (0.85, 0.2, 0.2),
    "Raya": (0.97, 0.94, 0.86),
    "Pendiente": (0.95, 0.76, 0.28),
}

# Que complementos tapan el gorro o el pelo de arriba.
TAPAN_CABEZA = {"Txapela", "SombreroCopa"}


def material(nombre):
    m = bpy.data.materials.get("ACC_" + nombre)
    if m is None:
        m = bpy.data.materials.new("ACC_" + nombre)
        m.use_nodes = True
        bsdf = m.node_tree.nodes.get("Principled BSDF")
        c = COLORES.get(nombre, (0.8, 0.8, 0.8))
        bsdf.inputs["Base Color"].default_value = (c[0], c[1], c[2], 1)
        bsdf.inputs["Roughness"].default_value = 0.55
        if nombre == "Brasa":
            bsdf.inputs["Emission Color"].default_value = (1, 0.4, 0.05, 1)
            bsdf.inputs["Emission Strength"].default_value = 2.0
        m.diffuse_color = (c[0], c[1], c[2], 1)
    return m


def suavizar(o):
    for p in o.data.polygons:
        p.use_smooth = True


def esfera(nombre, centro, escala, mat, segmentos=24):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segmentos, ring_count=segmentos // 2, location=centro)
    o = bpy.context.active_object
    o.name = nombre
    o.scale = escala
    o.data.materials.append(material(mat))
    suavizar(o)
    return o


def cilindro(nombre, centro, radio, alto, mat, rot=(0, 0, 0), vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radio, depth=alto, location=centro, rotation=rot)
    o = bpy.context.active_object
    o.name = nombre
    o.data.materials.append(material(mat))
    suavizar(o)
    return o


def toroide(nombre, centro, mayor, menor, mat, rot=(0, 0, 0), escala=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=mayor, minor_radius=menor, major_segments=40, minor_segments=14,
                                     location=centro, rotation=rot)
    o = bpy.context.active_object
    o.name = nombre
    o.scale = escala
    o.data.materials.append(material(mat))
    suavizar(o)
    return o


def cono(nombre, centro, r1, r2, alto, mat, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cone_add(vertices=24, radius1=r1, radius2=r2, depth=alto, location=centro, rotation=rot)
    o = bpy.context.active_object
    o.name = nombre
    o.data.materials.append(material(mat))
    suavizar(o)
    return o


def tubo_curvo(nombre, puntos, radios, mat):
    # Un tubo que sigue una curva y se va estrechando (para bigotes).
    curva = bpy.data.curves.new(nombre, "CURVE")
    curva.dimensions = "3D"
    curva.bevel_depth = 1.0
    curva.bevel_resolution = 6
    curva.resolution_u = 12
    spline = curva.splines.new("BEZIER")
    spline.bezier_points.add(len(puntos) - 1)

    for bp, p, r in zip(spline.bezier_points, puntos, radios):
        bp.co = p
        bp.handle_left_type = bp.handle_right_type = "AUTO"
        bp.radius = r

    o = bpy.data.objects.new(nombre, curva)
    bpy.context.collection.objects.link(o)
    o.data.materials.append(material(mat))
    bpy.context.view_layer.objects.active = o
    o.select_set(True)
    bpy.ops.object.convert(target="MESH")
    o = bpy.context.active_object
    suavizar(o)
    return o


def C(x, y, z):
    # Posicion relativa al centro de la cabeza.
    return CABEZA + Vector((x, y, z))


# ---------- Complementos (cada uno devuelve [(objeto, control_padre)]) ----------

def bigote():
    piezas = []
    for lado in (1, -1):
        pts = [C(0.0, -0.66, -0.2), C(0.14 * lado, -0.66, -0.25), C(0.28 * lado, -0.6, -0.22), C(0.36 * lado, -0.53, -0.1)]
        piezas.append((tubo_curvo("ACC_Bigote_Pelo_Bigote_" + ("D" if lado > 0 else "I"), pts, [0.075, 0.07, 0.05, 0.022], "Pelo"),
                       "CONTROL_CABEZA"))
    return piezas


def barba():
    b = esfera("ACC_Barba_Pelo_Barba", C(0, -0.25, -0.6), (0.4, 0.27, 0.19), "Pelo")
    perilla = esfera("ACC_Barba_Pelo_Perilla", C(0, -0.4, -0.7), (0.17, 0.13, 0.14), "Pelo")
    p1 = esfera("ACC_Barba_Pelo_Patilla_D", C(0.47, -0.14, -0.3), (0.08, 0.13, 0.22), "Pelo")
    p2 = esfera("ACC_Barba_Pelo_Patilla_I", C(-0.47, -0.14, -0.3), (0.08, 0.13, 0.22), "Pelo")
    return [(b, "CONTROL_CABEZA"), (perilla, "CONTROL_CABEZA"), (p1, "CONTROL_CABEZA"), (p2, "CONTROL_CABEZA")]


def gafas():
    piezas = []
    for lado in (1, -1):
        s = "D" if lado > 0 else "I"
        piezas.append((toroide("ACC_Gafas_Gafas_Aro_" + s, C(0.255 * lado, -0.68, 0.06), 0.25, 0.028, "Gafas",
                               rot=(math.pi / 2, 0, 0)), "CONTROL_CABEZA"))
        patilla = cilindro("ACC_Gafas_Gafas_Patilla_" + s, C(0.5 * lado, -0.36, 0.1), 0.02, 0.62, "Gafas",
                           rot=(math.pi / 2, 0, -0.18 * lado))
        piezas.append((patilla, "CONTROL_CABEZA"))
    puente = toroide("ACC_Gafas_Gafas_Puente", C(0, -0.7, 0.12), 0.06, 0.022, "Gafas", rot=(math.pi / 2, 0, 0), escala=(1, 1, 0.6))
    piezas.append((puente, "CONTROL_CABEZA"))
    return piezas


def puro():
    d = Vector((0.55, -0.75, -0.3)).normalized()
    inicio = C(0.13, -0.5, -0.33)
    largo = 0.36
    rot = d.to_track_quat("Z", "Y").to_euler()
    cuerpo = cilindro("ACC_Puro_Puro", inicio + d * (largo / 2), 0.04, largo, "Puro", rot=rot, vertices=20)
    brasa = cilindro("ACC_Puro_Brasa", inicio + d * (largo + 0.01), 0.041, 0.03, "Brasa", rot=rot, vertices=20)
    ceniza = cilindro("ACC_Puro_Ceniza", inicio + d * (largo + 0.035), 0.036, 0.03, "Ceniza", rot=rot, vertices=20)
    return [(cuerpo, "CONTROL_CABEZA"), (brasa, "CONTROL_CABEZA"), (ceniza, "CONTROL_CABEZA")]


def txapela():
    rot = (0.0, math.radians(-10), 0.0)
    disco = esfera("ACC_Txapela_Txapela", C(0.04, 0.0, 0.43), (0.66, 0.6, 0.15), "Txapela", segmentos=32)
    disco.rotation_euler = rot
    borde = toroide("ACC_Txapela_Txapela_Borde", C(0.03, 0.0, 0.36), 0.5, 0.05, "Txapela", rot=rot, escala=(1, 0.92, 1))
    rabito = cilindro("ACC_Txapela_Txapela_Rabito", C(0.06, 0.0, 0.6), 0.025, 0.12, "Txapela", rot=rot, vertices=12)
    return [(disco, "CONTROL_CABEZA"), (borde, "CONTROL_CABEZA"), (rabito, "CONTROL_CABEZA")]


def sombrero_copa():
    ala = cilindro("ACC_SombreroCopa_Sombrero_Ala", C(0, 0.02, 0.44), 0.64, 0.05, "Sombrero", vertices=48)
    copa = cilindro("ACC_SombreroCopa_Sombrero_Copa", C(0, 0.02, 0.73), 0.38, 0.56, "Sombrero", vertices=48)
    tapa = esfera("ACC_SombreroCopa_Sombrero_Tapa", C(0, 0.02, 1.01), (0.38, 0.38, 0.04), "Sombrero", segmentos=32)
    cinta = cilindro("ACC_SombreroCopa_Cinta", C(0, 0.02, 0.53), 0.385, 0.12, "Cinta", vertices=48)
    return [(ala, "CONTROL_CABEZA"), (copa, "CONTROL_CABEZA"), (tapa, "CONTROL_CABEZA"), (cinta, "CONTROL_CABEZA")]


def pajarita():
    centro = C(0, -0.4, -0.5)
    piezas = []
    for lado in (1, -1):
        s = "D" if lado > 0 else "I"
        o = cono("ACC_Pajarita_Pajarita_" + s, centro + Vector((0.11 * lado, 0, 0)), 0.11, 0.02, 0.2, "Pajarita",
                 rot=(0, math.pi / 2 * -lado, 0))
        o.scale = (1, 0.55, 1)
        piezas.append((o, "CONTROL_RAIZ"))
    nudo = esfera("ACC_Pajarita_Pajarita_Nudo", centro + Vector((0, -0.01, 0)), (0.05, 0.04, 0.06), "Pajarita")
    piezas.append((nudo, "CONTROL_RAIZ"))
    return piezas


def bufanda():
    anillo = toroide("ACC_Bufanda_Bufanda", C(0, -0.04, -0.6), 0.4, 0.11, "Bufanda", escala=(1, 0.9, 1))
    for i, z in enumerate((-0.6,)):
        pass
    raya = toroide("ACC_Bufanda_Raya", C(0, -0.04, -0.6), 0.4, 0.112, "Raya", escala=(1, 0.9, 0.25))
    cola = esfera("ACC_Bufanda_Bufanda_Cola", C(0.22, -0.42, -0.85), (0.11, 0.06, 0.26), "Bufanda")
    cola.rotation_euler = (0.0, math.radians(-12), 0.0)
    return [(anillo, "CONTROL_RAIZ"), (raya, "CONTROL_RAIZ"), (cola, "CONTROL_RAIZ")]


def pendiente():
    aro = toroide("ACC_Pendiente_Pendiente", C(0.66, -0.02, -0.3), 0.06, 0.015, "Pendiente", rot=(0, math.pi / 2, 0))
    return [(aro, "CONTROL_CABEZA")]


COMPLEMENTOS = {
    "Bigote": bigote,
    "Barba": barba,
    "Gafas": gafas,
    "Puro": puro,
    "Txapela": txapela,
    "SombreroCopa": sombrero_copa,
    "Pajarita": pajarita,
    "Bufanda": bufanda,
    "Pendiente": pendiente,
}

# Lo que lleva cada personaje por defecto (para que se distingan de un vistazo).
POR_DEFECTO = {
    "LILA": ["Gafas", "Pendiente"],
    "MOSTAZA": ["Txapela", "Bigote", "Puro"],
    "MENTA": ["Barba", "Bufanda"],
    "CORAL": ["SombreroCopa", "Pajarita"],
}


def emparentar(o, padre):
    # Mantiene la posicion en el mundo y deja el objeto colgando del control.
    mundo = o.matrix_world.copy()
    o.parent = padre
    o.matrix_world = mundo


def limpiar():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def exportar(ruta):
    limpiar()
    raiz = bpy.data.objects.new("ACC_CONTROL_RAIZ", None)
    bpy.context.collection.objects.link(raiz)
    cabeza = bpy.data.objects.new("ACC_CONTROL_CABEZA", None)
    bpy.context.collection.objects.link(cabeza)
    cabeza.location = CABEZA
    cabeza.parent = raiz
    controles = {"CONTROL_RAIZ": raiz, "CONTROL_CABEZA": cabeza}
    bpy.context.view_layer.update()

    for crear in COMPLEMENTOS.values():
        for o, control in crear():
            bpy.context.view_layer.update()
            emparentar(o, controles[control])

    bpy.ops.export_scene.fbx(filepath=ruta, object_types={"EMPTY", "MESH"}, use_selection=False,
                             apply_scale_options="FBX_SCALE_NONE", axis_forward="-Z", axis_up="Y",
                             bake_anim=False, add_leaf_bones=False, mesh_smooth_type="FACE")
    print("Exportado:", ruta)


def render(carpeta, salida):
    limpiar()
    nombres = ["LILA", "MOSTAZA", "MENTA", "CORAL"]

    for i, n in enumerate(nombres):
        antes = set(bpy.data.objects)
        bpy.ops.import_scene.fbx(filepath=f"{carpeta}/TINKO_{n}_Animaciones.fbx")
        nuevos = [o for o in bpy.data.objects if o not in antes]

        # Sin animaciones, para poder colocarlos en fila.
        for o in nuevos:
            o.animation_data_clear()
        raiz = bpy.data.objects[f"TINKO_{n}_CONTROL_RAIZ"]
        raiz.location.x = (i - 1.5) * 1.6
        bpy.context.view_layer.update()
        controles = {"CONTROL_RAIZ": raiz, "CONTROL_CABEZA": bpy.data.objects[f"TINKO_{n}_CONTROL_CABEZA"]}

        for comp in POR_DEFECTO[n]:
            if comp in TAPAN_CABEZA:
                for o in nuevos:
                    if any(k in o.name for k in ("Gorro_", "Pelo_Casquete")):
                        o.hide_render = True

            for o, control in COMPLEMENTOS[comp]():
                # Se crea respecto a una cabeza en el origen; se lleva a la de este personaje.
                o.matrix_world = controles["CONTROL_RAIZ"].matrix_world @ o.matrix_world
                bpy.context.view_layer.update()
                emparentar(o, controles[control])

        # Colores de cada personaje (como en el juego) para la vista previa.
        colores = {"LILA": (0.78, 0.66, 0.96), "MOSTAZA": (0.88, 0.73, 0.25), "MENTA": (0.55, 0.9, 0.7), "CORAL": (1.0, 0.6, 0.52)}
        ropa = bpy.data.materials.new("Ropa_" + n)
        ropa.use_nodes = True
        ropa.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (*colores[n], 1)
        piel = bpy.data.materials.new("Piel_" + n)
        piel.use_nodes = True
        piel.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (1, 0.8, 0.66, 1)
        for o in nuevos:
            if o.type != "MESH":
                continue
            p = o.name.replace(f"TINKO_{n}_", "")
            if p.startswith(("Poncho", "Manga", "Gorro_ParteAlta")):
                o.data.materials.clear(); o.data.materials.append(ropa)
            elif p.startswith(("Cabeza", "Oreja", "Cuello")) and p != "Cuello_Color":
                o.data.materials.clear(); o.data.materials.append(piel)

    # En el juego la lengua y el mordisco estan escondidos hasta que se hace la sena.
    for o in bpy.data.objects:
        if any(k in o.name for k in ("Lengua", "Mordisco")):
            o.hide_render = True
        if "Parpado" in o.name:
            o.data.materials.clear(); o.data.materials.append(bpy.data.materials["Piel_" + o.name.split("_")[1]])

    escena = bpy.context.scene
    cam = bpy.data.objects.new("Camara", bpy.data.cameras.new("Camara"))
    escena.collection.objects.link(cam)
    cam.data.lens = 50
    escena.camera = cam
    sol = bpy.data.objects.new("Sol", bpy.data.lights.new("Sol", "SUN"))
    sol.data.energy = 3.5
    sol.rotation_euler = Euler((math.radians(50), 0, math.radians(25)))
    escena.collection.objects.link(sol)
    mundo = bpy.data.worlds.new("Mundo")
    mundo.use_nodes = True
    mundo.node_tree.nodes["Background"].inputs[0].default_value = (0.35, 0.27, 0.2, 1)
    mundo.node_tree.nodes["Background"].inputs[1].default_value = 0.8
    escena.world = mundo
    escena.render.engine = "CYCLES"
    escena.cycles.samples = 24
    escena.cycles.device = "CPU"
    escena.render.resolution_x = 560
    escena.render.resolution_y = 640
    partes = []

    for n in nombres:
        cabeza = bpy.data.objects[f"TINKO_{n}_CONTROL_CABEZA"].matrix_world.translation
        objetivo = cabeza + Vector((0, 0, -0.35))
        cam.location = objetivo + Vector((1.5, -4.6, 0.7))
        cam.rotation_euler = (objetivo - cam.location).to_track_quat("-Z", "Y").to_euler()
        escena.render.filepath = salida.replace(".png", "_" + n + ".png")
        bpy.ops.render.render(write_still=True)
        partes.append(escena.render.filepath)

    print("Render:", partes)


if __name__ == "__main__":
    args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    if args and args[0] == "exportar":
        exportar(args[1])
    elif args and args[0] == "render":
        render(args[1], args[2])
    else:
        exportar(bpy.path.abspath("//Complementos_Tinko.fbx"))
