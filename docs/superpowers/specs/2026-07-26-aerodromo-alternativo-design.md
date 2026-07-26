# Aeródromo alternativo no cálculo de combustível

Data: 2026-07-26

## Objetivo

Permitir informar os dados do aeródromo **alternativo** (destino de desvio) e usar
uma perna calculada destino → alternado no cálculo do combustível, substituindo o
antigo campo de "minutos de alternativa" digitado manualmente.

## Decisões de desenho (aprovadas)

- **Modelo**: perna de **cruzeiro simples**. O usuário informa distância (NM) e curso
  magnético do destino ao alternado; a perna é voada em cruzeiro, usando a
  velocidade/consumo de cruzeiro e o vento do plano.
- **Substituição total**: o campo `AlternativaMin` (minutos manuais) é removido. O
  combustível do alternado passa a vir sempre da perna calculada.
- **Fora de escopo (YAGNI)**: perfil vertical próprio do alternado, elevação do
  alternado, segundo alternado.

## Mudanças

### 1. Domínio — `PlanoDeVoo`
Novos campos (entradas):
- `AlternadoNome : string`
- `AlternadoDistanciaNm : double`
- `AlternadoCursoMag : double`

### 2. Domínio — `ParametrosCombustivel`
Remover `AlternativaMin`.

### 3. Motor — `CalculadoraNavegacao`
- Novo helper que monta um `Segmento` sintético do alternado
  (`De = DestinoNome`, `Para = AlternadoNome`, `Fase = Cruzeiro`, nivelado na
  `AltitudeCruzeiroFt`) e o passa pelo **mesmo `CalcularSegmento`** já existente —
  reaproveitando TAS, triângulo do vento, GS, tempo e combustível.
- `CalcularCombustivel` recebe o combustível dessa perna como `AlternativaKg`.
- Distância ≤ 0 ou perna insolúvel → alternado = 0 kg, sem erro.

### 4. Resultado — `ResultadoPlano`
- Adicionar `Alternado : ResultadoPerna?` para exibição (dist, curso, GS, tempo, kg).
- `ResultadoCombustivel.AlternativaKg` permanece.

### 5. UI — `MainWindow.xaml` + `MainViewModel`
- Nova seção "Alternado" no painel de parâmetros: Nome, Distância (NM), Curso (°).
- Remover o campo "Alternativa (min)".
- Bloco de combustível: linha do alternado mostra também a perna
  (ex.: "Alternado SBXX — 18 NM, 09 min → 6,0 kg").
- Recálculo imediato a cada edição (padrão do app).

### 6. Exportação IVAO — `ExportacaoIvao`
- Preencher o campo oficial `alternativeId` com `AlternadoNome` (quando houver).

### 7. Persistência
- `.vfrplan`: os campos novos entram no JSON automaticamente. Arquivos antigos com
  `AlternativaMin` continuam abrindo (propriedade desconhecida é ignorada).
- CSV: a linha "Alternativa" passa a incluir distância/tempo.

## Testes

- Combustível do alternado calculado por distância/curso (substitui o teste que usava
  minutos).
- Distância 0 → alternado 0 kg.
- Alternado insolúvel (vento > TAS) → 0 kg, sem exceção.
- IVAO: `alternativeId` preenchido a partir do nome do alternado; ausente quando vazio.

## Fora de escopo

Perfil vertical do alternado, elevação do alternado, múltiplos alternados.
