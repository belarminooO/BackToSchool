Projeto
Ano letivo 2025/2026
ISEL - DEETC - LEIM

Back to School
1 de junho de 2026
Docente: Prof. Hugo Cordeiro
Arguente: Prof. Leticia Lucas
Trabalho realizado por:
Miguel Ferreira nº: 51878
Belarmino Sacate nº: 52057
Turma: [a confirmar]

## Índice

1. Introdução .... 5
2. Requisitos do sistema .... 5
2.1 Enquadramento e objetivos .... 5
2.2 Requisitos funcionais .... 6
2.2.1 Regras de jogo .... 6
2.2.2 Controlo do jogador .... 7
2.2.3 Persistência .... 7
2.2.4 Interface .... 8
2.2.5 Autonomia .... 8
2.2.6 Som .... 9
2.3 Requisitos não funcionais e atributos do sistema .... 9
2.4 Casos de utilização principais .... 10
3. Conceção do jogo .... 11
3.1 Motivação e objetivos de design .... 11
3.2 Inspirações .... 12
3.3 Conceito final .... 12
4. Arquitetura e tecnologias .... 13
4.1 Visão geral da arquitetura .... 13
4.2 Tecnologias principais .... 13
4.3 Estrutura lógica do projeto .... 14
4.4 Decisões técnicas estruturantes .... 15
4.5 Fluxo técnico de uma ronda .... 16
5. Implementação atual do protótipo .... 16
5.1 Geração dinâmica de quizzes .... 17
5.2 Gestão do ciclo de jogo .... 17
5.3 Jogador, interação e mobilidade .... 18
5.4 Comunicação por voz e deteção do Vigia .... 18
5.5 Pontuação, teletransporte e progressão .... 19
5.6 Interface e menus .... 20
5.7 Estado atual do protótipo .... 21
5.8 Conformidade entre requisitos e implementação .... 23
5.9 Validação e estratégia de teste .... 24
5.10 Limitações técnicas e trabalho em aberto .... 25
5.11 Priorização do trabalho até à entrega final .... 26
6. Conclusão .... 27
7. Referências .... 28

## Índice de figuras

Figura 1: Arquitetura lógica simplificada do sistema Back to School. .... 15
Figura 2: Fluxo simplificado de uma ronda de jogo e respetivas transições de estado. .... 16
Figura 3: Pipeline simplificada da voz por proximidade até à deteção do Vigia. .... 19
Figura 4: Vista atual da cena LobbySample, usada para menus e configuração inicial do lobby. .... 21
Figura 5: Vista geral atual da cena Quiz AI, evidenciando a disposição global do percurso e das salas. .... 22
Figura 6: Vista atual da Classroom 2 na cena Quiz AI, com quadro interativo, secretarias e botões de resposta. .... 22

## Índice de tabelas

Tabela 1: Requisitos funcionais relativos às regras de jogo. .... 6
Tabela 2: Requisitos funcionais relativos ao controlo do jogador. .... 7
Tabela 3: Requisitos funcionais relativos à persistência de dados. .... 7
Tabela 4: Requisitos funcionais relativos à interface do utilizador. .... 8
Tabela 5: Requisitos funcionais relativos à autonomia das personagens não jogáveis. .... 8
Tabela 6: Requisitos funcionais relativos ao sistema de som. .... 9
Tabela 7: Atributos não funcionais e metas de qualidade do sistema. .... 10
Tabela 8: Casos de utilização principais considerados no projeto. .... 10
Tabela 9: Tecnologias principais adotadas no desenvolvimento de Back to School. .... 13
Tabela 10: Matriz resumida de conformidade entre requisitos e implementação observada. .... 23
Tabela 11: Síntese das evidências de validação identificadas no projeto. .... 25
Tabela 12: Prioridades de evolução identificadas para a fase final do projeto. .... 26

# 1. Introdução

A proposta 61 da Unidade Curricular de Projeto definiu Back to School como um party game multijogador em três dimensões, desenvolvido em Unity para Windows, no qual os jogadores assumem o papel de alunos que competem em questionários temáticos gerados por inteligência artificial. Desde a sua formulação inicial, o projeto distinguiu-se por combinar uma situação escolar reconhecível com uma dinâmica competitiva menos convencional: responder corretamente não basta, porque os jogadores também precisam de comunicar com prudência, evitar a deteção do Vigia e atravessar corredores com obstáculos sempre que a partida transita para uma nova sala.

A partir dessa base conceptual, o trabalho evoluiu para um protótipo que articula geração dinâmica de quizzes, sincronização multijogador, interação física, progressão por rondas e voz por proximidade com impacto direto na jogabilidade. O presente relatório documenta essa evolução, relacionando a proposta inicial, o Game Design Document, abreviado GDD, a tradução do GDD para requisitos e o estado efetivamente observado no repositório Unity. A opção por iniciar o desenvolvimento analítico do documento com os requisitos resulta, assim, de uma preocupação de rastreabilidade: antes de discutir soluções técnicas, importa clarificar com precisão aquilo que o sistema se propõe cumprir.

O relatório encontra-se organizado da seguinte forma: a Secção 2 sistematiza os requisitos funcionais e não funcionais do projeto; a Secção 3 apresenta a motivação, as inspirações e o conceito final do jogo; a Secção 4 descreve a arquitetura técnica, as tecnologias adotadas e as decisões estruturantes do desenvolvimento; a Secção 5 documenta a implementação atual do protótipo, o grau de conformidade entre requisitos e código, a estratégia de validação seguida e as limitações identificadas; por fim, a Secção 6 sintetiza o trabalho realizado e identifica os passos seguintes considerados mais relevantes. Deste modo, o documento procura acompanhar o percurso do projeto desde a sua formulação conceptual até à sua materialização técnica, sem abdicar de uma leitura crítica do estado atual do sistema.

# 2. Requisitos do sistema

A presente secção organiza a especificação do sistema em quatro planos complementares. Começa por enquadrar a origem e os objetivos dos requisitos, prossegue com a identificação dos requisitos funcionais, reúne depois os requisitos não funcionais e termina com os casos de utilização principais que sintetizam a interação entre jogadores e sistema.

## 2.1 Enquadramento e objetivos

A formalização de requisitos teve como principal objetivo transformar uma descrição predominantemente lúdica, presente no GDD, numa especificação suficientemente clara para orientar o desenvolvimento, a validação e a redação do presente relatório. Os requisitos foram organizados por áreas funcionais, nomeadamente regras de jogo, controlo do jogador, persistência, interface, autonomia de personagens não jogáveis e som. Foi ainda preservada a categorização usada no documento de origem, distinguindo requisitos evidentes, invisíveis e de adorno, uma vez que essa separação ajuda a perceber se determinada funcionalidade é diretamente observável pelo jogador ou se atua sobretudo ao nível interno do sistema. Em complemento, foram também considerados atributos relacionados com usabilidade, desempenho, acessibilidade, plataforma-alvo e coerência estética.

## 2.2 Requisitos funcionais

Nas tabelas seguintes apresentam-se os requisitos funcionais identificados para o projeto. Em conjunto, estes requisitos definem o ciclo principal da partida, as capacidades do jogador, a infraestrutura mínima de armazenamento, a interface visível ao utilizador, o comportamento autónomo dos agentes presentes no cenário e a componente sonora que suporta a experiência de jogo.

Durante esta sistematização foi corrigida uma incoerência de numeração presente no documento intermédio, onde o requisito relativo aos menus pré-jogo surgia identificado como R2.4. No presente relatório, esse requisito passa a ser designado R4.5, por pertencer claramente ao grupo de interface e por essa correção facilitar a leitura do conjunto.

### 2.2.1 Regras de jogo

Tabela 1: Requisitos funcionais relativos às regras de jogo.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R1.1 | O sistema deve alternar as rondas entre "Rondas de Perguntas" e "Intervalos nos Corredores da Escola". | Evidente |
| R1.2 | O sistema deve exibir 5 perguntas interativas por cada ronda no quadro da sala de aula. | Evidente |
| R1.3 | O sistema deve penalizar o último jogador a chegar à sala de aula ou todos os jogadores que não chegarem à próxima sala dentro do tempo definido. | Evidente |
| R1.4 | Os jogadores devem ser organizados em equipas de dois, ou sozinhos caso o número de participantes seja ímpar. | Invisível |
| R1.5 | A equipa ou o jogador com maior pontuação total no final deverá ser o vencedor da partida. | Evidente |
| R1.6 | O sistema de castigo deve teletransportar o jogador apanhado pelo Vigia para a jaula na sala, impedindo-o de responder a mais perguntas na respetiva ronda. | Evidente |

### 2.2.2 Controlo do jogador

Tabela 2: Requisitos funcionais relativos ao controlo do jogador.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R2.1 | Movimento tridimensional em primeira pessoa, bem como salto, sprint e agachamento. | Evidente |
| R2.2 | Agarrar e empurrar oponentes. | Evidente |
| R2.3 | Agarrar objetos físicos e atirá-los. | Evidente |
| R2.4 | Carregar em botões de interação nas secretarias, correspondentes às respostas do quiz, ou interagir com portas. | Evidente |
| R2.5 | Falar por voz com sistema de proximidade. | Evidente |
| R2.6 | A câmara com Cinemachine deve seguir os movimentos do jogador em primeira pessoa. | Evidente |

### 2.2.3 Persistência

Tabela 3: Requisitos funcionais relativos à persistência de dados.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R3.1 | Guardar os temas do quiz em formato CSV no anfitrião. | Invisível |
| R3.2 | Gerar e guardar as perguntas do quiz produzidas por inteligência artificial em formato JSON. | Invisível |
| R3.3 | Guardar as respostas e as respetivas pontuações dos jogadores. | Invisível |

### 2.2.4 Interface

Tabela 4: Requisitos funcionais relativos à interface do utilizador.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R4.1 | Exibir uma mira no centro do ecrã. | Evidente |
| R4.2 | Mostrar uma barra de tempo que indique visualmente o prazo para responder no quiz e para concluir a corrida nos corredores. | Evidente |
| R4.3 | Exibir a pontuação individual e coletiva de todos os jogadores através do leaderboard. | Evidente |
| R4.4 | Mostrar um indicador quando um jogador está a usar o sistema de voz por proximidade. | Evidente |
| R4.5 | Disponibilizar menus pré-jogo com as opções Create Lobby, Browse Lobby, Join Lobby, Settings e Quit. | Evidente |
| R4.6 | Disponibilizar definições de criação de lobby para o anfitrião com Round Timer, Round Number e Quiz Themes. | Evidente |

### 2.2.5 Autonomia

Tabela 5: Requisitos funcionais relativos à autonomia das personagens não jogáveis.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R5.1 | O Vigia deve vigiar a sala de aula com um cone de visão. | Evidente |
| R5.2 | O Vigia deve deslocar-se com navmesh pela sala durante o quiz. | Evidente |
| R5.3 | O Vigia deve detetar níveis de som elevados e dirigir-se ao local para aplicar uma penalidade. | Evidente |
| R5.4 | Os Colegas devem ter um comportamento simples, andando pelos corredores para servirem de obstáculos na corrida. | Evidente |

### 2.2.6 Som

Tabela 6: Requisitos funcionais relativos ao sistema de som.

| Referência | Descrição | Categoria |
| --- | --- | --- |
| R6.1 | O jogo deverá reproduzir músicas diferentes consoante os jogadores estejam a responder ao quiz ou a participar na corrida de obstáculos. | Evidente |
| R6.2 | O sistema deve produzir sons associados a passos, apanhar objetos, cair no chão, atirar objetos, empurrar, agarrar, saltar e pressionar botões. | Evidente |
| R6.3 | O sistema deve produzir indicadores sonoros associados ao tempo, nomeadamente contagens decrescentes e transições entre rondas. | Evidente |
| R6.4 | O jogo deve reproduzir som ambiente adequado ao contexto, para além da música. | Adorno |
| R6.5 | Deve ser possível alterar o volume do som. | Evidente |

Observa-se que o núcleo funcional do projeto assenta numa alternância rigorosa entre a fase de quiz e a fase de deslocação, sendo essa alternância suportada por mecânicas de resposta, mobilidade, sabotagem e penalização. Para além disso, a presença do sistema de voz por proximidade, do Vigia e dos Colegas transforma o simples questionário num espaço de tensão social e física, o que reforça a identidade própria do jogo. A definição formal destes requisitos foi essencial para alinhar o trabalho de implementação com o conceito inicialmente proposto.

## 2.3 Requisitos não funcionais e atributos do sistema

Os requisitos não funcionais complementam a especificação anterior ao estabelecer restrições e metas de qualidade que condicionam a experiência global do utilizador. Neste projeto, assumem particular importância a facilidade de utilização, a legibilidade dos elementos gráficos, a resposta rápida do sistema, o desempenho estável em Windows e a coerência visual entre menus, objetos e cenários. Ainda que alguns destes atributos sejam classificados como desejáveis e não como obrigatórios, a sua presença influencia diretamente a perceção de polimento, justiça e acessibilidade do jogo.

Tabela 7: Atributos não funcionais e metas de qualidade do sistema.

| Atributo | Detalhe | Categoria |
| --- | --- | --- |
| Facilidade de utilização | Controlos intuitivos para um jogador comum. | Desejável |
| Facilidade de utilização | Navegação de menus simples. | Desejável |
| Acessibilidade | Complementar ícones com texto. | Desejável |
| Acessibilidade | Texto legível. | Desejável |
| Interação homem-máquina | Uso exclusivo de rato e teclado. | Obrigatório |
| Interação homem-máquina | Resposta rápida do sistema. | Desejável |
| Plataforma | Windows. | Obrigatório |
| Desempenho | Pelo menos 60 quadros por segundo estáveis. | Desejável |
| Estética | Os objetos em jogo devem seguir um mesmo estilo. | Desejável |
| Estética | Os menus do jogo devem ser visualmente adequados ao contexto do jogo. | Desejável |
| Dificuldade do jogo | O jogo não deve ser propositalmente difícil de completar. | Desejável |

## 2.4 Casos de utilização principais

Para além da enumeração de requisitos, torna-se útil identificar os casos de utilização que organizam a interação entre jogadores e sistema. A leitura destes casos evidencia os momentos centrais da experiência: configuração da partida, parametrização do lobby, resposta às perguntas, deslocação entre salas e consulta do resultado final. A tabela seguinte resume os principais casos de utilização considerados nesta fase do relatório.

Tabela 8: Casos de utilização principais considerados no projeto.

| Caso de utilização | Descrição |
| --- | --- |
| Mudar opções de jogo | Permite alterar parâmetros gerais, com destaque para o volume e restantes definições acessíveis através do menu Settings. |
| Mudar opções do lobby | Permite ao anfitrião definir número de rondas, duração das perguntas e temas que servirão de base à geração dos quizzes. |
| Responder ao quiz | Coloca os jogadores numa sala de aula onde devem responder a perguntas antes do tempo terminar, comunicando e sabotando adversários. |
| Deslocar até à próxima sala | Representa a corrida de obstáculos entre rondas, onde a rapidez influencia diretamente a pontuação final. |
| Consultar classificação final | Apresenta o leaderboard atualizado ao longo da partida e expandido no momento de encerramento do jogo. |

No caso de utilização Responder ao quiz, o sistema inicia uma ronda ao apresentar no quadro a pergunta atual, a contagem decrescente e as opções associadas aos botões físicos disponíveis na secretaria de cada jogador. A partir desse momento, os participantes podem responder, comunicar através de voz por proximidade e perturbar os adversários, tentando simultaneamente evitar a deteção do Vigia. O sistema deve registar a resposta escolhida, indicar quem está a falar e, caso o volume exceda o limiar admissível durante tempo suficiente, desencadear o castigo correspondente. Terminado o tempo da pergunta, a ronda avança para a questão seguinte ou para a fase de transição.

No caso de utilização Deslocar até à próxima sala, a porta de saída é aberta e inicia-se um intervalo sob a forma de corrida de obstáculos pelos corredores da escola. Durante esse período, o sistema altera a música, apresenta o tempo restante e permite aos jogadores correr, saltar e interferir uns com os outros enquanto evitam obstáculos e personagens autónomas. Quando todos chegam ao destino ou o tempo termina, a pontuação é revista, sendo penalizados os jogadores que tenham ficado para trás. Este caso de utilização garante a continuidade entre rondas e impede que a experiência fique limitada a uma sucessão estática de perguntas.

# 3. Conceção do jogo

Nesta secção apresenta-se a base conceptual do projeto antes da análise técnica detalhada. As subseções seguintes expõem a motivação e os objetivos de design, discutem as principais inspirações que moldaram a proposta e explicitam o conceito final que orienta o protótipo atualmente desenvolvido.

## 3.1 Motivação e objetivos de design

A motivação principal por detrás de Back to School resultou da vontade de integrar inteligência artificial generativa numa experiência multijogador que não dependesse apenas de reflexos ou apenas de conhecimento. Em vez de construir um jogo de perguntas tradicional, procurou-se combinar resposta cognitiva, improviso social e perturbação física dentro do mesmo ciclo de jogo. Esta combinação revelou-se particularmente interessante porque permite variar continuamente a natureza do desafio sem obrigar o jogador a aprender sistemas demasiado complexos.

Do ponto de vista do design, foram definidos quatro objetivos centrais: gerar quizzes dinâmicos de acordo com temas configurados pelo anfitrião, suportar uma experiência multijogador sincronizada e social, misturar a fase de quiz com uma corrida de obstáculos no interior da escola e aumentar a repetibilidade da partida através de conteúdo variável e progressão por rondas. A apresentação intermédia do projeto já evidenciava estes objetivos, colocando a integração de inteligência artificial, o multijogador e a alternância entre sala e corredor no centro da proposta. O conceito acabou, assim, por assentar menos na complexidade individual de cada mecânica e mais na forma como essas mecânicas se encadeiam.

## 3.2 Inspirações

Entre as inspirações mais evidentes encontra-se Kahoot!, cujo modelo de perguntas rápidas, limite temporal e classificação intermédia ajudou a definir a fase de quiz. A ideia de associar cada resposta a uma cor específica e de pressionar os jogadores através do tempo disponível foi transposta para um contexto tridimensional, onde a resposta deixa de ser apenas uma opção num ecrã e passa a corresponder a botões físicos espalhados pela sala. Desta forma, um sistema originalmente pensado para contexto pedagógico serviu de base para uma situação competitiva e caótica.

Uma segunda influência relevante foi Dale & Dawson Stationery Supplies, jogo que explora sabotagem, observação social e a presença de uma figura de autoridade. Em Back to School, essa lógica foi adaptada ao ambiente escolar através do Vigia, personagem não jogável que patrulha a sala, investiga ruído e castiga jogadores demasiado ruidosos. A tensão entre colaborar com colegas, enganar adversários e não ser apanhado aproxima o projeto de uma experiência social onde o comportamento dos participantes é tão importante quanto o seu desempenho intelectual.

Por fim, Fall Guys contribuiu sobretudo para a fase de deslocação entre salas, marcada por obstáculos, empurrões e competição pelo tempo. A adoção de uma corrida caótica entre rondas permitiu quebrar a cadência do quiz e introduzir uma segunda modalidade de desafio, mais física e imediata. Esta alternância reforça o ritmo da partida e reduz o risco de monotonia, uma vez que cada ronda passa a combinar pressão cognitiva com movimentação no espaço.

## 3.3 Conceito final

O conceito final de Back to School pode ser descrito como um party game competitivo em primeira pessoa, pensado para grupos de amigos e estruturado em rondas compostas por duas fases complementares. Numa primeira fase, os jogadores respondem a perguntas de escolha múltipla numa sala de aula, podendo comunicar, sabotar e ocupar fisicamente o espaço; numa segunda fase, deslocam-se até à sala seguinte através de um percurso com obstáculos, sob pena de perderem pontuação. O enquadramento escolar, aliado à presença do Vigia e de personagens autónomas nos corredores, procura tornar a experiência imediatamente reconhecível, humorística e potencialmente memorável.

Esta formulação conceptual introduz um desafio de equilíbrio particularmente relevante: o jogo tem de recompensar simultaneamente conhecimento, coordenação espacial, leitura social do grupo e controlo do risco associado à voz. Se uma destas dimensões dominar excessivamente as restantes, a experiência tende a perder identidade própria, aproximando-se demasiado de um quiz tradicional ou, pelo contrário, de uma simples corrida caótica. Por essa razão, a análise do protótipo não deve limitar-se à existência de funcionalidades isoladas, devendo considerar de que modo essas funcionalidades se articulam para sustentar a proposta lúdica inicialmente definida.

# 4. Arquitetura e tecnologias

Esta secção descreve a estrutura técnica que suporta o protótipo e clarifica a forma como os principais subsistemas se articulam. Nas subseções seguintes apresentam-se a visão global da arquitetura, as tecnologias adotadas, a organização lógica do projeto, as decisões técnicas estruturantes e o fluxo operacional de uma ronda completa.

## 4.1 Visão geral da arquitetura

A implementação do projeto assenta sobre Unity 6, utilizando uma arquitetura de rede peer-to-peer, abreviada P2P, suportada por PurrNet e pelos serviços de lobby integrados via Steam. Nesta arquitetura, o anfitrião da sessão assume responsabilidades adicionais, nomeadamente a leitura dos temas do quiz, a geração das perguntas com recurso a uma interface de programação de aplicações externa e a disseminação do estado necessário aos restantes pares. Esta opção permite manter a lógica da partida sincronizada sem recorrer a uma infraestrutura dedicada de servidor central.

Ao nível lógico, o sistema encontra-se organizado em subsistemas relativamente autónomos: gestão do estado da partida, geração de quizzes, pontuação e progressão, controlo do jogador, comunicação por voz, inteligência artificial do Vigia e dos Colegas, interface e configuração de lobby. A separação destes subsistemas foi importante para reduzir acoplamento entre funcionalidades, permitir iteração incremental e tornar o código mais legível. Embora se trate ainda de um protótipo em evolução, a estrutura do projeto já revela uma tentativa consistente de dividir responsabilidades.

## 4.2 Tecnologias principais

O conjunto de tecnologias identificado no repositório confirma a orientação apresentada anteriormente na demonstração intermédia do projeto. Para além do motor e da linguagem base, foram integradas bibliotecas específicas para sincronização multijogador, voz, navegação de agentes, câmaras e gestão de input. Essa composição tecnológica não foi arbitrária; cada componente responde diretamente a uma necessidade concreta do desenho do jogo.

Tabela 9: Tecnologias principais adotadas no desenvolvimento de Back to School.

| Tecnologia | Finalidade no projeto |
| --- | --- |
| Unity 6.0.4f1 | Motor principal do projeto e ambiente de desenvolvimento onde se encontram cenas, prefabs, interface e scripts. |
| C# | Linguagem utilizada para implementar lógica de jogo, sincronização, inteligência artificial e interação. |
| PurrNet | Biblioteca de sincronização multijogador usada para variáveis sincronizadas, chamadas remotas e máquina de estados da partida. |
| PurrLobby e Steamworks.NET | Camada de lobby, descoberta de sessões e integração com o ecossistema Steam. |
| MetaVoiceChat | Solução de voz por proximidade utilizada como base da comunicação entre jogadores. |
| Cinemachine | Gestão das câmaras em primeira pessoa e de câmaras auxiliares de apoio ao desenvolvimento. |
| Unity AI Navigation | Suporte à navegação do Vigia e dos Colegas através de navmesh. |
| Input System | Mapeamento moderno de ações de controlo para movimento, visão, salto e interação. |
| Universal Render Pipeline | Pipeline de renderização adotado pelo projeto para o cenário e para os materiais. |
| Gemini 2.5 Flash | Modelo utilizado para gerar quizzes dinâmicos em formato JSON a partir dos temas definidos pelo anfitrião. |

A presença simultânea destas ferramentas evidencia uma abordagem orientada para a integração de serviços e bibliotecas especializadas, em vez da reimplementação de funcionalidades base. Tal opção acelera o desenvolvimento do protótipo e concentra o esforço do grupo na lógica própria do jogo. Em contrapartida, exige maior cuidado na compatibilização entre pacotes e na gestão do estado de rede, particularmente quando a voz e a geração de conteúdo dinâmico passam a influenciar diretamente a jogabilidade.

## 4.3 Estrutura lógica do projeto

O repositório encontra-se organizado de forma coerente com essa divisão. A pasta Assets/Quiz concentra a lógica específica das rondas, dos botões, da interface do quadro, da pontuação e da inteligência artificial do contexto escolar; a pasta Assets/_Scripts agrega scripts transversais relacionados com jogador, voz, câmaras e comportamento em rede; a pasta Assets/Settings centraliza a configuração do anfitrião; por sua vez, o pacote de lobby e as bibliotecas de terceiros permanecem separadas do código principal. Esta estrutura facilita a manutenção do projeto e clarifica quais os componentes que constituem desenvolvimento próprio e quais os que correspondem a dependências externas.

## 4.4 Decisões técnicas estruturantes

Entre as decisões técnicas mais relevantes destaca-se a concentração de responsabilidades sensíveis no anfitrião da partida. Essa opção observa-se na geração do quiz, na leitura dos temas configurados, no armazenamento local dos ficheiros gerados e na difusão do estado necessário aos restantes participantes. A solução foi particularmente adequada porque reduz duplicação de chamadas externas, evita divergência no conteúdo gerado e simplifica a sincronização do início de partida, ainda que aumente a dependência do host para operações críticas.

Uma segunda decisão importante foi a separação entre transporte de áudio e interpretação do comportamento vocal. Em vez de associar diretamente a mecânica do Vigia ao próprio fluxo bruto de voz, o sistema introduz uma camada intermédia de filtragem e deteção, composta pelo NoiseGate e pelo PlayerVoiceState. Esta decomposição melhora a clareza arquitetural, porque o envio de áudio continua a ser tratado como problema de comunicação em rede, enquanto a deteção de infrações passa a ser tratada como problema de lógica de jogo. Do mesmo modo, o uso combinado de SyncVars e chamadas remotas permite distinguir entre estado persistente, como pontuações e nome do estado atual, e eventos de atualização imediata, como sincronização da pergunta apresentada ou teletransporte de jogadores.

A articulação global destes subsistemas pode ser observada na Figura 1, onde se resume a forma como o anfitrião, a máquina de estados, a camada de rede, a geração de conteúdo e os subsistemas de jogador comunicam entre si. Embora o diagrama simplifique inevitavelmente algumas dependências de implementação, ele ajuda a perceber que o projeto não assenta num único script centralizador, mas antes num conjunto de componentes especializados que cooperam para manter a partida consistente.

Figura 1: Arquitetura lógica simplificada do sistema Back to School.

![Figura 1](/Users/belarmino/Documents/BackToSchool/docs/figures/arquitetura_sistema.png)

## 4.5 Fluxo técnico de uma ronda

Para além da visão estática da arquitetura, importa compreender o fluxo operacional de uma ronda completa. A sessão começa numa fase de espera, durante a qual o anfitrião valida a presença dos jogadores e a disponibilidade do quiz. De seguida, o sistema entra num pequeno estado de preparação e avança para a sequência de perguntas, onde cada iteração sincroniza quadro, temporizador, resposta correta e feedback para todos os clientes. Quando a última pergunta da ronda termina, a lógica transita para a corrida de obstáculos, atualiza o registo de chegadas e calcula penalizações antes de decidir se a partida continua ou se deve ser apresentado o resultado final.

Este encadeamento pode ser observado na Figura 2. A sua relevância para o relatório reside no facto de demonstrar que a dinâmica do jogo foi pensada como um ciclo repetível e verificável, e não como uma sucessão ad hoc de eventos. Em termos de engenharia, esta opção favorece manutenção, depuração e expansão futura, uma vez que cada transição de estado passa a corresponder a uma responsabilidade explícita e mais fácil de testar.

Figura 2: Fluxo simplificado de uma ronda de jogo e respetivas transições de estado.

![Figura 2](/Users/belarmino/Documents/BackToSchool/docs/figures/fluxo_ronda.png)

# 5. Implementação atual do protótipo

A presente secção incide sobre a materialização concreta do projeto no repositório e no editor Unity. Para esse efeito, as subseções seguintes analisam a geração dinâmica de quizzes, a gestão do ciclo de jogo, o jogador e a interação, a voz e o Vigia, a pontuação e progressão, a interface, o estado atual do protótipo, a conformidade com os requisitos, a validação, as limitações técnicas e as prioridades até à entrega final.

## 5.1 Geração dinâmica de quizzes

A geração dinâmica de quizzes encontra-se concentrada no componente GenerateQuizJSON, o qual lê os temas previamente definidos pelo anfitrião a partir do ficheiro quiz_info.csv guardado na área persistente da aplicação. Com base nesses temas e no número de rondas configurado, o sistema constrói programaticamente um pedido textual que impõe restrições explícitas ao modelo generativo, tais como o número exato de rondas, a existência de cinco perguntas por ronda, a presença de quatro opções por pergunta e a randomização do índice da resposta correta. Esta preparação do pedido revelou-se fundamental para reduzir ambiguidade e aumentar a previsibilidade da resposta.

Depois de enviado o pedido à API do modelo Gemini 2.5 Flash, a resposta recebida é limpa, validada e convertida para uma estrutura interna de dados antes de ser guardada em formato JavaScript Object Notation, abreviado JSON, no armazenamento local do anfitrião. A mesma componente atualiza ainda a pergunta corrente, as opções de resposta e o índice correto, tornando essas informações imediatamente disponíveis para a interface do quiz e para a avaliação de pontuação. O código inclui também lógica de repetição da chamada em caso de falha de parsing e prevê um modo alternativo baseado em quiz local quando a geração dinâmica é ignorada, o que simplifica a depuração e reduz dependência de chamadas externas durante testes internos.

## 5.2 Gestão do ciclo de jogo

O ciclo principal da partida é orquestrado pelo QuizGameManager, que funciona como ponto de coordenação entre pontuação, geração de quiz, pontos de spawn, temporização e interface. Em vez de concentrar toda a lógica num único fluxo monolítico, o projeto recorre a uma máquina de estados da biblioteca PurrNet, composta pelos estados Waiting, Freeze, Question, ObstacleRace e ChangeRound. Cada estado possui responsabilidade delimitada: aguardar jogadores e quiz, apresentar uma pausa curta antes da pergunta, gerir a fase de resposta, controlar a corrida de obstáculos e preparar a ronda seguinte.

Esta solução permite que a progressão da partida seja previsível, sincronizada e facilmente extensível. O estado inicial só arranca a sessão quando o quiz já foi gerado e quando o número esperado de jogadores se encontra registado e instanciado na cena. A transição para a fase de pergunta sincroniza o conteúdo do quadro com todos os clientes, enquanto a passagem para a fase de corrida redefine o temporizador, limpa o registo de chegadas e reposiciona os jogadores fora da sala. Por fim, o estado de mudança de ronda atualiza a ronda corrente, volta a atribuir estações e prepara a nova pergunta, fechando o ciclo.

## 5.3 Jogador, interação e mobilidade

Do ponto de vista do jogador, a base de controlo foi implementada sobre Kinematic Character Controller, através do script MyCharacterController, que assegura movimento em primeira pessoa, rotação desacoplada da câmara, salto e gestão de sensibilidade do rato. A integração com o novo Input System de Unity permite separar claramente as ações de movimento, olhar, salto e alternância de câmara, o que facilita futura reconfiguração. Para além disso, foram incluídos mecanismos de teletransporte e de feedback visual, nomeadamente uma vinheta que informa o jogador sobre a correção ou o erro da resposta submetida.

A interação com o espaço é suportada pelo componente Interactor, que utiliza raycasts a partir da fonte de interação do jogador para detetar objetos interativos, apresentar indicação contextual no ecrã e executar a ação adequada quando a tecla de interação é pressionada. Os botões de resposta foram modelados através da dupla AnswerButton e AnswerButtonManager, permitindo associar uma estação a um jogador específico, registar a opção escolhida e animar a pressão do botão em todos os clientes. Já as interações físicas de perturbação assentam em componentes de rede dedicados, com destaque para FirstPersonNetworkState, responsável por coordenar a mecânica de empurrão entre pares e por aplicar knockback de forma sincronizada. Embora a documentação conceptual mencione igualmente agarrar e atirar objetos, o código principal analisado evidencia o empurrão como a interação competitiva mais consolidada nesta fase do desenvolvimento.

## 5.4 Comunicação por voz e deteção do Vigia

Uma das componentes mais distintivas do projeto é a utilização de voz por proximidade como mecânica central e não apenas como acessório social. A integração entre MetaVoiceChat e PurrNet é realizada pelo componente PurrNetNetProvider, responsável por encaminhar quadros de áudio através de chamadas remotas não fiáveis, adequadas à natureza contínua e efémera deste tipo de comunicação. No lado do jogador, o PlayerVoiceController gere o silenciamento do microfone e os indicadores visuais associados ao estado da transmissão.

Para que a voz possa influenciar a jogabilidade, o sistema inclui ainda um filtro de ruído próprio, designado NoiseGate, que calcula o volume aproximado em decibéis, descarta amostras abaixo do limiar definido e disponibiliza um valor suavizado para análise. O componente PlayerVoiceState transforma essa leitura num estado sincronizado de fala, o qual pode ser observado pelo servidor e pelos restantes subsistemas. Esta etapa intermédia foi importante, uma vez que desacopla o transporte de áudio da lógica de deteção comportamental.

A partir dessa informação, o Vigia implementa um comportamento reativo baseado em patrulha por waypoints, cone de visão, raio de audição, investigação de origem sonora e progressão de deteção até à captura. Quando a fala é suficientemente intensa e persistente, o agente desloca-se até ao local suspeito e, se confirmar o infrator em condições válidas, envia-o para a jaula da sala, retirando-lhe a possibilidade de continuar a responder nessa ronda. O resultado é uma mecânica interessante de risco e recompensa, onde comunicar demais pode beneficiar a equipa no imediato mas prejudicá-la pouco depois.

A sequência de transformação do áudio em sinal jogável encontra-se sintetizada na Figura 3. Esta representação é útil porque evidencia que o sistema não trata a voz apenas como canal de comunicação, mas como fonte de dados sujeita a filtragem, sincronização e interpretação comportamental. Tal escolha constitui um dos aspetos tecnicamente mais distintivos do projeto, distinguindo-o de soluções multijogador onde a voz é apenas um serviço paralelo sem impacto mecânico direto.

Figura 3: Pipeline simplificada da voz por proximidade até à deteção do Vigia.

![Figura 3](/Users/belarmino/Documents/BackToSchool/docs/figures/pipeline_voz_vigia.png)

## 5.5 Pontuação, teletransporte e progressão

A gestão de pontuação e progressão entre espaços é assegurada sobretudo pelos componentes ScoreManager, SpawnManager e RoomTrigger. O primeiro mantém valores sincronizados de pontuação por equipa, regista os jogadores presentes, avalia respostas corretas, aplica penalizações a quem não chega atempadamente à sala seguinte e atualiza o leaderboard tanto durante a partida como no ecrã final. O segundo define pontos de spawn por ronda para o interior da sala, para o exterior e para a prisão, executando teletransportes sincronizados sempre que a fase de jogo assim o exige.

Durante a corrida de obstáculos, os jogadores que entram no gatilho da sala correta são assinalados como chegados, enquanto os restantes permanecem elegíveis para penalização quando o temporizador termina. Para além disso, o sistema aplica cosméticos simples de destaque aos melhores e piores classificados, reforçando a leitura competitiva do estado da partida. Apesar de a lógica de equipas ainda poder ser refinada para alinhar totalmente com a regra conceptual de duplas, a infraestrutura existente já suporta classificação, progressão e encerramento da sessão de forma coerente.

## 5.6 Interface e menus

Ao nível de interface, o projeto combina elementos específicos do quiz com a infraestrutura de menus fornecida pelo sistema de lobby. O componente HostQuizManager disponibiliza ao anfitrião controlos para número de rondas, duração das perguntas e temas do quiz, persistindo essas escolhas localmente antes do início da partida. Por sua vez, a interface do quiz é atualizada pelo QuizUIManager, que ativa o quadro correspondente à ronda atual, apresenta pergunta e opções e ajusta a visibilidade do cursor consoante o estado da partida. O temporizador visual é gerido por RoundTimerManager, que adapta tanto o valor apresentado como a cor de preenchimento ao estado corrente da partida.

A solução de menus suporta criação de lobby, procura de salas existentes, entrada por código e acesso a definições, estando em linha com o que havia sido previsto no GDD e apresentado na demonstração intermédia. Em conjunto com indicadores de voz, leaderboard, temporizador e feedback visual de resposta, esta camada de interface procura manter o jogador informado sem sobrecarregar o ecrã. O grau de acabamento visual ainda pode evoluir, mas a estrutura funcional dos menus e do heads-up display, abreviado HUD, encontra-se montada.

A materialização atual desta infraestrutura de arranque pode ser observada na Figura 4, correspondente a uma vista recente da cena LobbySample no editor. Ainda que se trate de uma vista de trabalho e não de uma captura promocional, ela é particularmente útil para o relatório, porque documenta o estado real dos elementos de menu, dos painéis de configuração e da organização espacial usada no lobby multijogador.

Figura 4: Vista atual da cena LobbySample, usada para menus e configuração inicial do lobby.

![Figura 4](/Users/belarmino/Documents/BackToSchool/docs/figures/lobbysample_atual.png)

## 5.7 Estado atual do protótipo

No estado atual do protótipo, já se observam implementações concretas para a geração de quizzes por inteligência artificial, a sincronização multijogador, a progressão por rondas, a fase de sala de aula, a fase de corrida, a deteção do Vigia e a apresentação do resultado final. A apresentação intermédia já apontava como conquistas principais a integração de inteligência artificial no jogo e a estabilidade multijogador via Steam e PurrNet, e a leitura do repositório confirma que esses dois eixos continuam a constituir o núcleo do projeto. Em contrapartida, permanecem relevantes tarefas de polimento, equilíbrio de parâmetros, consolidação de interações físicas, melhoria visual dos cenários, correção de erros remanescentes e aprofundamento de testes em contexto real de partida.

Do ponto de vista espacial, a cena Quiz AI já evidencia uma estrutura global concreta, com várias salas, percursos de ligação, obstáculos e elementos de ambientação, como se pode observar na Figura 5. Esta vista geral é útil porque mostra que o projeto já não se encontra circunscrito a uma única sala isolada, existindo uma organização de cenário compatível com a alternância entre perguntas e deslocação competitiva prevista nos requisitos.

Figura 5: Vista geral atual da cena Quiz AI, evidenciando a disposição global do percurso e das salas.

![Figura 5](/Users/belarmino/Documents/BackToSchool/docs/figures/quiz_ai_visao_geral.png)

Por sua vez, a Figura 6 destaca uma vista recente da Classroom 2, onde já se reconhecem o quadro de apresentação, as secretarias distribuídas pela sala e os botões físicos usados para responder ao quiz. Em conjunto, estas duas figuras documentam com maior fidelidade o estado atual do protótipo do que as capturas antigas anteriormente usadas, refletindo melhor a configuração que se encontra hoje disponível no editor.

Figura 6: Vista atual da Classroom 2 na cena Quiz AI, com quadro interativo, secretarias e botões de resposta.

![Figura 6](/Users/belarmino/Documents/BackToSchool/docs/figures/quiz_ai_classroom2.png)

## 5.8 Conformidade entre requisitos e implementação

Uma vez que se trata de um projeto de fim de curso, não basta descrever funcionalidades isoladas; importa também avaliar o grau em que a implementação observada satisfaz os requisitos inicialmente definidos. A tabela seguinte resume esse cruzamento para os aspetos mais relevantes, distinguindo requisitos já suportados de forma clara, requisitos parcialmente implementados e áreas cuja presença no conceito ainda não corresponde a uma solução plenamente estabilizada no código analisado.

Tabela 10: Matriz resumida de conformidade entre requisitos e implementação observada.

| Referência | Evidência observada | Estado |
| --- | --- | --- |
| R1.1 / R1.2 | A máquina de estados Waiting, Freeze, Question, ObstacleRace e ChangeRound suporta a alternância de fases e a progressão estruturada das perguntas. | Implementado |
| R1.4 | O conceito define equipas de dois, mas o ScoreManager atribui atualmente equipas por índice individual, até um máximo de oito equipas distintas. | Parcial |
| R1.5 / R4.3 | O ScoreManager mantém pontuação sincronizada, atualiza o leaderboard e apresenta um ecrã final de classificação. | Implementado |
| R2.1 | O movimento em primeira pessoa, a rotação, o salto e o suporte de câmara encontram-se distribuídos por MyCharacterController e pelos controladores associados. | Implementado |
| R2.2 | O empurrão encontra-se suportado em FirstPersonNetworkState e nos NPCs ColegaEmpurrar; a componente de agarrar não surge com o mesmo grau de consolidação no código principal. | Parcial |
| R2.3 | A documentação prevê apanhar e atirar objetos, mas a base de código analisada não evidencia uma pipeline tão consolidada como a do empurrão. | Parcial |
| R2.4 | Interactor, AnswerButton, AnswerButtonManager e ChairInteraction suportam interação contextual com objetos e estações. | Implementado |
| R2.5 / R4.4 / R5.3 | MetaVoiceChat, NoiseGate, PlayerVoiceState e Vigia articulam voz por proximidade, deteção de ruído e punição do jogador infrator. | Implementado |
| R3.1 / R3.2 | O anfitrião persiste temas em CSV e perguntas geradas em JSON na área persistente da aplicação. | Implementado |
| R4.6 | HostQuizManager disponibiliza configuração de rondas, duração e temas do quiz ao anfitrião do lobby. | Implementado |
| R5.4 | ColegaVaguear e ColegaEmpurrar implementam navegação autónoma e interferência física em contexto de corrida. | Implementado |
| R6.5 | A infraestrutura de áudio do lobby utiliza PlayerPrefs para preservar níveis de volume entre sessões. | Implementado |

A leitura desta matriz permite retirar uma conclusão importante: o protótipo já cumpre com solidez a espinha dorsal do jogo, nomeadamente a alternância entre fases, a geração de quizzes, a sincronização da partida, a gestão de voz e a presença do Vigia. No entanto, também revela desalinhamentos que devem ser assumidos de forma transparente, como a regra de equipas de dois ainda não refletida integralmente na lógica atual de ScoreManager e a menor maturidade de mecânicas como agarrar ou atirar objetos quando comparadas com o sistema de empurrão. Esta honestidade analítica valoriza o relatório, porque demonstra compreensão técnica do estado real do projeto.

## 5.9 Validação e estratégia de teste

A validação do protótipo, nesta fase, assenta sobretudo em verificação manual, depuração em contexto de execução e análise direta do comportamento dos subsistemas principais. A própria organização do código evidencia esta realidade: vários componentes incluem mensagens de erro, mensagens de aviso e caminhos de fallback destinados a detetar rapidamente referências em falta, ficheiros inexistentes, dependências de cena não configuradas ou incoerências de spawn. Esta abordagem é compatível com um protótipo em evolução, embora ainda não substitua uma estratégia formal e repetível de testes automatizados.

A consulta do console Unity via MCP, efetuada em 1 de junho de 2026, não revelou erros recentes na amostra observada, contendo apenas registos de inicialização do próprio serviço MCP. Ainda assim, essa observação não deve ser interpretada como prova exaustiva de estabilidade, mas apenas como um indicador pontual de que não havia falhas imediatas evidentes no momento da inspeção. Em paralelo, a pesquisa realizada no repositório não revelou suites dedicadas de testes automatizados, o que significa que a confiança no sistema depende sobretudo de ensaios manuais e da robustez dos mecanismos de verificação embutidos nos próprios scripts.

Tabela 11: Síntese das evidências de validação identificadas no projeto.

| Área de validação | Observação | Estado |
| --- | --- | --- |
| Verificação estática do repositório | Leitura dos scripts principais, dependências, cenas e artefactos de configuração para confirmar o alinhamento entre especificação e protótipo. | Concluída |
| Console Unity via MCP | Consulta ao console em 1 de junho de 2026; a amostra devolvida continha apenas registos de arranque do MCP, sem erros recentes nessa observação. | Concluída com reserva |
| Testes automatizados | Não foram encontrados diretórios ou suites de testes automatizados dedicados no repositório analisado. | Em falta |
| Mecanismos defensivos no código | Existem salvaguardas para ausência de .env, CSV, spawn points, NoiseGate, referências de UI e bindings de cena. | Parcialmente consolidado |
| Testes multijogador e equilíbrio | A presença de logs de depuração e de comentários de afinação indica que áreas como empurrão, spawns e integração de cena permanecem em evolução. | Em progresso |

## 5.10 Limitações técnicas e trabalho em aberto

A análise do projeto torna igualmente visíveis algumas limitações que importa registar. Em primeiro lugar, várias funcionalidades dependem de configuração correta de cena e de referências no inspetor, como spawn points, estações, pontos de prisão, UI boards e componentes de voz. A presença de mensagens explícitas para falta dessas referências mostra que o grupo teve consciência do problema e tentou reduzir o impacto em tempo de execução, mas também indica que o sistema ainda está sensível a erros de montagem do cenário. Para um protótipo académico esta situação é compreensível, embora deva ser progressivamente reduzida até à entrega final.

Em segundo lugar, subsistem áreas onde a intenção de design é mais ambiciosa do que a maturidade atual da implementação. A mecânica de equipas deveria organizar jogadores em duplas, mas a lógica de atribuição atual não impõe esse comportamento da forma prevista no documento de requisitos. Do mesmo modo, a documentação conceptual sugere um leque de interações físicas mais vasto do que aquele que se observa claramente consolidado no código principal, onde o empurrão surge como funcionalidade mais desenvolvida. Estas diferenças não anulam o mérito do protótipo, mas exigem priorização cuidadosa das próximas iterações.

Por fim, a ausência de uma bateria de testes automatizados e a necessidade continuada de afinação em contexto multijogador sugerem que o principal desafio das etapas seguintes não será apenas acrescentar funcionalidades, mas estabilizar e validar aquilo que já existe. Nesse sentido, o trabalho em aberto passa por consolidar a lógica de equipas, reforçar testes repetíveis, reduzir dependências frágeis de cena, melhorar a coerência entre requisitos e implementação e refinar a experiência audiovisual e de interação. Este conjunto de ações tende a ter impacto direto tanto na qualidade do jogo como na solidez da apresentação académica do projeto.

## 5.11 Priorização do trabalho até à entrega final

Numa perspetiva de entrega final, não basta reconhecer limitações; importa ordenar o esforço de acordo com o impacto académico e funcional de cada intervenção. A priorização apresentada na tabela seguinte resulta da análise cruzada entre requisitos, estado atual do código e risco técnico observado. O objetivo não é apenas aumentar o número de funcionalidades, mas sobretudo reforçar a coerência entre aquilo que o projeto promete, aquilo que já demonstra e aquilo que será defendido no relatório e na apresentação final.

Tabela 12: Prioridades de evolução identificadas para a fase final do projeto.

| Prioridade | Ação | Impacto esperado |
| --- | --- | --- |
| Alta | Consolidar a lógica de equipas de dois e a pontuação coletiva, de forma a alinhar a implementação com o requisito funcional R1.4. | Elimina um desalinhamento funcional relevante e melhora a justiça competitiva. |
| Alta | Executar ciclos de teste multijogador repetíveis para validar sincronização, transições de ronda, empurrão e penalizações do Vigia. | Aumenta a confiança no protótipo e reduz riscos de regressão em demonstração. |
| Alta | Reduzir dependências frágeis de cena, revendo referências de inspetor, spawn points, prisões, estações e elementos de interface. | Diminui falhas por configuração e reforça a robustez da montagem final. |
| Média | Aprofundar as mecânicas de agarrar e de atirar objetos, caso se pretenda manter integralmente a ambição definida no GDD. | Aproxima o jogo do conceito original e torna a sabotagem mais expressiva. |
| Média | Refinar o polimento audiovisual, a legibilidade do HUD e a coerência estética entre menus, sala de aula e percursos. | Melhora a perceção de acabamento e a qualidade da apresentação académica. |

A principal leitura desta priorização é que a etapa seguinte do projeto deve privilegiar consolidação sobre expansão indiscriminada. Em particular, a correção dos desalinhamentos mais visíveis, a estabilização da experiência multijogador e a redução de fragilidade de cena tendem a produzir ganhos mais relevantes do que a introdução apressada de novas mecânicas. Esta perspetiva é especialmente importante num projeto de fim de curso, uma vez que a avaliação valoriza não apenas ambição conceptual, mas também consistência técnica, capacidade de análise e maturidade de execução.

# 6. Conclusão

Back to School evoluiu de uma ideia centrada em quizzes para um protótipo multijogador mais ambicioso, no qual conhecimento, movimentação, sabotagem e comunicação coexistem no mesmo ciclo de jogo. A introdução formal dos requisitos no início do relatório permitiu clarificar o âmbito funcional do projeto e criar uma ponte mais sólida entre o material conceptual do GDD e o desenvolvimento concreto observado no repositório. Esta clarificação revelou-se importante para enquadrar o trabalho já realizado e para identificar, com maior objetividade, os aspetos que ainda necessitam de consolidação.

Em termos técnicos, o projeto já demonstra uma base consistente: existe uma arquitetura de rede funcional, um mecanismo de geração automática de conteúdo, um ciclo de jogo estruturado por estados e uma integração expressiva da voz na própria jogabilidade. Simultaneamente, a análise crítica do código mostra que a qualidade de um relatório final não depende apenas da enumeração do que já funciona, mas também da capacidade de reconhecer desalinhamentos, fragilidades e prioridades de evolução. O trabalho futuro deverá concentrar-se no reforço da robustez, no refinamento da experiência de utilizador, na revisão de equilíbrio entre mecânicas, na consolidação da lógica de equipas e na formalização de estratégias de teste mais sistemáticas. Ainda assim, o estado atual do Back to School mostra que o conceito definido no início do projeto não só é exequível, como já se encontra materializado num protótipo jogável, tecnicamente coerente e suficientemente promissor para justificar investimento adicional de polimento e validação.

# 7. Referências

[1] Ferreira, Miguel, e Sacate, Belarmino. Back to School. Apresentação intermédia do projeto, Instituto Superior de Engenharia de Lisboa, maio de 2026.

[2] Ferreira, Miguel, e Sacate, Belarmino. GDD - Back To School. Documento interno de conceção do jogo, 2026.

[3] Ferreira, Miguel, e Sacate, Belarmino. Tradução de GDD para requisitos. Documento interno de análise de requisitos, 2026.

[4] Ferreira, Miguel, e Sacate, Belarmino. Projeto Relatório. Versão preliminar do relatório, 2026.

[5] Código-fonte do projeto Back to School. Repositório local Unity analisado em 1 de junho de 2026.

[6] Documentação técnica das bibliotecas Unity, PurrNet, MetaVoiceChat, Steamworks.NET, Cinemachine e Unity AI Navigation consultada ao longo do desenvolvimento.
