# BackToSchool - Questionario de Avaliacao da Jogabilidade (Google Forms)

Este guiao foi preparado para transformar o formulario antigo num questionario alinhado com o jogo `BackToSchool`, com foco principal no `GEQ (Game Experience Questionnaire)`.

## Recomendacao geral

Estrutura recomendada para o Google Forms:

1. Apresentacao
2. Consentimento
3. Instrucoes de teste
4. Perfil do participante e contexto da sessao
5. Avaliacao especifica do BackToSchool
6. GEQ - Modulo Principal
7. GEQ - Presenca Social (condicional)
8. GEQ - Pos-jogo
9. Comentarios finais

Recomendacao metodologica:

- Usa o `GEQ Core Module` como bloco principal de avaliacao.
- Usa o `GEQ Post-Game Module` no fim da experiencia.
- Usa o `GEQ Social Presence Module` apenas se o teste tiver sido feito com outros jogadores humanos.
- Nao recomendo usar o `GEQ In-Game` neste trabalho, porque interrompe a sessao e complica a recolha no Google Forms.

## Configuracao do Google Forms

- Tipo de pergunta para os blocos `GEQ`: `Grelha de escolha multipla`.
- Escala do `GEQ`: `Nada (0)`, `Ligeiramente (1)`, `Moderadamente (2)`, `Bastante (3)`, `Extremamente (4)`.
- Tipo de pergunta para o bloco especifico do jogo: `Grelha de escolha multipla`.
- Escala do bloco especifico do jogo: `Discordo totalmente`, `Discordo`, `Nem concordo nem discordo`, `Concordo`, `Concordo totalmente`.
- Ativa `Obrigatoria` em todas as perguntas, exceto genero e comentarios finais, se quiseres manter opcional.
- No modulo social, usa `Ir para a secao com base na resposta`.
- Mantem os prefixos `BTS`, `GEQ`, `SP` e `PG` nas linhas para facilitar a analise depois no Google Sheets.

## Secao 1 - Apresentacao

Titulo do formulario:

`BackToSchool - Questionario de Avaliacao da Jogabilidade`

Descricao sugerida:

`Caro/a participante,`

`Obrigado/a por participar neste estudo de jogabilidade do BackToSchool. Este questionario insere-se no projeto academico desenvolvido no ambito da unidade curricular de Projeto (PRJ), da Licenciatura em Engenharia Informatica e Multimedia (LEIM) do ISEL. O seu principal objetivo e avaliar a experiencia de jogo proporcionada pelo prototipo.`

`A sua participacao e voluntaria e os dados recolhidos serao utilizados exclusivamente para fins academicos, sendo tratados de forma confidencial.`

`O preenchimento demora aproximadamente 10 a 15 minutos. Responda com honestidade, com base na sua experiencia durante a sessao de teste.`

`Desde ja, obrigado/a pela colaboracao.`

## Secao 2 - Consentimento

Titulo da secao:

`Consentimento`

Descricao:

`Antes de continuar, indique se concorda em participar neste estudo de forma voluntaria.`

Pergunta:

- Tipo: `Escolha multipla`
- Titulo: `Concorda em participar neste estudo de forma voluntaria?`
- Opcoes:
  - `Sim, concordo`
  - `Nao, nao concordo`

Logica:

- Se responder `Sim, concordo` -> continuar para a secao seguinte.
- Se responder `Nao, nao concordo` -> terminar formulario.

## Secao 3 - Instrucoes de teste

Titulo da secao:

`Teste o jogo`

Descricao sugerida:

`Antes de responder ao questionario, jogue uma sessao do BackToSchool.`

`BackToSchool e um jogo multijogador em 3D do genero party game, desenvolvido em Unity para Windows PC. Neste jogo, os participantes assumem o papel de alunos numa escola e competem em questionarios sobre diversos temas, escolhidos pelos jogadores e gerados por inteligencia artificial.`

`Durante a partida, os jogadores devem responder aos questionarios sem que o Vigia escute conversas indevidas. Para isso, o jogo integra comunicacao por proximidade, criando tensao adicional durante a sessao. Sempre que o tema muda, os jogadores deslocam-se para uma nova sala, enfrentando uma corrida de obstaculos pelos corredores. No final, vence quem obtiver a maior pontuacao total.`

`Se possivel, experimente pelo menos uma ronda completa com as duas fases principais do jogo:`

- `Fase de quiz na sala de aula`
- `Fase de corrida/intervalo ate a sala seguinte`

`Durante o teste, tente experimentar os seguintes elementos:`

- `Responder as perguntas nos botoes da secretaria`
- `Movimentacao pela sala e pelos corredores`
- `Interacao com objetos e outros jogadores`
- `Pressao do tempo`
- `Presenca do Vigia`
- `Comunicacao por voz, se estiver disponivel`

`Controlos base sugeridos para a versao PC:`

- `WASD` - mover
- `Rato` - olhar
- `Shift` - correr
- `Espaco` - saltar
- `C` - baixar
- `E` - interagir / responder / apanhar objetos
- `Botao esquerdo do rato` - acao fisica / atirar, quando aplicavel

`Link para o jogo: [inserir aqui o link final do BackToSchool]`

## Secao 4 - Perfil do participante e contexto da sessao

Titulo da secao:

`Perfil e contexto`

Perguntas sugeridas:

1. `Idade`
   - Tipo: `Resposta curta`
   - Obrigatoria: `Sim`

2. `Genero (opcional)`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Feminino`
     - `Masculino`
     - `Nao binario`
     - `Prefiro nao responder`
     - `Outro`
   - Obrigatoria: `Nao`

3. `Com que frequencia joga videojogos?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Todos os dias`
     - `Varias vezes por semana`
     - `Uma vez por semana`
     - `Menos de uma vez por semana`
     - `Raramente`

4. `Qual e a sua experiencia com jogos multijogador competitivos/cooperativos?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Muito elevada`
     - `Elevada`
     - `Moderada`
     - `Reduzida`
     - `Nenhuma`

5. `Ja conhecia o BackToSchool antes desta sessao?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Sim`
     - `Nao`

6. `Durante esta sessao, jogou com outros participantes humanos?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Sim`
     - `Nao`
   - Nota: esta resposta sera usada para decidir se aparece o modulo social.

7. `Conseguiu experimentar pelo menos uma fase de quiz e uma fase de corrida/intervalo?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Sim`
     - `Nao`

8. `Usou comunicacao por voz durante o teste?`
   - Tipo: `Escolha multipla`
   - Opcoes:
     - `Sim`
     - `Nao`

## Secao 5 - Avaliacao especifica do BackToSchool

Titulo da secao:

`Elementos especificos do jogo`

Descricao:

`Indique o seu grau de concordancia com as afirmacoes seguintes, tendo em conta a sua experiencia no BackToSchool.`

Configuracao:

- Tipo: `Grelha de escolha multipla`
- Colunas:
  - `Discordo totalmente`
  - `Discordo`
  - `Nem concordo nem discordo`
  - `Concordo`
  - `Concordo totalmente`

Linhas sugeridas:

- `BTS1 - Compreendi rapidamente o objetivo geral do jogo.`
- `BTS2 - Foi facil perceber como responder as perguntas durante a fase de quiz.`
- `BTS3 - Os controlos pareceram-me adequados ao tipo de jogo.`
- `BTS4 - O feedback visual e sonoro ajudou-me a perceber o que estava a acontecer.`
- `BTS5 - A alternancia entre quiz e corrida tornou a experiencia mais interessante.`
- `BTS6 - A presenca do Vigia acrescentou tensao de forma positiva.`
- `BTS7 - As interacoes fisicas entre jogadores e objetos enriqueceram a experiencia.`
- `BTS8 - O equilibrio entre conhecimento, caos e corrida pareceu-me adequado.`
- `BTS9 - O jogo conseguiu criar uma experiencia diferente de um quiz tradicional.`

## Secao 6 - GEQ - Modulo Principal

Titulo da secao:

`GEQ - Experiencia durante o jogo`

Descricao:

`Indique como se sentiu enquanto jogava, usando a escala seguinte.`

Escala:

- `Nada (0)`
- `Ligeiramente (1)`
- `Moderadamente (2)`
- `Bastante (3)`
- `Extremamente (4)`

Sugestao pratica:

- Divide este modulo em `3 grelhas` para o formulario nao ficar demasiado longo.

### Grelha 1

Linhas:

- `GEQ1 - Senti-me satisfeito/a.`
- `GEQ2 - Senti-me habilidoso/a.`
- `GEQ3 - Fiquei interessado/a na historia do jogo.`
- `GEQ4 - Achei divertido.`
- `GEQ5 - Estive completamente absorvido/a pelo jogo.`
- `GEQ6 - Senti-me feliz.`
- `GEQ7 - O jogo pos-me de mau humor.`
- `GEQ8 - Pensei noutras coisas.`
- `GEQ9 - Achei-o cansativo.`
- `GEQ10 - Senti-me competente.`
- `GEQ11 - Achei-o dificil.`

### Grelha 2

Linhas:

- `GEQ12 - Achei-o esteticamente agradavel.`
- `GEQ13 - Esqueci-me de tudo o que me rodeava.`
- `GEQ14 - Senti-me bem.`
- `GEQ15 - Fui bom/boa a joga-lo.`
- `GEQ16 - Senti-me aborrecido/a.`
- `GEQ17 - Senti-me bem-sucedido/a.`
- `GEQ18 - Senti-me imaginativo/a.`
- `GEQ19 - Senti que podia explorar coisas.`
- `GEQ20 - Gostei da experiencia.`
- `GEQ21 - Fui rapido/a a atingir os objetivos do jogo.`
- `GEQ22 - Senti-me incomodado/a.`

### Grelha 3

Linhas:

- `GEQ23 - Senti-me pressionado/a.`
- `GEQ24 - Senti-me irritado/a.`
- `GEQ25 - Perdi a nocao do tempo.`
- `GEQ26 - Senti-me desafiado/a.`
- `GEQ27 - Achei-o impressionante.`
- `GEQ28 - Estive profundamente concentrado/a no jogo.`
- `GEQ29 - Senti-me frustrado/a.`
- `GEQ30 - Pareceu-me uma experiencia rica.`
- `GEQ31 - Perdi a ligacao com o mundo exterior.`
- `GEQ32 - Senti pressao do tempo.`
- `GEQ33 - Tive de fazer muito esforco.`

## Secao 7 - GEQ - Presenca Social (condicional)

Titulo da secao:

`GEQ - Presenca social`

Mostrar esta secao apenas se a pergunta `Durante esta sessao, jogou com outros participantes humanos?` tiver a resposta `Sim`.

Descricao:

`As afirmacoes seguintes dizem respeito a forma como se sentiu em relacao aos outros jogadores durante a sessao.`

Configuracao:

- Tipo: `Grelha de escolha multipla`
- Colunas:
  - `Nada (0)`
  - `Ligeiramente (1)`
  - `Moderadamente (2)`
  - `Bastante (3)`
  - `Extremamente (4)`

Sugestao pratica:

- Divide este modulo em `2 grelhas`.

### Grelha 1

Linhas:

- `SP1 - Empatizei com os outros jogadores.`
- `SP2 - As minhas acoes dependeram das acoes dos outros.`
- `SP3 - As acoes dos outros dependeram das minhas.`
- `SP4 - Senti-me ligado/a aos outros.`
- `SP5 - Os outros prestaram muita atencao a mim.`
- `SP6 - Prestei muita atencao aos outros.`
- `SP7 - Senti ciumes dos outros.`
- `SP8 - Gostei de estar com os outros.`
- `SP9 - Quando eu estava feliz, os outros tambem estavam felizes.`

### Grelha 2

Linhas:

- `SP10 - Quando os outros estavam felizes, eu tambem estava feliz.`
- `SP11 - Influenciei o estado de espirito dos outros.`
- `SP12 - Fui influenciado/a pelo estado de espirito dos outros.`
- `SP13 - Admirei os outros.`
- `SP14 - O que os outros fizeram afetou o que eu fiz.`
- `SP15 - O que eu fiz afetou o que os outros fizeram.`
- `SP16 - Senti vontade de me vingar.`
- `SP17 - Senti satisfacao com o infortunio dos outros.`

## Secao 8 - GEQ - Pos-jogo

Titulo da secao:

`GEQ - Como se sentiu depois de jogar`

Descricao:

`Indique como se sentiu apos terminar a sessao de jogo.`

Configuracao:

- Tipo: `Grelha de escolha multipla`
- Colunas:
  - `Nada (0)`
  - `Ligeiramente (1)`
  - `Moderadamente (2)`
  - `Bastante (3)`
  - `Extremamente (4)`

Sugestao pratica:

- Divide este modulo em `2 grelhas`.

### Grelha 1

Linhas:

- `PG1 - Senti-me revitalizado/a.`
- `PG2 - Senti-me mal.`
- `PG3 - Tive dificuldade em voltar a realidade.`
- `PG4 - Senti-me culpado/a.`
- `PG5 - Pareceu uma vitoria.`
- `PG6 - Pareceu-me uma perda de tempo.`
- `PG7 - Senti-me cheio/a de energia.`
- `PG8 - Senti-me satisfeito/a.`
- `PG9 - Senti-me desorientado/a.`

### Grelha 2

Linhas:

- `PG10 - Senti-me exausto/a.`
- `PG11 - Senti que poderia ter feito coisas mais uteis.`
- `PG12 - Senti-me poderoso/a.`
- `PG13 - Senti-me cansado/a.`
- `PG14 - Senti arrependimento.`
- `PG15 - Senti vergonha.`
- `PG16 - Senti orgulho.`
- `PG17 - Tive a sensacao de ter regressado de uma viagem.`

## Secao 9 - Comentarios finais

Titulo da secao:

`Comentarios finais`

Perguntas sugeridas:

1. `Que elemento do BackToSchool achou mais divertido ou mais conseguido?`
   - Tipo: `Paragrafo`

2. `Que parte do jogo lhe causou mais frustracao, confusao ou dificuldade?`
   - Tipo: `Paragrafo`

3. `Se pudesse mudar apenas uma coisa no jogo, o que mudaria?`
   - Tipo: `Paragrafo`

4. `Comentarios adicionais (opcional)`
   - Tipo: `Paragrafo`

## Scoring do GEQ

No Google Sheets, a pontuacao de cada componente deve ser calculada pela media dos itens correspondentes.

### GEQ - Modulo Principal

- `Competencia`: GEQ2, GEQ10, GEQ15, GEQ17, GEQ21
- `Imersao sensorial e imaginativa`: GEQ3, GEQ12, GEQ18, GEQ19, GEQ27, GEQ30
- `Fluxo`: GEQ5, GEQ13, GEQ25, GEQ28, GEQ31
- `Tensao / Irritacao`: GEQ22, GEQ24, GEQ29
- `Desafio`: GEQ11, GEQ23, GEQ26, GEQ32, GEQ33
- `Efeito negativo`: GEQ7, GEQ8, GEQ9, GEQ16
- `Efeito positivo`: GEQ1, GEQ4, GEQ6, GEQ14, GEQ20

### GEQ - Presenca Social

- `Envolvimento psicologico - Empatia`: SP1, SP4, SP8, SP9, SP10, SP13
- `Envolvimento psicologico - Sentimentos negativos`: SP7, SP11, SP12, SP16, SP17
- `Envolvimento comportamental`: SP2, SP3, SP5, SP6, SP14, SP15

### GEQ - Pos-jogo

- `Experiencia positiva`: PG1, PG5, PG7, PG8, PG12, PG16
- `Experiencia negativa`: PG2, PG4, PG6, PG11, PG14, PG15
- `Cansaco`: PG10, PG13
- `Retorno a realidade`: PG3, PG9, PG17

## Sugestao de analise final

Se quiseres cruzar os dados de forma simples no relatorio:

- compara `jogadores com experiencia elevada` vs `reduzida`
- compara `quem jogou com outros` vs `quem nao jogou com outros`
- observa especialmente os resultados de `Desafio`, `Fluxo`, `Tensao/Irritacao` e `Presenca Social`, porque sao dimensoes muito relevantes para o BackToSchool
- usa as respostas abertas para justificar os pontos fortes e os problemas de design mais recorrentes

## Observacao final

Se quiseres encurtar o formulario, a versao minima que ainda faz sentido academicamente e:

- perfil e contexto
- bloco especifico do BackToSchool
- `GEQ - Modulo Principal`
- comentarios finais

Mas, para este jogo, a melhor versao e manter tambem:

- `GEQ - Pos-jogo`
- `GEQ - Presenca Social` quando a sessao for realmente multijogador
