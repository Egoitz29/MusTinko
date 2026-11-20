using System;
using System.Collections.Generic;
using System.Diagnostics;

public static class PruebasNucleoMus
{
    private static int fallos;

    private static void Comprobar(Action<string> log, bool condicion, string mensaje)
    {
        if (condicion)
        {
            log("  OK    " + mensaje);
            return;
        }

        fallos++;
        log("  FALLO " + mensaje);
    }

    private static int C(string valor, string palo) => CartasMus.Desde(valor, palo);

    public static int Ejecutar(Action<string> log)
    {
        fallos = 0;
        log("== Baraja ==");
        BarajaMus baraja = new(new Random(1));
        HashSet<int> vistas = new();
        for (int i = 0; i < 40; i++) vistas.Add(baraja.Sacar());
        Comprobar(log, vistas.Count == 40, "La baraja tiene 40 cartas distintas");
        Comprobar(log, CartasMus.Texto(C("REY", "OROS")) == "REY\nOROS", "Formato de texto compatible con RepartoPrueba");
        Comprobar(log, CartasMus.Rango(C("3", "COPAS")) == 8 && CartasMus.Rango(C("2", "COPAS")) == 1, "El 3 vale como rey y el 2 como as");

        log("== Mus corrido ==");
        PartidaMus p = new(7);
        p.IniciarPartida();
        Comprobar(log, p.Fase == FaseMus.Mus && p.JugadorActual == 0, "La partida empieza en Mus hablando el jugador 0");
        p.Aplicar(0, AccionMus.Mus);
        Comprobar(log, !p.Aplicar(0, AccionMus.Mus), "El jugador 0 no puede hablar dos veces seguidas");
        Comprobar(log, !p.Aplicar(2, AccionMus.Mus), "El jugador 2 no puede hablar fuera de turno");
        p.Aplicar(1, AccionMus.Mus);
        p.Aplicar(2, AccionMus.NoHayMus);
        Comprobar(log, p.Mano == 2 && p.Fase == FaseMus.Grande, "Quien corta el mus corrido pasa a ser mano (jugador 2)");
        Comprobar(log, p.JugadorActual == 2, "En Grande habla primero el mano");
        while (PartidaMus.EsLance(p.Fase)) p.Aplicar(p.JugadorActual, AccionMus.Pasar);
        p.SiguienteMano();
        Comprobar(log, p.Mano == 3 && !p.MusCorridoPendiente && p.JugadorActual == 3,
            "En la mano siguiente es mano el de despues (jugador 3) y empieza a hablar el");

        PartidaMus pc = new(21);
        pc.IniciarPartida();
        for (int j = 0; j < 4; j++) pc.Aplicar(j, AccionMus.Mus);
        for (int j = 0; j < 4; j++) pc.Aplicar(j, AccionMus.Descartar(1));
        Comprobar(log, pc.MusCorridoPendiente && pc.JugadorActual == 0, "Si todos dan mus en el corrido, se sigue rotando (vuelve a hablar el 0)");
        pc.Aplicar(0, AccionMus.Mus);
        pc.Aplicar(1, AccionMus.NoHayMus);
        Comprobar(log, pc.Mano == 1 && !pc.MusCorridoPendiente, "El primero que corta en la segunda vuelta (jugador 1) es mano");

        log("== Descarte ==");
        PartidaMus d = new(11);
        d.IniciarPartida();
        for (int j = 0; j < 4; j++) d.Aplicar(j, AccionMus.Mus);
        Comprobar(log, d.Fase == FaseMus.Descarte, "Cuatro MUS llevan a Descarte");
        int[] antes = d.CartasDe(0);
        d.Aplicar(0, AccionMus.Descartar(0b0011));
        int[] despues = d.CartasDe(0);
        Comprobar(log, antes[2] == despues[2] && antes[3] == despues[3] && (antes[0] != despues[0] || antes[1] != despues[1]),
            "Descartar 2 cartas sustituye solo esas posiciones");
        for (int j = 1; j < 4; j++) d.Aplicar(j, AccionMus.Descartar(0b1111));
        Comprobar(log, d.Fase == FaseMus.Mus && d.RondaMus == 2, "Tras los cuatro descartes vuelve a Mus (ronda 2)");
        Comprobar(log, SinDuplicados(d), "No hay cartas repetidas entre las manos tras descartar");

        log("== Tanteo en paso frente a TanteoEnPasoMus ==");
        int coincidencias = 0;
        int casos = 300;
        for (int s = 0; s < casos; s++)
        {
            PartidaMus t = new(1000 + s);
            t.IniciarPartida();
            t.Aplicar(0, AccionMus.NoHayMus);
            while (PartidaMus.EsLance(t.Fase))
                t.Aplicar(t.JugadorActual, AccionMus.Pasar);

            EvaluadorManoMus.Resultado[] ev = new EvaluadorManoMus.Resultado[4];
            for (int j = 0; j < 4; j++) ev[j] = t.Evaluacion(j);
            TanteoEnPasoMus.Resumen esperado = TanteoEnPasoMus.Calcular(ev, t.Mano);

            if (t.Puntos(0) == esperado.Pareja02 && t.Puntos(1) == esperado.Pareja13)
                coincidencias++;
        }
        Comprobar(log, coincidencias == casos, "Todo en paso coincide con TanteoEnPasoMus en " + coincidencias + "/" + casos + " manos");

        log("== Envites ==");
        PartidaMus e = ManoFija(
            new[] { C("REY", "OROS"), C("REY", "COPAS"), C("CABALLO", "OROS"), C("4", "OROS") },
            new[] { C("AS", "OROS"), C("AS", "COPAS"), C("5", "OROS"), C("6", "OROS") },
            new[] { C("SOTA", "OROS"), C("7", "OROS"), C("5", "COPAS"), C("6", "COPAS") },
            new[] { C("4", "COPAS"), C("7", "COPAS"), C("SOTA", "COPAS"), C("CABALLO", "COPAS") });
        e.Aplicar(0, AccionMus.Envidar(2));
        Comprobar(log, e.JugadorActual == 1, "Tras envidar el 0, responde el rival 1");
        Comprobar(log, !e.Aplicar(2, AccionMus.Quiero), "El companero del que envida no puede responder");
        e.Aplicar(1, AccionMus.NoQuiero);
        Comprobar(log, e.JugadorActual == 3 && e.Fase == FaseMus.Grande, "Un NO QUIERO deja hablar al companero (jugador 3)");
        e.Aplicar(3, AccionMus.NoQuiero);
        Comprobar(log, e.Puntos(0) == 1 && e.Fase == FaseMus.Chica, "Envite rechazado: 1 tanto inmediato para la pareja 0-2");

        PartidaMus q = ManoFija(
            new[] { C("REY", "OROS"), C("REY", "COPAS"), C("CABALLO", "OROS"), C("4", "OROS") },
            new[] { C("AS", "OROS"), C("AS", "COPAS"), C("5", "OROS"), C("6", "OROS") },
            new[] { C("SOTA", "OROS"), C("7", "OROS"), C("5", "COPAS"), C("6", "COPAS") },
            new[] { C("4", "COPAS"), C("7", "COPAS"), C("SOTA", "COPAS"), C("CABALLO", "COPAS") });
        q.Aplicar(0, AccionMus.Envidar(2));
        q.Aplicar(1, AccionMus.Reenvidar(2));
        Comprobar(log, q.JugadorActual == 2, "Tras reenvidar el 1, responde el siguiente rival (2)");
        q.Aplicar(2, AccionMus.Quiero);
        Comprobar(log, q.Fase == FaseMus.Chica, "QUIERO cierra Grande");
        while (PartidaMus.EsLance(q.Fase)) q.Aplicar(q.JugadorActual, AccionMus.Pasar);
        Comprobar(log, q.Puntos(0) >= 4, "Grande querida a 4 la cobra la pareja 0-2 (dos reyes): " + q.Puntos(0) + "-" + q.Puntos(1));

        PartidaMus o = ManoFija(
            new[] { C("REY", "OROS"), C("REY", "COPAS"), C("CABALLO", "OROS"), C("4", "OROS") },
            new[] { C("AS", "OROS"), C("AS", "COPAS"), C("5", "OROS"), C("6", "OROS") },
            new[] { C("SOTA", "OROS"), C("7", "OROS"), C("5", "COPAS"), C("6", "COPAS") },
            new[] { C("4", "COPAS"), C("7", "COPAS"), C("SOTA", "COPAS"), C("CABALLO", "COPAS") });
        o.Aplicar(0, AccionMus.Pasar);
        o.Aplicar(1, AccionMus.Ordago);
        o.Aplicar(2, AccionMus.Quiero);
        Comprobar(log, o.PartidaTerminada && o.VictoriaPorOrdago && o.ParejaGanadora == 0,
            "Ordago a Grande querido: gana la pareja con mejor Grande (0-2)");

        log("== Senales ==");
        PartidaMus sn = ManoFija(
            new[] { C("REY", "OROS"), C("REY", "COPAS"), C("CABALLO", "OROS"), C("4", "OROS") },
            new[] { C("AS", "OROS"), C("AS", "COPAS"), C("5", "OROS"), C("6", "OROS") },
            new[] { C("SOTA", "OROS"), C("7", "OROS"), C("5", "COPAS"), C("6", "COPAS") },
            new[] { C("4", "COPAS"), C("7", "COPAS"), C("SOTA", "COPAS"), C("CABALLO", "COPAS") });
        Comprobar(log, sn.HacerSenal(0, TipoSenalMus.DosReyes), "Jugador 0 puede senalar dos reyes");
        Comprobar(log, !sn.HacerSenal(0, TipoSenalMus.DosAses), "Jugador 0 no puede senalar dos ases sin tenerlos");
        Comprobar(log, sn.Observar(2).SenalesRecibidas.Contains(TipoSenalMus.DosReyes), "El companero (2) recibe la sena");
        Comprobar(log, sn.Observar(1).SenalesRecibidas.Count == 0 && sn.Observar(3).SenalesRecibidas.Count == 0, "Los rivales no reciben la sena");

        log("== Partidas completas aleatorias ==");
        SimuladorMus sim = new();
        int abortadas = 0, invalidas = 0, malCerradas = 0;
        Stopwatch reloj = Stopwatch.StartNew();
        for (int i = 0; i < 2000; i++)
        {
            IBotMus[] bots = { new BotAleatorioMus(), new BotAleatorioMus(), new BotAleatorioMus(), new BotAleatorioMus() };
            ResultadoPartidaMus r = sim.JugarPartida(bots, i, i);
            if (r.Abortada) abortadas++;
            invalidas += r.AccionesInvalidas;
            bool cierreCorrecto = r.PorOrdago || r.Puntos0 >= 40 || r.Puntos1 >= 40;
            if (!cierreCorrecto || r.ParejaGanadora < 0) malCerradas++;
        }
        Comprobar(log, abortadas == 0, "2000 partidas aleatorias sin abortar");
        Comprobar(log, invalidas == 0, "Ninguna accion legal rechazada por las reglas");
        Comprobar(log, malCerradas == 0, "Todas terminan por 40 tantos u ordago");
        log("  (" + reloj.ElapsedMilliseconds + " ms)");

        log("== Desglose del tanteo ==");
        bool desgloseCuadra = true;
        int manosRevisadas = 0;
        for (int s = 0; s < 300; s++)
        {
            PartidaMus pd = new(9000 + s, 40, 4);
            Random azarD = new(s);
            pd.IniciarPartida();
            int pasosD = 0;
            while (!pd.PartidaTerminada && pasosD++ < 20000)
            {
                if (pd.Fase == FaseMus.FinMano)
                {
                    int suma0 = 0, suma1 = 0;
                    foreach (ConceptoTanteoMus c in pd.Desglose)
                    {
                        if (c.Pareja == 0) suma0 += c.Tantos; else suma1 += c.Tantos;
                    }
                    if (suma0 != pd.PuntosGanadosEnMano(0) || suma1 != pd.PuntosGanadosEnMano(1))
                        desgloseCuadra = false;
                    manosRevisadas++;
                    pd.SiguienteMano();
                    continue;
                }
                List<AccionMus> legalesD = pd.AccionesLegales(pd.JugadorActual);
                pd.Aplicar(pd.JugadorActual, legalesD[azarD.Next(legalesD.Count)]);
            }
        }
        Comprobar(log, desgloseCuadra && manosRevisadas > 0, "El desglose suma exactamente los tantos de la mano (" + manosRevisadas + " manos)");

        log("== Invariantes paso a paso ==");
        bool invariantes = true;
        for (int s = 0; s < 300 && invariantes; s++)
        {
            PartidaMus pv = new(5000 + s, 40, 4);
            Random azar = new(s);
            pv.IniciarPartida();
            int pasos = 0;
            while (!pv.PartidaTerminada && pasos++ < 20000)
            {
                if (pv.Fase == FaseMus.FinMano) { pv.SiguienteMano(); continue; }
                if (!SinDuplicados(pv)) { invariantes = false; break; }
                for (int j = 0; j < 4; j++)
                {
                    if (j != pv.JugadorActual && pv.AccionesLegales(j).Count > 0) { invariantes = false; break; }
                }
                List<AccionMus> legales = pv.AccionesLegales(pv.JugadorActual);
                if (!pv.Aplicar(pv.JugadorActual, legales[azar.Next(legales.Count)])) { invariantes = false; break; }
            }
        }
        Comprobar(log, invariantes, "Sin cartas duplicadas y solo el jugador con turno tiene acciones");

        log("");
        log(fallos == 0 ? "TODAS LAS PRUEBAS SUPERADAS" : fallos + " PRUEBAS FALLIDAS");
        return fallos == 0 ? 0 : 1;
    }

    private static PartidaMus ManoFija(int[] j0, int[] j1, int[] j2, int[] j3)
    {
        PartidaMus p = new(3);
        p.IniciarPartida();
        p.PrepararManosFijas(new[] { j0, j1, j2, j3 });
        p.Aplicar(0, AccionMus.NoHayMus);
        return p;
    }

    private static bool SinDuplicados(PartidaMus p)
    {
        HashSet<int> cartas = new();
        for (int j = 0; j < 4; j++)
            foreach (int c in p.CartasDe(j))
                if (!cartas.Add(c))
                    return false;
        return true;
    }
}
