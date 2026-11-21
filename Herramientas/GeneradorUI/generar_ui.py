"""Genera los graficos de la interfaz de Mus TINKO (estilo taberna).

Uso: python3 generar_ui.py
Salida: Assets/Resources/UI_Tinko/*.png  (se cargan en tiempo de ejecucion como sprites 9-slice)
"""
import math
import os
import random

from PIL import Image, ImageDraw, ImageFilter

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DESTINO = os.path.join(RAIZ, "Assets", "Resources", "UI_Tinko")
SS = 2


def guardar(img, nombre):
    os.makedirs(DESTINO, exist_ok=True)
    img.save(os.path.join(DESTINO, nombre + ".png"), optimize=True)


def redondeado(w, h, r, color):
    im = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    ImageDraw.Draw(im).rounded_rectangle([0, 0, w - 1, h - 1], radius=r, fill=color)
    return im


def mascara(w, h, r, margen=0):
    m = Image.new("L", (w, h), 0)
    ImageDraw.Draw(m).rounded_rectangle([margen, margen, w - 1 - margen, h - 1 - margen], radius=max(1, r - margen), fill=255)
    return m


def vetas(w, h, base, oscuro, claro, semilla):
    rnd = random.Random(semilla)
    im = Image.new("RGBA", (w, h), base)
    capa = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    d = ImageDraw.Draw(capa)
    y = 0
    while y < h:
        alto = rnd.randint(3 * SS, 9 * SS)
        col = oscuro if rnd.random() < 0.5 else claro
        alfa = rnd.randint(25, 70)
        for x in range(0, w, 2):
            ondula = math.sin(x / (40.0 * SS) + y) * 2 * SS
            d.line([(x, y + ondula), (x + 2, y + ondula)], fill=col[:3] + (alfa,), width=max(1, alto // 3))
        y += alto
    for _ in range(max(1, w * h // (9000 * SS * SS))):
        cx, cy = rnd.randint(0, w), rnd.randint(0, h)
        rx, ry = rnd.randint(4, 9) * SS, rnd.randint(2, 4) * SS
        d.ellipse([cx - rx, cy - ry, cx + rx, cy + ry], outline=oscuro[:3] + (90,), width=SS)
    im.alpha_composite(capa.filter(ImageFilter.GaussianBlur(0.6 * SS)))
    return im


def panel_madera(nombre, w, h, r, base, semilla):
    W, H, R = w * SS, h * SS, r * SS
    borde = tuple(int(c * 0.45) for c in base[:3]) + (255,)
    oscuro = tuple(int(c * 0.7) for c in base[:3]) + (255,)
    claro = tuple(min(255, int(c * 1.25)) for c in base[:3]) + (255,)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    sombra = redondeado(W, H, R, (0, 0, 0, 110)).filter(ImageFilter.GaussianBlur(3 * SS))
    im.alpha_composite(sombra, (0, 2 * SS))
    im.paste(redondeado(W, H, R, borde), (0, 0), mascara(W, H, R))
    madera = vetas(W, H, base, oscuro, claro, semilla)
    im.paste(madera, (0, 0), mascara(W, H, R, 4 * SS))
    lineas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    dl = ImageDraw.Draw(lineas)
    dl.rounded_rectangle([4 * SS, 4 * SS, W - 1 - 4 * SS, H - 1 - 4 * SS], radius=R - 4 * SS, outline=claro[:3] + (120,), width=SS)
    dl.rounded_rectangle([7 * SS, 7 * SS, W - 1 - 7 * SS, H - 1 - 7 * SS], radius=R - 7 * SS, outline=borde[:3] + (140,), width=SS)
    im.alpha_composite(lineas)
    d = ImageDraw.Draw(im)
    for cx, cy in ((10, 10), (w - 10, 10), (10, h - 10), (w - 10, h - 10)):
        x, y = cx * SS, cy * SS
        d.ellipse([x - 3 * SS, y - 3 * SS, x + 3 * SS, y + 3 * SS], fill=(70, 55, 40, 255), outline=(30, 22, 15, 255), width=SS)
        d.ellipse([x - 1.5 * SS, y - 2 * SS, x, y - 0.5 * SS], fill=(190, 170, 140, 200))
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def pergamino(nombre, w, h, r):
    W, H, R = w * SS, h * SS, r * SS
    rnd = random.Random(7)
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    im.alpha_composite(redondeado(W, H, R, (0, 0, 0, 90)).filter(ImageFilter.GaussianBlur(3 * SS)), (0, 2 * SS))
    papel = Image.new("RGBA", (W, H), (240, 226, 190, 255))
    manchas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(manchas)
    for _ in range(260):
        x, y = rnd.randint(0, W), rnd.randint(0, H)
        rr = rnd.randint(4, 20) * SS
        d.ellipse([x - rr, y - rr, x + rr, y + rr], fill=(200, 175, 120, rnd.randint(6, 18)))
    papel.alpha_composite(manchas.filter(ImageFilter.GaussianBlur(4 * SS)))
    borde = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(borde).rounded_rectangle([0, 0, W - 1, H - 1], radius=R, outline=(150, 105, 55, 200), width=10 * SS)
    papel.alpha_composite(borde.filter(ImageFilter.GaussianBlur(5 * SS)))
    im.paste(papel, (0, 0), mascara(W, H, R))
    ImageDraw.Draw(im).rounded_rectangle([1, 1, W - 2, H - 2], radius=R, outline=(110, 75, 40, 255), width=2 * SS)
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def boton(nombre, w, h, r, color, pulsado=False):
    W, H, R = w * SS, h * SS, r * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    hundido = 3 * SS if pulsado else 0
    base_alto = H - 5 * SS
    oscuro = tuple(int(c * 0.55) for c in color) + (255,)
    im.paste(redondeado(W, base_alto, R, oscuro), (0, 5 * SS), mascara(W, base_alto, R))
    cara = Image.new("RGBA", (W, base_alto), (0, 0, 0, 0))
    dc = ImageDraw.Draw(cara)
    for y in range(base_alto):
        k = y / base_alto
        col = tuple(int(c * (1.12 - 0.3 * k)) if c * (1.12 - 0.3 * k) < 255 else 255 for c in color)
        dc.line([(0, y), (W, y)], fill=col + (255,))
    im.paste(cara, (0, hundido), mascara(W, base_alto, R))
    brillo = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    ImageDraw.Draw(brillo).rounded_rectangle([5 * SS, hundido + 4 * SS, W - 1 - 5 * SS, hundido + base_alto * 0.45], radius=R - 5 * SS, fill=(255, 255, 255, 55))
    im.alpha_composite(brillo.filter(ImageFilter.GaussianBlur(SS)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([0, hundido, W - 1, hundido + base_alto - 1], radius=R, outline=(25, 18, 12, 255), width=2 * SS)
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def barra(nombre, w, h, color):
    W, H = w * SS, h * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for y in range(H):
        k = y / H
        col = tuple(min(255, int(c * (1.15 - 0.35 * k))) for c in color)
        d.line([(0, y), (W, y)], fill=col + (255,))
    im.putalpha(mascara(W, H, H // 2))
    ImageDraw.Draw(im).rounded_rectangle([0, 0, W - 1, H - 1], radius=H // 2, outline=(25, 18, 12, 255), width=SS)
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def vineta(nombre, w, h):
    W, H = w * SS, h * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for i in range(60):
        k = i / 60
        a = int(200 * (k ** 2.2))
        d.rectangle([k * W * 0.0, 0, W, H], fill=None)
    grad = Image.new("L", (W, H), 0)
    dg = ImageDraw.Draw(grad)
    for y in range(H):
        a = int(255 * max(0.0, (y / H - 0.3) / 0.7) ** 1.6)
        dg.line([(0, y), (W, y)], fill=a)
    negro = Image.new("RGBA", (W, H), (10, 6, 3, 255))
    negro.putalpha(grad)
    guardar(negro.resize((w, h), Image.LANCZOS), nombre)


def bocadillo(nombre, w, h, r):
    W, H, R = w * SS, h * SS, r * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    im.alpha_composite(redondeado(W - 6 * SS, H - 6 * SS, R, (0, 0, 0, 90)).filter(ImageFilter.GaussianBlur(2 * SS)), (4 * SS, 5 * SS))
    cuerpo = redondeado(W - 6 * SS, H - 6 * SS, R, (40, 26, 16, 255))
    im.alpha_composite(cuerpo, (0, 0))
    papel = redondeado(W - 6 * SS - 6 * SS, H - 6 * SS - 6 * SS, R - 3 * SS, (255, 248, 230, 255))
    brillo = Image.new("RGBA", papel.size, (0, 0, 0, 0))
    ImageDraw.Draw(brillo).rounded_rectangle([6 * SS, 4 * SS, papel.width - 6 * SS, papel.height * 0.42], radius=R - 6 * SS, fill=(255, 255, 255, 110))
    papel.alpha_composite(brillo.filter(ImageFilter.GaussianBlur(2 * SS)))
    im.alpha_composite(papel, (3 * SS, 3 * SS))
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def cola(nombre, w, h):
    W, H = w * SS, h * SS
    im = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    d.polygon([(W * 0.08, 0), (W * 0.92, 0), (W * 0.3, H)], fill=(40, 26, 16, 255))
    d.polygon([(W * 0.2, 0), (W * 0.78, 0), (W * 0.33, H - 6 * SS)], fill=(255, 248, 230, 255))
    guardar(im.resize((w, h), Image.LANCZOS), nombre)


def mesa_arriba(nombre, tam):
    T = tam * SS
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    m = Image.new("L", (T, T), 0)
    ImageDraw.Draw(m).ellipse([6 * SS, 6 * SS, T - 6 * SS, T - 6 * SS], fill=255)
    sombra = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    ImageDraw.Draw(sombra).ellipse([10 * SS, 16 * SS, T - 2 * SS, T], fill=(0, 0, 0, 110))
    im.alpha_composite(sombra.filter(ImageFilter.GaussianBlur(6 * SS)))
    madera = Image.new("RGBA", (T, T), (150, 96, 52, 255))
    d = ImageDraw.Draw(madera)
    tabla = T / 7
    for i in range(8):
        x = i * tabla
        d.line([(x, 0), (x, T)], fill=(95, 58, 30, 255), width=2 * SS)
    madera = madera.filter(ImageFilter.GaussianBlur(0.5 * SS))
    vet = vetas(T, T, (0, 0, 0, 0), (100, 62, 32, 255), (190, 130, 75, 255), 9)
    madera.alpha_composite(vet)
    im.paste(madera, (0, 0), m)
    d = ImageDraw.Draw(im)
    d.ellipse([6 * SS, 6 * SS, T - 6 * SS, T - 6 * SS], outline=(55, 34, 18, 255), width=5 * SS)
    d.ellipse([16 * SS, 16 * SS, T - 16 * SS, T - 16 * SS], outline=(200, 145, 90, 120), width=2 * SS)
    for k in range(16):
        a = 2 * math.pi * k / 16
        x, y = T / 2 + (T / 2 - 14 * SS) * math.cos(a), T / 2 + (T / 2 - 14 * SS) * math.sin(a)
        d.ellipse([x - 2.5 * SS, y - 2.5 * SS, x + 2.5 * SS, y + 2.5 * SS], fill=(60, 50, 40, 255))
    guardar(im.resize((tam, tam), Image.LANCZOS), nombre)


def silla_arriba(nombre, tam):
    T = tam * SS
    im = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    sombra = Image.new("RGBA", (T, T), (0, 0, 0, 0))
    ImageDraw.Draw(sombra).rounded_rectangle([T * 0.16, T * 0.2, T * 0.9, T * 0.96], radius=T * 0.12, fill=(0, 0, 0, 110))
    im.alpha_composite(sombra.filter(ImageFilter.GaussianBlur(4 * SS)))
    d = ImageDraw.Draw(im)
    d.rounded_rectangle([T * 0.12, T * 0.14, T * 0.88, T * 0.86], radius=T * 0.12, fill=(235, 235, 235, 255), outline=(40, 30, 22, 255), width=3 * SS)
    d.rounded_rectangle([T * 0.12, T * 0.68, T * 0.88, T * 0.88], radius=T * 0.08, fill=(200, 200, 200, 255), outline=(40, 30, 22, 255), width=3 * SS)
    d.rounded_rectangle([T * 0.22, T * 0.22, T * 0.78, T * 0.6], radius=T * 0.1, fill=(250, 250, 250, 255))
    guardar(im.resize((tam, tam), Image.LANCZOS), nombre)


def main():
    panel_madera("panel_madera", 128, 128, 18, (128, 82, 46, 255), 1)
    panel_madera("panel_madera_oscura", 128, 128, 18, (78, 50, 30, 255), 2)
    pergamino("panel_pergamino", 160, 160, 16)
    colores = {
        "verde": (70, 150, 70),
        "rojo": (180, 55, 45),
        "naranja": (220, 130, 40),
        "azul": (60, 110, 180),
        "dorado": (215, 170, 60),
        "gris": (120, 112, 104),
        "morado": (130, 90, 170),
    }
    for nombre, color in colores.items():
        boton("boton_" + nombre, 120, 56, 14, color)
        boton("boton_" + nombre + "_pulsado", 120, 56, 14, color, True)
    barra("barra_fondo", 64, 16, (40, 30, 22))
    barra("barra_nosotros", 64, 16, (80, 170, 90))
    barra("barra_ellos", 64, 16, (200, 80, 65))
    vineta("degradado_inferior", 16, 128)
    bocadillo("bocadillo", 140, 100, 30)
    cola("bocadillo_cola", 44, 30)
    mesa_arriba("mesa_arriba", 320)
    silla_arriba("silla_arriba", 120)
    print("Graficos generados en", DESTINO)


if __name__ == "__main__":
    main()
