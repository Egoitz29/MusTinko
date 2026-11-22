"""Baraja espanola de Mus TINKO, estilo tradicional (como las de verdad).

- Carton marfil con grano de papel, borde negro redondeado.
- "Pintas": cortes en el marco que dicen el palo sin ver la carta
  (oros sin corte, copas 1, espadas 2, bastos 3).
- Numeros en dos esquinas.
- Palos con detalle: moneda acordonada, caliz con nudo, espada de acero con guarda, garrote con nudos.
- Espadas y bastos cruzados en aspa; oros y copas en rejilla.
- Figuras de cuerpo entero: sota de pie, caballo con jinete, rey con corona y capa de armino.

Uso:
  python3 baraja_tradicional.py            -> escribe Assets/Resources/Baraja/Cartas_Tinko.png (mismo orden y meta)
  python3 baraja_tradicional.py muestra X  -> solo una hoja de muestra en X (no toca el proyecto)
"""
import math
import os
import random
import re
import sys

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, ImageFont

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DESTINO = os.path.join(RAIZ, "Assets", "Resources", "Baraja")
NOMBRE = "Cartas_Tinko"

W, H = 486, 762
SS = 3
CW, CH = W * SS, H * SS
FUENTE = "/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf"

PAPEL = (251, 249, 243, 255)
TINTA = (28, 22, 20, 255)
ROJO = (196, 34, 40, 255)
ROJO_CLARO = (232, 84, 76, 255)
ROJO_OSC = (118, 14, 22, 255)
AZUL = (36, 82, 166, 255)
AZUL_CLARO = (96, 148, 214, 255)
AZUL_OSC = (16, 40, 96, 255)
ACERO_CLARO = (232, 240, 248, 255)
ACERO = (148, 168, 190, 255)
ACERO_OSC = (84, 100, 122, 255)
ORO = (236, 184, 40, 255)
ORO_CLARO = (255, 232, 132, 255)
ORO_OSC = (150, 98, 12, 255)
VERDE = (40, 128, 66, 255)
VERDE_CLARO = (110, 186, 96, 255)
VERDE_OSC = (18, 72, 36, 255)
MADERA = (156, 98, 46, 255)
MADERA_CLARA = (214, 160, 96, 255)
MADERA_OSC = (90, 54, 24, 255)
PIEL = (248, 210, 172, 255)
PIEL_OSC = (210, 150, 112, 255)
BLANCO = (252, 250, 244, 255)
GRIS = (170, 170, 176, 255)
NEGRO_PELO = (52, 34, 24, 255)

PALOS = ["ESPADAS", "COPAS", "OROS", "BASTOS"]          # filas de la hoja (como siempre)
NUMEROS = [1, 2, 3, 4, 5, 6, 7, 10, 11, 12]             # columnas
COLOR_PALO = {"ESPADAS": AZUL, "COPAS": ROJO, "OROS": ORO_OSC, "BASTOS": VERDE}
CORTES = {"OROS": 0, "COPAS": 1, "ESPADAS": 2, "BASTOS": 3}

LW = 2 * SS   # grosor de linea normal


# ---------------------------------------------------------------- utilidades

def capa(w, h):
    return Image.new("RGBA", (int(w), int(h)), (0, 0, 0, 0))


def gradiente(tam, c1, c2, ang=90.0, radial=False, centro=(0.38, 0.32)):
    w, h = max(1, int(tam[0])), max(1, int(tam[1]))
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    if radial:
        t = np.sqrt(((x - w * centro[0]) / w) ** 2 + ((y - h * centro[1]) / h) ** 2) / 0.8
    else:
        a = math.radians(ang)
        t = x * math.cos(a) + y * math.sin(a)
        t = (t - t.min()) / (t.max() - t.min() + 1e-6)
    t = np.clip(t, 0, 1)[..., None]
    c = np.array(c1, np.float32) * (1 - t) + np.array(c2, np.float32) * t
    return Image.fromarray(c.astype(np.uint8), "RGBA")


def rellenar(base, dibujar, c1, c2=None, ang=90.0, radial=False, centro=(0.38, 0.32)):
    """Pinta en 'base' la forma que dibuja 'dibujar(d)' (con fill=255) con un degradado."""
    m = Image.new("L", base.size, 0)
    dibujar(ImageDraw.Draw(m))
    bb = m.getbbox()
    if not bb:
        return
    g = gradiente((bb[2] - bb[0], bb[3] - bb[1]), c1, c2 or c1, ang, radial, centro)
    g.putalpha(ImageChops.multiply(m.crop(bb), g.getchannel("A")))
    base.alpha_composite(g, (bb[0], bb[1]))


def pol(base, pts, c1, c2=None, ang=90.0, borde=TINTA, g=LW, radial=False):
    rellenar(base, lambda d: d.polygon(pts, fill=255), c1, c2, ang, radial)
    if borde:
        ImageDraw.Draw(base).line(list(pts) + [pts[0]], fill=borde, width=g, joint="curve")


def eli(base, caja, c1, c2=None, ang=90.0, borde=TINTA, g=LW, radial=False, centro=(0.38, 0.32)):
    rellenar(base, lambda d: d.ellipse(caja, fill=255), c1, c2, ang, radial, centro)
    if borde:
        ImageDraw.Draw(base).ellipse(caja, outline=borde, width=g)


def rect(base, caja, c1, c2=None, ang=90.0, borde=TINTA, g=LW, radio=0):
    rellenar(base, lambda d: d.rounded_rectangle(caja, radius=radio, fill=255), c1, c2, ang)
    if borde:
        ImageDraw.Draw(base).rounded_rectangle(caja, radius=radio, outline=borde, width=g)


def pegar(base, im, cx, cy, giro=0.0):
    if giro:
        im = im.rotate(giro, resample=Image.BICUBIC, expand=True)
    base.alpha_composite(im, (int(cx - im.width / 2), int(cy - im.height / 2)))


def curva(p0, p1, p2, n=16):
    """Puntos de una curva cuadratica (para formas suaves)."""
    return [((1 - t) ** 2 * p0[0] + 2 * (1 - t) * t * p1[0] + t ** 2 * p2[0],
             (1 - t) ** 2 * p0[1] + 2 * (1 - t) * t * p1[1] + t ** 2 * p2[1]) for t in (i / n for i in range(n + 1))]


def brillo(base, caja, alfa=90):
    """Reflejo blanco suave (da volumen)."""
    c = capa(*base.size)
    ImageDraw.Draw(c).ellipse(caja, fill=(255, 255, 255, alfa))
    base.alpha_composite(c.filter(ImageFilter.GaussianBlur(SS * 3)))


# ---------------------------------------------------------------- los palos

def perfil(im, caja, linea):
    """Cara de perfil mirando a la izquierda, dentro de la moneda."""
    x0, y0, x1, y1 = caja
    w, h = x1 - x0, y1 - y0
    P = lambda x, y: (x0 + x * w, y0 + y * h)
    cara_pts = [P(0.56, 0.1), P(0.78, 0.18), P(0.84, 0.42), P(0.74, 0.6), P(0.72, 0.86), P(0.42, 0.86), P(0.4, 0.68),
                P(0.33, 0.64), P(0.34, 0.58), P(0.29, 0.55), P(0.32, 0.5), P(0.21, 0.44), P(0.3, 0.37), P(0.31, 0.25), P(0.4, 0.14)]
    pol(im, cara_pts, (255, 238, 170, 255), ORO, ang=0, borde=linea, g=max(2, SS))
    pelo = [P(0.5, 0.11), P(0.78, 0.18), P(0.84, 0.42), P(0.74, 0.6), P(0.62, 0.52), P(0.6, 0.3), P(0.44, 0.2)]
    pol(im, pelo, ORO, ORO_OSC, borde=linea, g=max(2, SS))
    d = ImageDraw.Draw(im)
    for k in range(4):                                   # rizos
        cx_, cy_ = P(0.66 + 0.04 * (k % 2), 0.24 + k * 0.08)
        rr = w * 0.05
        d.arc([cx_ - rr, cy_ - rr, cx_ + rr, cy_ + rr], 30, 300, fill=linea, width=max(2, SS))
    for k in range(5):                                   # laurel en la cabeza
        cx_, cy_ = P(0.4 + k * 0.08, 0.13 + abs(k - 2) * 0.015)
        rr = w * 0.035
        eli(im, [cx_ - rr, cy_ - rr * 1.6, cx_ + rr, cy_ + rr * 1.6], VERDE_CLARO, VERDE, borde=VERDE_OSC, g=max(1, SS // 2))
    ex, ey = P(0.36, 0.38)                               # ojo
    d.ellipse([ex - w * 0.025, ey - w * 0.018, ex + w * 0.025, ey + w * 0.018], fill=linea)


def moneda(s, grande=False):
    im = capa(s, s)
    c = s / 2
    r = s * 0.47
    # borde rojizo y aro amarillo, color plano como las cartas impresas
    eli(im, [c - r, c - r, c + r, c + r], ROJO_CLARO, ROJO, g=LW + SS)
    r2 = r * 0.86
    eli(im, [c - r2, c - r2, c + r2, c + r2], (255, 214, 64, 255), ORO, radial=True, g=LW)
    for i in range(40 if grande else 28):                # puntos del canto
        a = 2 * math.pi * i / (40 if grande else 28)
        x, y = c + r * 0.93 * math.cos(a), c + r * 0.93 * math.sin(a)
        rr = r * 0.025
        ImageDraw.Draw(im).ellipse([x - rr, y - rr, x + rr, y + rr], fill=(255, 220, 120, 255))
    r3 = r * 0.66
    ImageDraw.Draw(im).ellipse([c - r3, c - r3, c + r3, c + r3], outline=ROJO_OSC, width=max(2, SS))
    perfil(im, [c - r3 * 0.98, c - r3 * 1.0, c + r3 * 0.98, c + r3 * 1.0], ROJO_OSC)
    brillo(im, [c - r * 0.55, c - r * 0.78, c - r * 0.15, c - r * 0.58], 35)
    return im


def copa(s, grande=False):
    w, h = s * 0.82, s
    im = capa(w, h)
    cx = w / 2
    # copa (cuenco)
    izq, der = w * 0.08, w * 0.92
    borde_y = h * 0.1
    cuenco = curva((izq, borde_y + h * 0.05), (w * 0.06, h * 0.42), (cx, h * 0.5)) + \
        curva((cx, h * 0.5), (w * 0.94, h * 0.42), (der, borde_y + h * 0.05))[1:]
    pol(im, cuenco, ORO_OSC, ORO_CLARO, ang=0)
    pol(im, cuenco, (0, 0, 0, 0), borde=TINTA, g=LW)
    # banda roja con joyas
    banda = [(w * 0.13, h * 0.24), (w * 0.87, h * 0.24), (w * 0.83, h * 0.31), (w * 0.17, h * 0.31)]
    pol(im, banda, ROJO_CLARO, ROJO_OSC, borde=TINTA, g=max(2, SS))
    for i in range(5):
        x = w * (0.24 + i * 0.13)
        rr = w * 0.03
        eli(im, [x - rr, h * 0.275 - rr, x + rr, h * 0.275 + rr], ORO_CLARO, ORO_OSC, radial=True, g=max(1, SS))
    # boca (vino)
    eli(im, [izq, borde_y, der, borde_y + h * 0.1], ORO, ORO_OSC, g=LW)
    eli(im, [izq + w * 0.05, borde_y + h * 0.015, der - w * 0.05, borde_y + h * 0.085], ROJO_OSC, ROJO, borde=None)
    # tallo, nudo y pie
    rect(im, [cx - w * 0.05, h * 0.49, cx + w * 0.05, h * 0.8], ORO_OSC, ORO_CLARO, ang=0, g=max(2, SS))
    eli(im, [cx - w * 0.14, h * 0.6, cx + w * 0.14, h * 0.69], ORO_CLARO, ORO_OSC, radial=True, g=LW)
    rr = w * 0.04
    eli(im, [cx - rr, h * 0.645 - rr, cx + rr, h * 0.645 + rr], ROJO_CLARO, ROJO_OSC, radial=True, g=max(1, SS))
    pol(im, [(cx - w * 0.07, h * 0.79), (cx + w * 0.07, h * 0.79), (cx + w * 0.3, h * 0.92), (cx - w * 0.3, h * 0.92)],
        ORO_OSC, ORO_CLARO, ang=0)
    eli(im, [cx - w * 0.33, h * 0.89, cx + w * 0.33, h * 0.97], ORO, ORO_OSC, g=LW)
    brillo(im, [w * 0.22, h * 0.34, w * 0.3, h * 0.44], 40)
    return im


def espada(largo, grande=False):
    w, h = largo * 0.3, largo
    im = capa(w, h)
    cx = w / 2
    hb = w * 0.11                         # media anchura de la hoja
    guarda_y = h * 0.72
    # hoja
    hoja = [(cx, h * 0.01), (cx + hb, h * 0.07), (cx + hb * 0.85, guarda_y), (cx - hb * 0.85, guarda_y), (cx - hb, h * 0.07)]
    pol(im, hoja, AZUL_CLARO, (196, 222, 246, 255), ang=0, borde=AZUL_OSC)
    ImageDraw.Draw(im).line([(cx, h * 0.05), (cx, guarda_y - h * 0.01)], fill=AZUL, width=max(2, SS))
    ImageDraw.Draw(im).line([(cx + hb * 0.35, h * 0.08), (cx + hb * 0.3, guarda_y - h * 0.02)], fill=(255, 255, 255, 150), width=max(2, SS))
    # guarda con gavilanes curvos
    gy = guarda_y
    rect(im, [cx - w * 0.36, gy - h * 0.012, cx + w * 0.36, gy + h * 0.022], ORO_CLARO, ORO_OSC, g=max(2, SS), radio=h * 0.01)
    for lado in (-1, 1):
        x = cx + lado * w * 0.4
        rr = w * 0.07
        eli(im, [x - rr, gy - h * 0.03, x + rr, gy + h * 0.005], ORO_CLARO, ORO_OSC, radial=True, g=max(2, SS))
    rr = w * 0.08
    eli(im, [cx - rr, gy - h * 0.012, cx + rr, gy + h * 0.035], ROJO_CLARO, ROJO_OSC, radial=True, g=max(2, SS))
    # puno forrado
    rect(im, [cx - w * 0.06, gy + h * 0.03, cx + w * 0.06, h * 0.9], ROJO_OSC, ROJO_CLARO, ang=0, g=max(2, SS))
    for k in range(7):
        y = gy + h * 0.04 + k * h * 0.022
        ImageDraw.Draw(im).line([(cx - w * 0.06, y), (cx + w * 0.06, y + h * 0.012)], fill=ORO, width=max(2, SS))
    # pomo
    eli(im, [cx - w * 0.1, h * 0.89, cx + w * 0.1, h * 0.97], ORO_CLARO, ORO_OSC, radial=True, g=LW)
    return im


def basto(largo, grande=False):
    w, h = largo * 0.34, largo
    im = capa(w, h)
    cx = w / 2
    cuerpo = curva((cx - w * 0.07, h * 0.98), (cx - w * 0.2, h * 0.5), (cx - w * 0.2, h * 0.12)) + \
        curva((cx - w * 0.2, h * 0.12), (cx, h * -0.04), (cx + w * 0.2, h * 0.12))[1:] + \
        curva((cx + w * 0.2, h * 0.12), (cx + w * 0.2, h * 0.5), (cx + w * 0.07, h * 0.98))[1:]
    pol(im, cuerpo, MADERA_OSC, MADERA_CLARA, ang=0)
    pol(im, cuerpo, (0, 0, 0, 0), borde=TINTA, g=LW)
    # vetas
    for k in range(4):
        x = cx + (k - 1.5) * w * 0.06
        ImageDraw.Draw(im).line(curva((x, h * 0.15), (x + w * 0.03, h * 0.5), (x * 0.98 + cx * 0.02, h * 0.92), 12),
                                fill=MADERA_OSC, width=max(1, SS))
    # nudos (ramas cortadas)
    for (y, lado) in ((0.28, -1), (0.43, 1), (0.58, -1), (0.73, 1)) if not grande else ((0.22, -1), (0.34, 1), (0.46, -1), (0.58, 1), (0.7, -1), (0.82, 1)):
        x = cx + lado * w * (0.2 - (y - 0.1) * 0.12)
        p = [(x, h * y - h * 0.02), (x + lado * w * 0.16, h * y - h * 0.045), (x + lado * w * 0.18, h * y + h * 0.005), (x, h * y + h * 0.025)]
        pol(im, p, MADERA, MADERA_OSC, borde=TINTA, g=max(2, SS))
        ex = x + lado * w * 0.17
        eli(im, [ex - w * 0.035, h * y - h * 0.04, ex + w * 0.035, h * y + h * 0.0], MADERA_CLARA, MADERA, borde=TINTA, g=max(1, SS))
    # hojas arriba
    for lado in (-1, 1):
        bx, by = cx + lado * w * 0.12, h * 0.08
        hoja = curva((bx, by), (bx + lado * w * 0.35, by - h * 0.06), (bx + lado * w * 0.42, by + h * 0.04)) + \
            curva((bx + lado * w * 0.42, by + h * 0.04), (bx + lado * w * 0.2, by + h * 0.05), (bx, by))[1:]
        pol(im, hoja, VERDE_CLARO, VERDE_OSC, borde=TINTA, g=max(2, SS))
    brillo(im, [cx - w * 0.12, h * 0.1, cx - w * 0.02, h * 0.5], 60)
    return im


def porra(largo, verde=True):
    """Basto de las cartas de numero: porra alargada de colores, gruesa arriba y fina abajo."""
    w, h = largo * 0.3, largo
    im = capa(w, h)
    cx = w / 2
    cuerpo = curva((cx - w * 0.06, h * 0.93), (cx - w * 0.36, h * 0.45), (cx - w * 0.26, h * 0.12)) + \
        curva((cx - w * 0.26, h * 0.12), (cx, h * -0.05), (cx + w * 0.26, h * 0.12))[1:] + \
        curva((cx + w * 0.26, h * 0.12), (cx + w * 0.36, h * 0.45), (cx + w * 0.06, h * 0.93))[1:]
    claro_, oscuro_ = ((150, 210, 90, 255), VERDE) if verde else ((255, 150, 70, 255), ROJO)
    pol(im, cuerpo, claro_, oscuro_, ang=0)
    # punta de otro color (como en la baraja de verdad)
    punta = curva((cx - w * 0.27, h * 0.2), (cx, h * -0.05), (cx + w * 0.27, h * 0.2)) + [(cx + w * 0.3, h * 0.27), (cx - w * 0.3, h * 0.27)]
    pol(im, punta, (255, 200, 80, 255) if verde else (150, 210, 90, 255), ORO if verde else VERDE, ang=0, borde=None)
    pol(im, cuerpo, (0, 0, 0, 0), borde=TINTA, g=LW)
    ImageDraw.Draw(im).line(curva((cx - w * 0.08, h * 0.2), (cx - w * 0.16, h * 0.5), (cx - w * 0.04, h * 0.85), 12),
                            fill=(255, 255, 255, 150), width=max(2, SS * 2))
    eli(im, [cx - w * 0.09, h * 0.91, cx + w * 0.09, h * 0.985], MADERA_CLARA, MADERA, g=max(2, SS))
    return im


def simbolo(palo, s, grande=False, variante=0):
    if palo == "OROS":
        return moneda(s, grande)
    if palo == "COPAS":
        return copa(s, grande)
    if palo == "ESPADAS":
        return espada(s, grande)
    return basto(s, grande) if grande else porra(s, variante % 2 == 0)


# ---------------------------------------------------------------- carton, marco, numeros

def carton():
    base = capa(CW, CH)
    radio = int(CH * 0.047)
    rect(base, [0, 0, CW - 1, CH - 1], PAPEL, (243, 238, 226, 255), borde=None, radio=radio)
    # grano del papel
    rng = np.random.default_rng(7)
    ruido = rng.normal(0, 7, (CH, CW)).astype(np.float32)
    ruido = np.array(Image.fromarray(np.clip(ruido + 128, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.2)), np.float32) - 128
    a = np.array(base, np.float32)
    for k in range(3):
        a[..., k] = np.clip(a[..., k] + ruido * 0.9, 0, 255)
    base = Image.fromarray(a.astype(np.uint8), "RGBA")
    # borde exterior negro fino
    ImageDraw.Draw(base).rounded_rectangle([SS, SS, CW - 1 - SS, CH - 1 - SS], radius=radio, outline=TINTA, width=SS * 2)
    return base


def marco(base, palo):
    d = ImageDraw.Draw(base)
    m = int(CW * 0.085)
    color = COLOR_PALO[palo]
    d.rounded_rectangle([m, m, CW - m, CH - m], radius=int(CW * 0.03), outline=color, width=SS * 3)
    d.rounded_rectangle([m + SS * 6, m + SS * 6, CW - m - SS * 6, CH - m - SS * 6], radius=int(CW * 0.025), outline=color, width=SS)
    cortes = CORTES[palo]
    hueco = CW * 0.055
    for i in range(cortes):
        x = CW / 2 + (i - (cortes - 1) / 2) * CW * 0.13
        for y in (m, CH - m):
            d.rectangle([x - hueco / 2, y - SS * 7, x + hueco / 2, y + SS * 9], fill=PAPEL)


def numeros(base, numero, palo):
    capa_n = capa(CW, CH)
    d = ImageDraw.Draw(capa_n)
    f = ImageFont.truetype(FUENTE, int(CH * 0.05))
    color = COLOR_PALO[palo]
    m = CW * 0.085
    d.text((m + SS * 9, m + SS * 2), str(numero), font=f, fill=color, stroke_width=SS * 2, stroke_fill=PAPEL)
    base.alpha_composite(capa_n)
    base.alpha_composite(capa_n.rotate(180))


# ---------------------------------------------------------------- cartas de numero

REJILLA = {
    2: [(0.5, 0.2), (0.5, 0.8)],
    3: [(0.5, 0.16), (0.5, 0.5), (0.5, 0.84)],
    4: [(0.3, 0.22), (0.7, 0.22), (0.3, 0.78), (0.7, 0.78)],
    5: [(0.3, 0.18), (0.7, 0.18), (0.5, 0.5), (0.3, 0.82), (0.7, 0.82)],
    6: [(0.3, 0.16), (0.7, 0.16), (0.3, 0.5), (0.7, 0.5), (0.3, 0.84), (0.7, 0.84)],
    7: [(0.3, 0.11), (0.7, 0.11), (0.3, 0.37), (0.7, 0.37), (0.5, 0.63), (0.3, 0.89), (0.7, 0.89)],
}


def numerada(base, numero, palo):
    x0, y0, an, al = CW * 0.13, CH * 0.12, CW * 0.74, CH * 0.76
    if palo in ("OROS", "COPAS"):
        tam = {2: 0.46, 3: 0.38, 4: 0.36, 5: 0.33, 6: 0.31, 7: 0.25}[numero]
        for (px, py) in REJILLA[numero]:
            pegar(base, simbolo(palo, CW * tam), x0 + px * an, y0 + py * al)
        return
    # espadas y bastos: el 2 y el 3 cruzados en aspa; del 4 al 7 en columnas, algo inclinados
    if numero <= 3:
        largo = CH * 0.74
        for i, lado in enumerate((-1, 1)):
            pegar(base, simbolo(palo, largo, variante=i), CW / 2, CH / 2, giro=lado * 24)
        if numero == 3:
            pegar(base, simbolo(palo, largo * 0.98, variante=2), CW / 2, CH / 2)
        return
    filas = {4: 2, 5: 2, 6: 3, 7: 3}[numero]
    largo = CH * (0.36 if filas == 2 else 0.26)
    columnas_x = (0.31, 0.69)
    for f in range(filas):
        y = y0 + al * ((f + 0.5) / filas)
        for k, px in enumerate(columnas_x):
            giro = (8 if k == 0 else -8) if palo == "BASTOS" else 0
            pegar(base, simbolo(palo, largo, variante=f + k), x0 + px * an, y, giro=giro)
    if numero in (5, 7):
        pegar(base, simbolo(palo, largo * 0.9, variante=1), CW / 2, CH / 2)


def as_carta(base, palo):
    if palo == "OROS":
        # laurel alrededor
        cx, cy, r = CW / 2, CH * 0.47, CW * 0.36
        for i in range(30):
            a = math.pi * 0.62 + i * (math.pi * 1.76 / 29)
            for lado in (-1, 1):
                aa = a if lado > 0 else math.pi - a
                x, y = cx + (r + CW * 0.035) * math.cos(aa), cy + (r + CW * 0.035) * math.sin(aa)
                hoja = [(x, y - SS * 9), (x + SS * 7, y), (x, y + SS * 9), (x - SS * 7, y)]
                pol(base, hoja, VERDE_CLARO, VERDE_OSC, borde=VERDE_OSC, g=SS)
        pegar(base, moneda(int(CW * 0.7), True), cx, cy)
        cinta(base, CH * 0.8, "MUS TINKO")
    elif palo == "COPAS":
        pegar(base, copa(int(CH * 0.66), True), CW / 2, CH / 2)
    elif palo == "ESPADAS":
        pegar(base, espada(int(CH * 0.78), True), CW / 2, CH / 2)
        cinta(base, CH * 0.36, "MUS")
    else:
        pegar(base, basto(int(CH * 0.76), True), CW / 2, CH / 2)


def cinta(base, y, texto):
    w = CW * 0.62
    x0 = (CW - w) / 2
    pts = [(x0, y - CH * 0.03), (x0 + w, y - CH * 0.03), (x0 + w - CW * 0.04, y), (x0 + w, y + CH * 0.03), (x0, y + CH * 0.03), (x0 + CW * 0.04, y)]
    pol(base, pts, ROJO_CLARO, ROJO_OSC)
    f = ImageFont.truetype(FUENTE, int(CH * 0.034))
    ImageDraw.Draw(base).text((CW / 2, y), texto, font=f, fill=ORO_CLARO, anchor="mm", stroke_width=SS, stroke_fill=ROJO_OSC)


# ---------------------------------------------------------------- figuras

TRAJES = {   # (ropa principal, segunda, adorno)
    "OROS": (ROJO, AZUL, ORO),
    "COPAS": (AZUL, ROJO, ORO),
    "ESPADAS": (VERDE, ROJO, ORO),
    "BASTOS": (ORO, VERDE, ROJO),
}


def oscuro(c, f=0.62):
    return (int(c[0] * f), int(c[1] * f), int(c[2] * f), 255)


def claro(c, f=0.45):
    return (int(c[0] + (255 - c[0]) * f), int(c[1] + (255 - c[1]) * f), int(c[2] + (255 - c[2]) * f), 255)


def rombos(base, dibujar, color, paso):
    """Dibujo de rombos dentro de una forma (tela estampada)."""
    m = Image.new("L", base.size, 0)
    dibujar(ImageDraw.Draw(m))
    c = capa(*base.size)
    d = ImageDraw.Draw(c)
    for i in range(-60, 60):
        d.line([(i * paso, 0), (i * paso + CH, CH)], fill=color, width=SS)
        d.line([(i * paso, CH), (i * paso + CH, 0)], fill=color, width=SS)
    c.putalpha(ImageChops.multiply(m, c.getchannel("A")))
    base.alpha_composite(c)


def cara(base, cx, cy, r, barba=None, bigote=False):
    eli(base, [cx - r * 0.9, cy - r, cx + r * 0.9, cy + r], PIEL, PIEL_OSC, radial=True, centro=(0.4, 0.35), g=LW)
    d = ImageDraw.Draw(base)
    for lado in (-1, 1):                               # ojos
        x = cx + lado * r * 0.36
        d.ellipse([x - r * 0.16, cy - r * 0.2, x + r * 0.16, cy + r * 0.02], fill=BLANCO, outline=TINTA, width=SS)
        d.ellipse([x - r * 0.07, cy - r * 0.15, x + r * 0.07, cy - r * 0.01], fill=TINTA)
        d.arc([x - r * 0.22, cy - r * 0.42, x + r * 0.22, cy - r * 0.12], 200, 340, fill=NEGRO_PELO, width=SS * 2)
    d.line([(cx, cy - r * 0.05), (cx - r * 0.1, cy + r * 0.3), (cx + r * 0.05, cy + r * 0.32)], fill=PIEL_OSC, width=SS * 2)
    d.arc([cx - r * 0.28, cy + r * 0.3, cx + r * 0.28, cy + r * 0.62], 20, 160, fill=ROJO_OSC, width=SS * 2)
    for lado in (-1, 1):                               # mofletes
        x = cx + lado * r * 0.55
        c = capa(*base.size)
        ImageDraw.Draw(c).ellipse([x - r * 0.18, cy + r * 0.1, x + r * 0.18, cy + r * 0.34], fill=(230, 110, 100, 90))
        base.alpha_composite(c)
    if barba:
        pts = [(cx - r * 0.85, cy + r * 0.05)] + curva((cx - r * 0.85, cy + r * 0.05), (cx - r * 0.6, cy + r * 1.6), (cx, cy + r * 1.75))[1:] + \
            curva((cx, cy + r * 1.75), (cx + r * 0.6, cy + r * 1.6), (cx + r * 0.85, cy + r * 0.05))[1:] + \
            [(cx + r * 0.5, cy + r * 0.25), (cx + r * 0.3, cy + r * 0.7), (cx - r * 0.3, cy + r * 0.7), (cx - r * 0.5, cy + r * 0.25)]
        pol(base, pts, claro(barba, 0.2), barba, borde=TINTA, g=SS * 2)
    if bigote:
        for lado in (-1, 1):
            pts = curva((cx, cy + r * 0.32), (cx + lado * r * 0.35, cy + r * 0.2), (cx + lado * r * 0.62, cy + r * 0.42), 10) + \
                curva((cx + lado * r * 0.62, cy + r * 0.42), (cx + lado * r * 0.3, cy + r * 0.38), (cx, cy + r * 0.42), 10)[1:]
            pol(base, pts, barba or NEGRO_PELO, borde=TINTA, g=SS)


def persona(base, caja, rey, palo, sentado_y=None):
    """Sota o rey de pie, de cuerpo entero, con su palo en la mano."""
    x0, y0, x1, y1 = caja
    w, h = x1 - x0, y1 - y0
    cx = x0 + w * 0.5
    ropa, ropa2, adorno = TRAJES[palo]
    hy = y0 + h * 0.15
    r = w * 0.12

    if rey:
        # capa larga
        capa_pts = [(cx - w * 0.32, y0 + h * 0.97), (cx - w * 0.2, y0 + h * 0.28), (cx + w * 0.2, y0 + h * 0.28), (cx + w * 0.32, y0 + h * 0.97)]
        pol(base, capa_pts, oscuro(ropa2, 0.8), ropa2, ang=0, g=LW)
        # tunica larga
        tunica = [(cx - w * 0.22, y0 + h * 0.96), (cx - w * 0.17, y0 + h * 0.3), (cx + w * 0.17, y0 + h * 0.3), (cx + w * 0.22, y0 + h * 0.96)]
        pol(base, tunica, claro(ropa, 0.15), oscuro(ropa, 0.75), ang=0, g=LW)
        rombos(base, lambda d: d.polygon(tunica, fill=255), claro(adorno, 0.1), SS * 26)
        rect(base, [cx - w * 0.04, y0 + h * 0.3, cx + w * 0.04, y0 + h * 0.96], adorno, oscuro(adorno), ang=0, g=SS)
        # cuello de armino
        armino = [(cx - w * 0.26, y0 + h * 0.31), (cx - w * 0.12, y0 + h * 0.25), (cx + w * 0.12, y0 + h * 0.25), (cx + w * 0.26, y0 + h * 0.31),
                  (cx + w * 0.18, y0 + h * 0.37), (cx - w * 0.18, y0 + h * 0.37)]
        pol(base, armino, BLANCO, GRIS, g=SS * 2)
        d = ImageDraw.Draw(base)
        for k in range(7):
            x = cx - w * 0.18 + k * w * 0.06
            d.polygon([(x, y0 + h * 0.3), (x + SS * 4, y0 + h * 0.33), (x - SS * 4, y0 + h * 0.33)], fill=TINTA)
        # zapatos
        for lado in (-1, 1):
            eli(base, [cx + lado * w * 0.12 - w * 0.08, y0 + h * 0.94, cx + lado * w * 0.12 + w * 0.08, y0 + h * 0.99], oscuro(MADERA), MADERA, g=SS)
    else:
        # piernas con calzas
        for lado, color in ((-1, ropa2), (1, adorno)):
            pierna = [(cx + lado * w * 0.03, y0 + h * 0.6), (cx + lado * w * 0.13, y0 + h * 0.6), (cx + lado * w * 0.12, y0 + h * 0.93), (cx + lado * w * 0.05, y0 + h * 0.93)]
            pol(base, pierna, claro(color, 0.2), oscuro(color, 0.8), ang=0, g=LW)
            eli(base, [cx + lado * w * 0.085 - w * 0.08, y0 + h * 0.92, cx + lado * w * 0.085 + w * 0.08, y0 + h * 0.975], oscuro(MADERA), MADERA, g=SS)
        # sayo (tunica corta)
        tunica = [(cx - w * 0.25, y0 + h * 0.64), (cx - w * 0.17, y0 + h * 0.28), (cx + w * 0.17, y0 + h * 0.28), (cx + w * 0.25, y0 + h * 0.64)]
        pol(base, tunica, claro(ropa, 0.15), oscuro(ropa, 0.78), ang=0, g=LW)
        rombos(base, lambda d: d.polygon(tunica, fill=255), claro(adorno, 0.15), SS * 22)
        d = ImageDraw.Draw(base)
        for k in range(9):                              # faldon con picos
            x = cx - w * 0.25 + k * w * 0.0625
            d.polygon([(x, y0 + h * 0.62), (x + w * 0.031, y0 + h * 0.67), (x + w * 0.0625, y0 + h * 0.62)], fill=adorno, outline=TINTA)
        rect(base, [cx - w * 0.21, y0 + h * 0.47, cx + w * 0.21, y0 + h * 0.5], MADERA, MADERA_OSC, g=SS)   # cinturon
        rect(base, [cx - w * 0.03, y0 + h * 0.465, cx + w * 0.03, y0 + h * 0.505], ORO_CLARO, ORO_OSC, g=SS)
        pol(base, [(cx - w * 0.17, y0 + h * 0.28), (cx, y0 + h * 0.34), (cx + w * 0.17, y0 + h * 0.28), (cx, y0 + h * 0.26)], BLANCO, GRIS, g=SS)

    # brazos: el derecho (a la izquierda de la carta) sujeta el palo
    brazo_izq = [(cx - w * 0.17, y0 + h * 0.3), (cx - w * 0.3, y0 + h * 0.42), (cx - w * 0.32, y0 + h * 0.5), (cx - w * 0.24, y0 + h * 0.5), (cx - w * 0.15, y0 + h * 0.38)]
    pol(base, brazo_izq, claro(ropa2, 0.2), oscuro(ropa2, 0.8), ang=0, g=LW)
    eli(base, [cx - w * 0.34, y0 + h * 0.48, cx - w * 0.22, y0 + h * 0.54], PIEL, PIEL_OSC, g=SS)
    brazo_der = [(cx + w * 0.17, y0 + h * 0.3), (cx + w * 0.28, y0 + h * 0.36), (cx + w * 0.3, y0 + h * 0.46), (cx + w * 0.22, y0 + h * 0.46), (cx + w * 0.15, y0 + h * 0.38)]
    pol(base, brazo_der, claro(ropa2, 0.2), oscuro(ropa2, 0.8), ang=0, g=LW)
    mano = (cx + w * 0.27, y0 + h * 0.47)
    eli(base, [mano[0] - w * 0.06, mano[1] - h * 0.025, mano[0] + w * 0.06, mano[1] + h * 0.025], PIEL, PIEL_OSC, g=SS)

    # cabeza
    pol(base, [(cx - w * 0.05, hy + r * 0.8), (cx + w * 0.05, hy + r * 0.8), (cx + w * 0.05, y0 + h * 0.29), (cx - w * 0.05, y0 + h * 0.29)], PIEL, PIEL_OSC, g=SS)
    if rey:
        # melena
        eli(base, [cx - r * 1.05, hy - r * 0.95, cx + r * 1.05, hy + r * 0.9], NEGRO_PELO, oscuro(NEGRO_PELO), g=LW)
    else:
        eli(base, [cx - r * 1.0, hy - r * 1.05, cx + r * 1.0, hy + r * 0.4], oscuro(MADERA), MADERA_OSC, g=LW)
    cara(base, cx, hy, r, barba=(120, 78, 40, 255) if rey else None, bigote=rey)
    if rey:
        # corona
        by = hy - r * 0.75
        corona = [(cx - r * 0.95, by), (cx - r * 1.05, by - r * 0.85), (cx - r * 0.55, by - r * 0.45), (cx, by - r * 1.05),
                  (cx + r * 0.55, by - r * 0.45), (cx + r * 1.05, by - r * 0.85), (cx + r * 0.95, by)]
        pol(base, corona, ORO_CLARO, ORO_OSC)
        for k, xx in enumerate((-0.55, 0, 0.55)):
            rr = r * 0.13
            eli(base, [cx + r * xx - rr, by - r * 0.3 - rr, cx + r * xx + rr, by - r * 0.3 + rr],
                ROJO_CLARO if k != 1 else AZUL_CLARO, ROJO_OSC if k != 1 else AZUL_OSC, radial=True, g=SS)
    else:
        # gorra con pluma
        gorra = [(cx - r * 1.15, hy - r * 0.55), (cx - r * 0.9, hy - r * 1.25), (cx + r * 0.8, hy - r * 1.3), (cx + r * 1.2, hy - r * 0.6)]
        pol(base, gorra, ropa2, oscuro(ropa2, 0.7))
        pluma = curva((cx + r * 0.6, hy - r * 1.2), (cx + r * 2.0, hy - r * 2.4), (cx + r * 2.4, hy - r * 1.1), 14) + \
            curva((cx + r * 2.4, hy - r * 1.1), (cx + r * 1.6, hy - r * 1.6), (cx + r * 0.7, hy - r * 0.95), 14)[1:]
        pol(base, pluma, BLANCO, claro(adorno, 0.3), g=SS)

    # el palo en la mano
    if palo == "OROS":
        pegar(base, moneda(int(w * 0.36)), mano[0] - w * 0.02, mano[1] - h * 0.08)
    elif palo == "COPAS":
        pegar(base, copa(int(w * 0.38)), mano[0], mano[1] - h * 0.09)
    elif palo == "ESPADAS":
        pegar(base, espada(int(h * 0.62)), mano[0] + w * 0.04, mano[1] - h * 0.18, giro=-8)
    else:
        pegar(base, basto(int(h * 0.55)), mano[0] + w * 0.05, mano[1] - h * 0.12, giro=-14)


def caballo(base, caja, palo):
    x0, y0, x1, y1 = caja
    w, h = x1 - x0, y1 - y0
    ropa, ropa2, adorno = TRAJES[palo]
    pelo = {"OROS": (236, 230, 218, 255), "COPAS": (150, 96, 54, 255), "ESPADAS": (88, 64, 52, 255), "BASTOS": (196, 150, 96, 255)}[palo]
    crin = oscuro(pelo, 0.5) if palo != "ESPADAS" else (30, 22, 18, 255)
    P = lambda x, y: (x0 + x * w, y0 + y * h)

    def pata(cadera, rodilla, casco, ancho, color):
        (ax, ay), (bx, by), (cx_, cy_) = cadera, rodilla, casco
        pts = [P(ax - ancho, ay), P(ax + ancho, ay), P(bx + ancho * 0.6, by), P(cx_ + ancho * 0.45, cy_), P(cx_ - ancho * 0.45, cy_), P(bx - ancho * 0.6, by)]
        pol(base, pts, claro(color, 0.1), oscuro(color, 0.8), ang=0, g=LW)
        rect(base, [P(cx_ - ancho * 0.7, cy_ - 0.005)[0], P(0, cy_ - 0.005)[1], P(cx_ + ancho * 0.8, 0)[0], P(0, cy_ + 0.03)[1]], TINTA, (70, 60, 52, 255), g=SS)

    # patas de detras (mas oscuras) y de delante
    pata((0.33, 0.6), (0.29, 0.76), (0.31, 0.92), 0.048, oscuro(pelo, 0.82))
    pata((0.72, 0.6), (0.74, 0.77), (0.73, 0.92), 0.042, oscuro(pelo, 0.82))
    # cola
    cola = curva(P(0.24, 0.47), P(0.06, 0.55), P(0.1, 0.84), 14) + curva(P(0.1, 0.84), P(0.15, 0.62), P(0.27, 0.53), 14)[1:]
    pol(base, cola, crin, oscuro(crin, 0.7))
    # cuerpo con pecho y grupa redondos
    cuerpo = curva(P(0.66, 0.42), P(0.45, 0.38), P(0.27, 0.44), 14) + curva(P(0.27, 0.44), P(0.16, 0.52), P(0.27, 0.64), 12)[1:] + \
        curva(P(0.27, 0.64), P(0.52, 0.7), P(0.78, 0.63), 14)[1:] + curva(P(0.78, 0.63), P(0.9, 0.56), P(0.86, 0.44), 10)[1:]
    pol(base, cuerpo, claro(pelo, 0.15), oscuro(pelo, 0.82))
    # cuello arqueado y cabeza con hocico
    cuello = curva(P(0.66, 0.44), P(0.7, 0.26), P(0.79, 0.17), 12) + [P(0.85, 0.16)] + \
        curva(P(0.88, 0.27), P(0.86, 0.4), P(0.88, 0.5), 10)
    pol(base, cuello, claro(pelo, 0.1), oscuro(pelo, 0.85), ang=0)
    cabeza = [P(0.79, 0.17), P(0.84, 0.13), P(0.88, 0.15)] + curva(P(0.88, 0.15), P(0.95, 0.24), P(0.98, 0.33), 8)[1:] + \
        curva(P(0.98, 0.33), P(0.97, 0.38), P(0.92, 0.37), 6)[1:] + [P(0.86, 0.3)]
    pol(base, cabeza, claro(pelo, 0.12), oscuro(pelo, 0.85), ang=0)
    d = ImageDraw.Draw(base)
    ox, oy = P(0.865, 0.2)
    d.ellipse([ox - w * 0.012, oy - w * 0.009, ox + w * 0.012, oy + w * 0.009], fill=TINTA)
    nx, ny = P(0.955, 0.335)
    d.ellipse([nx - w * 0.008, ny - w * 0.006, nx + w * 0.008, ny + w * 0.006], fill=TINTA)
    d.polygon([P(0.82, 0.15), P(0.83, 0.08), P(0.85, 0.14)], fill=pelo, outline=TINTA)
    for k in range(7):                                  # crin
        x, y = P(0.81 - k * 0.022, 0.15 + k * 0.042)
        pol(base, [(x, y), (x - w * 0.06, y + h * 0.035), (x - w * 0.004, y + h * 0.05)], crin, borde=TINTA, g=SS)
    # patas de delante (encima del cuerpo)
    pata((0.38, 0.6), (0.35, 0.77), (0.38, 0.92), 0.048, pelo)
    pata((0.78, 0.58), (0.8, 0.76), (0.79, 0.92), 0.042, pelo)
    # riendas y manta
    d.line([P(0.95, 0.35), P(0.84, 0.3), P(0.62, 0.4)], fill=ROJO_OSC, width=SS * 2)
    manta = [P(0.4, 0.4), P(0.65, 0.4), P(0.67, 0.6), P(0.38, 0.6)]
    pol(base, manta, claro(ropa2, 0.15), oscuro(ropa2, 0.8))
    rombos(base, lambda dd: dd.polygon(manta, fill=255), claro(adorno, 0.2), SS * 18)
    for k in range(10):
        x, y = P(0.39 + k * 0.029, 0.6)
        d.line([(x, y), (x, y + h * 0.03)], fill=adorno, width=SS * 2)

    # jinete
    jx = x0 + w * 0.53
    pierna = [(jx - w * 0.02, y0 + h * 0.4), (jx + w * 0.07, y0 + h * 0.4), (jx + w * 0.06, y0 + h * 0.62), (jx - w * 0.01, y0 + h * 0.62)]
    pol(base, pierna, claro(adorno, 0.2), oscuro(adorno, 0.8), ang=0)
    eli(base, [jx - w * 0.03, y0 + h * 0.6, jx + w * 0.09, y0 + h * 0.64], oscuro(MADERA), MADERA, g=SS)
    torso = [(jx - w * 0.09, y0 + h * 0.42), (jx - w * 0.07, y0 + h * 0.22), (jx + w * 0.07, y0 + h * 0.22), (jx + w * 0.1, y0 + h * 0.42)]
    pol(base, torso, claro(ropa, 0.15), oscuro(ropa, 0.78), ang=0)
    rombos(base, lambda dd: dd.polygon(torso, fill=255), claro(adorno, 0.15), SS * 18)
    brazo = [(jx + w * 0.06, y0 + h * 0.26), (jx + w * 0.17, y0 + h * 0.3), (jx + w * 0.18, y0 + h * 0.35), (jx + w * 0.12, y0 + h * 0.35), (jx + w * 0.05, y0 + h * 0.31)]
    pol(base, brazo, claro(ropa2, 0.2), oscuro(ropa2, 0.8), ang=0)
    mano = (jx + w * 0.17, y0 + h * 0.355)
    eli(base, [mano[0] - w * 0.035, mano[1] - h * 0.018, mano[0] + w * 0.035, mano[1] + h * 0.018], PIEL, PIEL_OSC, g=SS)
    r = w * 0.075
    hy = y0 + h * 0.18
    eli(base, [jx - r * 0.95, hy - r * 1.0, jx + r * 0.95, hy + r * 0.5], NEGRO_PELO, oscuro(NEGRO_PELO), g=SS)
    cara(base, jx, hy, r, bigote=True, barba=None)
    sombrero = [(jx - r * 1.6, hy - r * 0.55), (jx + r * 1.6, hy - r * 0.55), (jx + r * 0.9, hy - r * 0.8), (jx + r * 0.7, hy - r * 1.6), (jx - r * 0.7, hy - r * 1.6), (jx - r * 0.9, hy - r * 0.8)]
    pol(base, sombrero, ropa2, oscuro(ropa2, 0.6))
    pluma = curva((jx + r * 0.5, hy - r * 1.5), (jx + r * 2.2, hy - r * 2.5), (jx + r * 2.5, hy - r * 1.2), 12) + \
        curva((jx + r * 2.5, hy - r * 1.2), (jx + r * 1.5, hy - r * 1.7), (jx + r * 0.6, hy - r * 1.2), 12)[1:]
    pol(base, pluma, BLANCO, claro(adorno, 0.3), g=SS)
    if palo == "OROS":
        pegar(base, moneda(int(w * 0.22)), mano[0] + w * 0.04, mano[1] - h * 0.06)
    elif palo == "COPAS":
        pegar(base, copa(int(w * 0.24)), mano[0] + w * 0.02, mano[1] - h * 0.07)
    elif palo == "ESPADAS":
        pegar(base, espada(int(h * 0.42)), mano[0] + w * 0.03, mano[1] - h * 0.13, giro=-12)
    else:
        pegar(base, basto(int(h * 0.38)), mano[0] + w * 0.04, mano[1] - h * 0.1, giro=-18)


def figura(base, numero, palo):
    caja = (CW * 0.13, CH * 0.11, CW * 0.87, CH * 0.89)
    # fondo suave del recuadro
    # como en la baraja de verdad: la figura sobre el carton blanco
    # suelo
    rect(base, [caja[0] + SS * 3, caja[1] + (caja[3] - caja[1]) * 0.93, caja[2] - SS * 3, caja[3] - SS * 3],
         claro(VERDE, 0.55), claro(VERDE, 0.35), borde=None, radio=CW * 0.02)
    if numero == 11:
        caballo(base, caja, palo)
    else:
        persona(base, caja, numero == 12, palo)


# ---------------------------------------------------------------- montaje

def carta(numero, palo):
    base = carton()
    marco(base, palo)
    if numero == 1:
        as_carta(base, palo)
    elif numero <= 7:
        numerada(base, numero, palo)
    else:
        figura(base, numero, palo)
    numeros(base, numero, palo)
    # recorte con las esquinas redondeadas
    m = Image.new("L", (CW, CH), 0)
    ImageDraw.Draw(m).rounded_rectangle([0, 0, CW - 1, CH - 1], radius=int(CH * 0.047), fill=255)
    base.putalpha(ImageChops.multiply(base.getchannel("A"), m))
    return base.resize((W, H), Image.LANCZOS)


def hoja_completa():
    hoja = Image.new("RGBA", (W * 10 + 8, H * 4), (0, 0, 0, 0))
    for fila, palo in enumerate(PALOS):
        for col, numero in enumerate(NUMEROS):
            hoja.alpha_composite(carta(numero, palo), (col * W, fila * H))
            print("  ", palo, numero, flush=True)
    return hoja


def main():
    if len(sys.argv) > 2 and sys.argv[1] == "muestra":
        cartas = [(1, "OROS"), (5, "COPAS"), (4, "ESPADAS"), (7, "BASTOS"), (10, "OROS"), (11, "COPAS"), (12, "ESPADAS"), (12, "BASTOS")]
        m = Image.new("RGBA", (W * 4, H * 2), (60, 44, 30, 255))
        for i, (n, p) in enumerate(cartas):
            m.alpha_composite(carta(n, p), ((i % 4) * W, (i // 4) * H))
        m.save(sys.argv[2])
        print("Muestra:", sys.argv[2])
        return

    hoja = hoja_completa()
    ruta = os.path.join(DESTINO, NOMBRE + ".png")
    hoja.save(ruta, optimize=True)
    print("Generado:", ruta, hoja.size)


if __name__ == "__main__":
    main()
