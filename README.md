# Planejamento de Voo Visual (VFR)

Aplicativo desktop (Windows / WPF) para montar o **navlog** de um voo VFR: você
informa a rota e os parâmetros da aeronave, e o programa calcula o perfil vertical,
proas, tempos, combustível e os pontos notáveis (TOC/TOD) — pronto para exportar
para a IVAO ou salvar em disco.

Versão atual: **1.1.0**

---

## Recursos

- **Navlog automático** a partir da rota: para cada trecho o motor calcula TAS,
  correção de deriva (WCA), proa magnética, GS, tempo e combustível.
- **Perfil vertical calculado pela razão** (performance da aeronave): a subida e a
  descida são distribuídas pelos trechos conforme a razão típica, e os pontos
  **TOC** (Top of Climb) e **TOD** (Top of Descent) aparecem no navlog **entre
  quais waypoints** serão atingidos — como linhas não editáveis.
- **Fase automática** de cada trecho (subida / cruzeiro / descida), inferida dos dados.
- **Step climb**: você pode sobrescrever a altitude de um trecho e isso fixa a nova
  altitude para os trechos seguintes (exceto o destino).
- **Origem e destino como parâmetros** (com elevações); na tabela de rota você
  informa apenas o "Para" (waypoint de destino) e os dados para chegar até ele.
- **Marcação de progresso**: um checkbox por linha do navlog para marcar por onde já
  passou — a linha muda de cor ao ser marcada.
- **Exportação para a IVAO**: abre o formulário oficial da IVAO
  (`fpl.ivao.aero`) já pré-preenchido, via URL com o plano em Base64.
- **Persistência** em arquivo `.vfrplan` (JSON) e **exportação CSV** do navlog.
- **Exibição em pt-BR** (vírgula decimal); a serialização é sempre em cultura
  invariante, para o arquivo ser portátil.

> **Tudo é magnético.** O aplicativo trabalha o tempo todo com valores magnéticos
> (cursos, proas, vento). **Não há conversão de declinação** em nenhum ponto.

---

## Arquitetura

A solução tem três projetos:

| Projeto | Papel |
|---|---|
| `PlanejamentoVooVisual.Core` | Motor de cálculo puro (domínio + fórmulas). Sem dependência de UI — testável isoladamente. |
| `PlanejamentoVooVisual` | Aplicativo WPF (janela única, MVVM escrito à mão, recálculo imediato a cada edição). |
| `PlanejamentoVooVisual.Core.Tests` | Testes xUnit do motor (fórmulas, perfil vertical, exportação IVAO, persistência). |

Peças principais do núcleo:

- `Aerodinamica` — TAS pela densidade em altitude, triângulo do vento (WCA/GS).
- `PerfilVertical` — resolve o perfil de altitude ao longo da rota e injeta os
  vértices sintéticos TOC/TOD/BOD.
- `CalculadoraNavegacao` — orquestra o cálculo de cada segmento e os totais.
- `ExportacaoIvao` — monta a URL de pré-preenchimento da IVAO.
- `PlanoPersistencia` — salva/abre `.vfrplan` e exporta CSV.

---

## Requisitos

- Windows 10/11
- [.NET SDK 9](https://dotnet.microsoft.com/download) (o alvo é `net9.0-windows`)

## Como compilar e executar

```powershell
# Restaurar e compilar tudo
dotnet build PlanejamentoVooVisualSolution.sln -c Release

# Executar o app
dotnet run --project PlanejamentoVooVisual -c Release
```

## Testes

```powershell
dotnet test PlanejamentoVooVisual.Core.Tests
```

## Gerar o instalador (MSI)

O instalador usa [WiX v5](https://wixtoolset.org/) (dotnet global tool). Uma vez:

```powershell
dotnet tool install --global wix --version 5.0.2
wix extension add -g WixToolset.UI.wixext/5.0.2
```

Depois, para publicar o app (self-contained x64) e gerar o MSI:

```powershell
powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1
```

O instalador sai em `artifacts\PlanejamentoVooVisual-Setup.msi`. Ele é *perMachine*
(instala em Arquivos de Programas e pede elevação/UAC) e cria atalhos no Menu
Iniciar e na Área de Trabalho.

---

## Formato `.vfrplan`

É um JSON (indentado, enums como texto, cultura invariante) contendo o plano: origem,
destino, altitude de cruzeiro, aeronave, regras, EOBT e a lista de trechos com seus
overrides. Como é texto, pode ser versionado e inspecionado à mão.

## Exportação para a IVAO

A IVAO não importa arquivos — o plano vive no sistema web e é pré-carregado por URL.
O app monta o objeto do plano, **codifica em Base64** e redireciona para
`https://fpl.ivao.aero/flight-plans/create?flightPlan=<Base64>`. Você revisa e envia
no próprio site (requer login na IVAO). Referência da API:
<https://wiki.ivao.aero/en/home/devops/api/flightplan>.

---

## Aviso

Software para **simulação de voo**. Não deve ser usado para operações aéreas reais.
