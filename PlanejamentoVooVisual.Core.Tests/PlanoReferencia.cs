using PlanejamentoVooVisual.Core;

namespace PlanejamentoVooVisual.Core.Tests;

/// <summary>
/// Rota de referência no novo modelo: o usuário informa a rota (waypoints,
/// distâncias, cursos) e o perfil vertical (partida/cruzeiro/destino + razões);
/// as altitudes e fases são calculadas.
/// </summary>
internal static class PlanoReferencia
{
    public static PlanoDeVoo Criar()
    {
        var plano = new PlanoDeVoo
        {
            Aeronave = "PT-REF",
            Piloto = "Referência",
            OrigemNome = "A",
            DestinoNome = "E",
            ElevacaoPartidaFt = 2400,
            AltitudeCruzeiroFt = 5500,
            ElevacaoDestinoFt = 2200,
            Atmosfera = new CondicoesAtmosfera
            {
                VentoDirGrausMag = 270,
                VentoVelKt = 15,
                AltRefFt = 2400,
                OatRefC = 22,
                GradienteCPor1000Ft = 1.98
            },
            Combustivel = new ParametrosCombustivel
            {
                PartidaTaxiKg = 3,
                ReservaMin = 45,
                ContingenciaPercentual = 0.05,
                AlternativaMin = 0
            }
        };

        plano.Perfis.Add(new PerfilFase { Fase = Fase.Subida, IasKt = 80, ConsumoKgH = 30, RazaoTipicaFtMin = 700 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Cruzeiro, IasKt = 110, ConsumoKgH = 22, RazaoTipicaFtMin = 0 });
        plano.Perfis.Add(new PerfilFase { Fase = Fase.Descida, IasKt = 120, ConsumoKgH = 15, RazaoTipicaFtMin = -500 });

        // Origem A → B → C → D → E (destino). Só o "para" é informado.
        plano.Pernas.Add(new Perna { Para = "B", DistanciaNm = 25, CursoMag = 292 });
        plano.Pernas.Add(new Perna { Para = "C", DistanciaNm = 30, CursoMag = 274 });
        plano.Pernas.Add(new Perna { Para = "D", DistanciaNm = 14, CursoMag = 251 });
        plano.Pernas.Add(new Perna { Para = "E", DistanciaNm = 20, CursoMag = 240 });

        plano.RenumerarPernas();
        return plano;
    }
}
