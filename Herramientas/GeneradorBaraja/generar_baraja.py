"""Genera la baraja espanola de Mus TINKO (40 cartas) como hoja de sprites.

Uso:  python3 generar_baraja.py
Salida: Assets/Resources/Baraja/Cartas_Tinko.png (+ .meta con los 40 sprites cortados)

Rejilla: 10 columnas (AS,2,3,4,5,6,7,SOTA,CABALLO,REY) x 4 filas
(ESPADAS, COPAS, OROS, BASTOS), igual que la baraja anterior, para que
DibujoCartaAutomaticoMus funcione sin cambios.
"""
import math
import os
import random
import re
import uuid

from PIL import Image, ImageDraw, ImageFont, ImageFilter

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DESTINO = os.path.join(RAIZ, "Assets", "Resources", "Baraja")
NOMBRE = "Cartas_Tinko"

W, H = 486, 762
HOJA_W, HOJA_H = 4868, 3048
SS = 2
CW, CH = W * SS, H * SS

FUENTE = "/usr/share/fonts/truetype/dejavu/DejaVuSerif-Bold.ttf"

CREMA = (251, 246, 234, 255)
TINTA = (40, 32, 28, 255)
ORO = (242, 190, 46, 255)
ORO_OSC = (176, 120, 20, 255)
ROJO = (178, 34, 40, 255)
ROJO_OSC = (110, 18, 24, 255)
AZUL = (44, 96, 160, 255)
AZUL_CLARO = (160, 200, 236, 255)
VERDE = (52, 120, 64, 255)
MADERA = (150, 92, 44, 255)
MADERA_CLARA = (204, 140, 74, 255)
PIEL = (241, 200, 150, 255)

PALOS = ["ESPADAS", "COPAS", "OROS", "BASTOS"]
COLOR_PALO = {"ESPADAS": AZUL, "COPAS": ROJO, "OROS": ORO_OSC, "BASTOS": VERDE}
CORTES = {"OROS": 0, "COPAS": 1, "ESPADAS": 2, "BASTOS": 3}
NUMEROS = [1, 2, 3, 4, 5, 6, 7, 10, 11, 12]


def lienzo(tam):
    return Image.new("RGBA", (tam, tam), (0, 0, 0, 0))


def simbolo(palo, tam):
    im = lienzo(tam)
    d = ImageDraw.Draw(im)
    s = tam
    lw = max(2, int(s * 0.018))
    cx = s / 2

    if palo == "OROS":
        r = s * 0.44
        d.ellipse([cx - r, cx - r, cx + r, cx + r], fill=ORO_OSC)
        r2 = r * 0.9
        d.ellipse([cx - r2, cx - r2, cx + r2, cx + r2], fill=ORO, outline=ORO_OSC, width=lw)
        r3 = r * 0.62
        d.ellipse([cx - r3, cx - r3, cx + r3, cx + r3], outline=ORO_OSC, width=lw)
        puntos = []
        for i in range(16):
            ang = math.pi * i / 8 - math.pi / 2
            rr = r3 * (0.95 if i % 2 == 0 else 0.45)
            puntos.append((cx + rr * math.cos(ang), cx + rr * math.sin(ang)))
        d.polygon(puntos, fill=ROJO, outline=ORO_OSC)
        r4 = r * 0.16
        d.ellipse([cx - r4, cx - r4, cx + r4, cx + r4], fill=ORO, outline=ORO_OSC, width=lw)
        for i in range(24):
            ang = 2 * math.pi * i / 24
            x, y = cx + r * 0.95 * math.cos(ang), cx + r * 0.95 * math.sin(ang)
            d.ellipse([x - lw, y - lw, x + lw, y + lw], fill=ORO)

    elif palo == "COPAS":
        top = s * 0.08
        d.ellipse([s * 0.14, top, s * 0.86, top + s * 0.14], fill=ROJO_OSC, outline=ORO_OSC, width=lw)
        d.chord([s * 0.14, top - s * 0.3, s * 0.86, top + s * 0.44], 0, 180, fill=ORO, outline=ORO_OSC, width=lw)
        d.polygon([(s * 0.17, top + s * 0.14), (s * 0.83, top + s * 0.14), (s * 0.8, top + s * 0.2), (s * 0.2, top + s * 0.2)], fill=ROJO)
        d.ellipse([s * 0.14, top, s * 0.86, top + s * 0.14], fill=ROJO_OSC, outline=ORO_OSC, width=lw)
        d.rectangle([s * 0.46, top + s * 0.43, s * 0.54, s * 0.72], fill=ORO, outline=ORO_OSC, width=lw)
        d.ellipse([s * 0.39, s * 0.58, s * 0.61, s * 0.66], fill=ROJO, outline=ORO_OSC, width=lw)
        d.polygon([(s * 0.3, s * 0.86), (s * 0.7, s * 0.86), (s * 0.58, s * 0.72), (s * 0.42, s * 0.72)],
                  fill=ORO, outline=ORO_OSC)
        d.rectangle([s * 0.26, s * 0.86, s * 0.74, s * 0.92], fill=ROJO, outline=ORO_OSC, width=lw)

    elif palo == "ESPADAS":
        d.polygon([(cx, s * 0.02), (cx + s * 0.06, s * 0.1), (cx + s * 0.05, s * 0.64),
                   (cx - s * 0.05, s * 0.64), (cx - s * 0.06, s * 0.1)], fill=AZUL, outline=TINTA)
        d.polygon([(cx, s * 0.06), (cx + s * 0.018, s * 0.12), (cx + s * 0.012, s * 0.6),
                   (cx - s * 0.012, s * 0.6), (cx - s * 0.018, s * 0.12)], fill=AZUL_CLARO)
        d.rounded_rectangle([cx - s * 0.22, s * 0.63, cx + s * 0.22, s * 0.69], radius=s * 0.02,
                            fill=ORO, outline=ORO_OSC, width=lw)
        for lado in (-1, 1):
            x = cx + lado * s * 0.24
            d.ellipse([x - s * 0.035, s * 0.625, x + s * 0.035, s * 0.695], fill=ORO, outline=ORO_OSC, width=lw)
        d.rectangle([cx - s * 0.035, s * 0.69, cx + s * 0.035, s * 0.88], fill=ROJO, outline=ROJO_OSC, width=lw)
        for y in (0.74, 0.8):
            d.rectangle([cx - s * 0.04, s * y, cx + s * 0.04, s * (y + 0.02)], fill=ORO)
        d.ellipse([cx - s * 0.06, s * 0.87, cx + s * 0.06, s * 0.98], fill=ORO, outline=ORO_OSC, width=lw)

    else:
        d.polygon([(cx - s * 0.05, s * 0.96), (cx + s * 0.05, s * 0.96), (cx + s * 0.13, s * 0.2),
                   (cx - s * 0.13, s * 0.2)], fill=MADERA, outline=TINTA)
        d.ellipse([cx - s * 0.13, s * 0.03, cx + s * 0.13, s * 0.3], fill=MADERA, outline=TINTA, width=lw)
        d.polygon([(cx - s * 0.015, s * 0.92), (cx + s * 0.02, s * 0.92), (cx + s * 0.06, s * 0.15),
                   (cx + s * 0.0, s * 0.12)], fill=MADERA_CLARA)
        for (y, lado) in ((0.35, -1), (0.5, 1), (0.65, -1), (0.8, 1)):
            x = cx + lado * s * (0.13 - (y - 0.2) * 0.1)
            d.ellipse([x - s * 0.05, s * y - s * 0.03, x + s * 0.05, s * y + s * 0.03], fill=VERDE, outline=TINTA)
        d.ellipse([cx - s * 0.04, s * 0.08, cx + s * 0.02, s * 0.14], fill=MADERA_CLARA)

    return im


def pegar(base, im, cx, cy):
    base.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


DISPOSICIONES = {
    2: [(0.5, 0.22), (0.5, 0.78)],
    3: [(0.5, 0.18), (0.5, 0.5), (0.5, 0.82)],
    4: [(0.3, 0.24), (0.7, 0.24), (0.3, 0.76), (0.7, 0.76)],
    5: [(0.3, 0.2), (0.7, 0.2), (0.5, 0.5), (0.3, 0.8), (0.7, 0.8)],
    6: [(0.3, 0.18), (0.7, 0.18), (0.3, 0.5), (0.7, 0.5), (0.3, 0.82), (0.7, 0.82)],
    7: [(0.3, 0.16), (0.7, 0.16), (0.5, 0.33), (0.3, 0.5), (0.7, 0.5), (0.3, 0.84), (0.7, 0.84)],
}


def marco(d, palo):
    color = COLOR_PALO[palo]
    m = int(CW * 0.035)
    d.rounded_rectangle([m, m, CW - m, CH - m], radius=int(CW * 0.05), outline=TINTA, width=SS * 2)
    m2 = int(CW * 0.075)
    d.rounded_rectangle([m2, m2, CW - m2, CH - m2], radius=int(CW * 0.035), outline=color, width=SS * 5)
    cortes = CORTES[palo]
    hueco = CW * 0.05
    for i in range(cortes):
        x = CW / 2 + (i - (cortes - 1) / 2) * CW * 0.14
        for y in (m2, CH - m2):
            d.rectangle([x - hueco / 2, y - SS * 5, x + hueco / 2, y + SS * 5], fill=CREMA)


def indices(base, numero, palo):
    capa = Image.new("RGBA", base.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    fuente = ImageFont.truetype(FUENTE, int(CH * 0.075))
    texto = str(numero)
    x, y = CW * 0.11, CH * 0.085
    d.text((x, y), texto, font=fuente, fill=COLOR_PALO[palo] if palo != "OROS" else ORO_OSC, anchor="lm")
    pegar(capa, simbolo(palo, int(CW * 0.12)), x + CW * 0.03 + (CW * 0.04 if numero >= 10 else 0), y + CH * 0.075)
    base.alpha_composite(capa)
    base.alpha_composite(capa.rotate(180))


def figura(base, numero, palo):
    d = ImageDraw.Draw(base)
    color = COLOR_PALO[palo]
    x0, y0, x1, y1 = CW * 0.16, CH * 0.14, CW * 0.84, CH * 0.86
    fondo = tuple(int(c * 0.18 + 255 * 0.82) for c in color[:3]) + (255,)
    d.rounded_rectangle([x0, y0, x1, y1], radius=CW * 0.04, fill=fondo, outline=color, width=SS * 4)
    cx = CW / 2
    lw = SS * 3

    if numero == 11:
        cuerpo = [(cx - CW * 0.2, CH * 0.78), (cx - CW * 0.18, CH * 0.55), (cx - CW * 0.05, CH * 0.42),
                  (cx - CW * 0.02, CH * 0.27), (cx + CW * 0.06, CH * 0.22), (cx + CW * 0.2, CH * 0.33),
                  (cx + CW * 0.18, CH * 0.38), (cx + CW * 0.08, CH * 0.35), (cx + CW * 0.1, CH * 0.5),
                  (cx + CW * 0.2, CH * 0.62), (cx + CW * 0.2, CH * 0.78)]
        d.polygon(cuerpo, fill=(120, 84, 60, 255), outline=TINTA)
        d.polygon([(cx - CW * 0.01, CH * 0.27), (cx - CW * 0.05, CH * 0.2), (cx + CW * 0.02, CH * 0.24)],
                  fill=(120, 84, 60, 255), outline=TINTA)
        d.ellipse([cx + CW * 0.07, CH * 0.26, cx + CW * 0.1, CH * 0.28], fill=TINTA)
        d.line([(cx - CW * 0.06, CH * 0.3), (cx - CW * 0.16, CH * 0.5)], fill=TINTA, width=lw * 3)
        d.rectangle([cx - CW * 0.12, CH * 0.5, cx + CW * 0.06, CH * 0.58], fill=color, outline=TINTA, width=lw)
        d.line([(cx + CW * 0.1, CH * 0.36), (cx - CW * 0.08, CH * 0.5)], fill=ORO_OSC, width=lw)
    else:
        d.polygon([(cx - CW * 0.22, CH * 0.82), (cx - CW * 0.16, CH * 0.5), (cx + CW * 0.16, CH * 0.5),
                   (cx + CW * 0.22, CH * 0.82)], fill=color, outline=TINTA)
        d.polygon([(cx - CW * 0.16, CH * 0.5), (cx, CH * 0.62), (cx + CW * 0.16, CH * 0.5),
                   (cx + CW * 0.1, CH * 0.46), (cx - CW * 0.1, CH * 0.46)], fill=ORO, outline=TINTA)
        d.rectangle([cx - CW * 0.03, CH * 0.6, cx + CW * 0.03, CH * 0.82], fill=ORO, outline=TINTA, width=lw)
        r = CW * 0.11
        hy = CH * 0.36
        d.ellipse([cx - r, hy - r, cx + r, hy + r], fill=PIEL, outline=TINTA, width=lw)
        d.ellipse([cx - r * 0.45, hy - r * 0.15, cx - r * 0.25, hy + r * 0.05], fill=TINTA)
        d.ellipse([cx + r * 0.25, hy - r * 0.15, cx + r * 0.45, hy + r * 0.05], fill=TINTA)
        if numero == 12:
            d.chord([cx - r * 0.95, hy - r * 0.1, cx + r * 0.95, hy + r * 1.35], 0, 180, fill=(110, 70, 40, 255), outline=TINTA)
            base_corona = hy - r * 0.75
            d.polygon([(cx - r, base_corona), (cx - r, base_corona - r * 0.7), (cx - r * 0.5, base_corona - r * 0.3),
                       (cx, base_corona - r * 0.9), (cx + r * 0.5, base_corona - r * 0.3), (cx + r, base_corona - r * 0.7),
                       (cx + r, base_corona)], fill=ORO, outline=ORO_OSC)
            for xx in (-0.5, 0, 0.5):
                d.ellipse([cx + r * xx - r * 0.1, base_corona - r * 0.25, cx + r * xx + r * 0.1, base_corona - r * 0.05], fill=ROJO)
        else:
            d.arc([cx - r * 0.4, hy + r * 0.1, cx + r * 0.4, hy + r * 0.6], 20, 160, fill=TINTA, width=lw)
            d.ellipse([cx - r * 1.25, hy - r * 1.25, cx + r * 1.25, hy - r * 0.45], fill=color, outline=TINTA, width=lw)
            d.line([(cx + r * 0.6, hy - r * 1.0), (cx + r * 1.6, hy - r * 1.9)], fill=ROJO, width=lw * 4)

    pegar(base, simbolo(palo, int(CW * 0.3)), cx + CW * 0.2, CH * 0.66)

    fuente = ImageFont.truetype(FUENTE, int(CH * 0.035))
    nombre = {10: "SOTA", 11: "CABALLO", 12: "REY"}[numero]
    d.text((cx, CH * 0.835), nombre, font=fuente, fill=TINTA, anchor="mm")


def carta(numero, palo):
    base = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    d = ImageDraw.Draw(base)
    d.rounded_rectangle([0, 0, CW - 1, CH - 1], radius=int(CW * 0.07), fill=CREMA)
    marco(d, palo)

    if numero == 1:
        pegar(base, simbolo(palo, int(CW * 0.78)), CW / 2, CH / 2)
    elif numero <= 7:
        tam = {2: 0.4, 3: 0.34, 4: 0.3, 5: 0.28, 6: 0.26, 7: 0.25}[numero]
        if palo in ("ESPADAS", "BASTOS") and numero != 3:
            tam *= 1.15
        if numero == 3:
            tam = 0.3
        x0, y0, ancho, alto = CW * 0.14, CH * 0.13, CW * 0.72, CH * 0.74
        for (px, py) in DISPOSICIONES[numero]:
            pegar(base, simbolo(palo, int(CW * tam)), x0 + px * ancho, y0 + py * alto)
    else:
        figura(base, numero, palo)

    indices(base, numero, palo)
    return base.resize((W, H), Image.LANCZOS)


def reverso():
    base = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    d = ImageDraw.Draw(base)
    d.rounded_rectangle([0, 0, CW - 1, CH - 1], radius=int(CW * 0.07), fill=(120, 24, 36, 255))
    d.rounded_rectangle([CW * 0.06, CW * 0.06, CW * 0.94, CH - CW * 0.06], radius=int(CW * 0.04), outline=ORO, width=SS * 5)
    paso = CW * 0.08
    for i in range(-20, 30):
        d.line([(i * paso, CW * 0.1), (i * paso + CH, CH)], fill=(150, 40, 50, 255), width=SS * 3)
        d.line([(i * paso, CH - CW * 0.1), (i * paso + CH, 0)], fill=(150, 40, 50, 255), width=SS * 3)
    mascara = Image.new("L", (CW, CH), 0)
    ImageDraw.Draw(mascara).rounded_rectangle([CW * 0.09, CW * 0.09, CW * 0.91, CH - CW * 0.09], radius=int(CW * 0.03), fill=255)
    fondo = Image.new("RGBA", (CW, CH), (0, 0, 0, 0))
    ImageDraw.Draw(fondo).rounded_rectangle([0, 0, CW - 1, CH - 1], radius=int(CW * 0.07), fill=(120, 24, 36, 255))
    ImageDraw.Draw(fondo).rounded_rectangle([CW * 0.06, CW * 0.06, CW * 0.94, CH - CW * 0.06], radius=int(CW * 0.04), outline=ORO, width=SS * 5)
    fondo.paste(base, (0, 0), mascara)
    d = ImageDraw.Draw(fondo)
    r = CW * 0.22
    d.ellipse([CW / 2 - r, CH / 2 - r, CW / 2 + r, CH / 2 + r], fill=(120, 24, 36, 255), outline=ORO, width=SS * 5)
    fuente = ImageFont.truetype(FUENTE, int(CH * 0.06))
    d.text((CW / 2, CH / 2), "TINKO", font=fuente, fill=ORO, anchor="mm")
    return fondo.resize((W, H), Image.LANCZOS)


def escribir_meta(ruta_png, guid):
    plantilla = os.path.join(os.path.dirname(__file__), "plantilla_sprites.meta")
    texto = open(plantilla, encoding="latin-1").read()
    texto = re.sub(r"^guid: \w+", "guid: " + guid, texto, flags=re.M)
    texto = texto.replace("NOMBRE_HOJA", NOMBRE)
    rng = random.Random(guid)
    texto = re.sub(r"spriteID: \w+", lambda m: "spriteID: " + uuid.UUID(int=rng.getrandbits(128)).hex, texto)
    open(ruta_png + ".meta", "w", encoding="latin-1", newline="\n").write(texto)


def main():
    os.makedirs(DESTINO, exist_ok=True)
    hoja = Image.new("RGBA", (HOJA_W, HOJA_H), (0, 0, 0, 0))

    for fila, palo in enumerate(PALOS):
        for col, numero in enumerate(NUMEROS):
            hoja.alpha_composite(carta(numero, palo), (col * W, fila * H))

    ruta = os.path.join(DESTINO, NOMBRE + ".png")
    hoja.save(ruta, optimize=True)
    reverso().save(os.path.join(DESTINO, "Reverso_Tinko.png"), optimize=True)

    meta = ruta + ".meta"
    guid = None
    if os.path.exists(meta):
        m = re.search(r"^guid: (\w+)", open(meta, encoding="latin-1").read(), flags=re.M)
        guid = m.group(1) if m else None
    escribir_meta(ruta, guid or uuid.uuid4().hex)
    print("Generado:", ruta)


if __name__ == "__main__":
    main()
