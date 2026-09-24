# 🎒 Back To School

**Back To School** é um jogo multijogador em 3D do género “Party Game” para WindowsPC, desenvolvido em Unity3D. Neste jogo, os jogadores assumem o papel de alunos numa escola e competem em questionários sobre diversos temas, os quais são escolhidos pelos jogadores, mas gerados por inteligência artificial. Os jogadores deverão conseguir resolver os questionários sem que o vigia da prova escute possíveis conversas. Neste contexto será implementado um sistema de “Proximity Voice Chat”. Sempre que o tema muda, os jogadores deslocam-se a uma nova sala, enfrentando uma corrida de obstáculos pelos corredores. No final, vence quem tiver maior pontuação total.

![Sala de quiz do jogo](docs/figures/quiz_ai_classroom2.png)

## 📦 Tecnologias

- **Unity 6.0.4f1** e **C#**
- **PurrNet** para sincronização multijogador *peer-to-peer*
- **PurrLobby** e **Steamworks.NET** para criação e descoberta de lobbies
- **MetaVoiceChat** para voz de proximidade
- **Gemini 2.5 Flash** para gerar quizzes temáticos em JSON
- **Cinemachine**, **Unity Input System** e **Unity AI Navigation**

## 🚀 Funcionalidades

- Criação, pesquisa e entrada em lobbies multijogador.
- Quizzes dinâmicos a partir de temas definidos pelo anfitrião.
- Rondas de perguntas em salas de aula, com botões físicos para responder.
- Voz de proximidade e Vigia que deteta jogadores demasiado ruidosos.
- Corridas de obstáculos entre salas, com NPCs e penalizações.
- Movimento em primeira pessoa: correr, saltar, agachar, agarrar, empurrar e atirar objetos.
- Pontuação, temporizador, transições de ronda e classificação final sincronizados.

## 🧭 Processo de desenvolvimento

O projeto começou pela definição do conceito e dos requisitos do jogo, seguida do Game Design Document e dos diagramas de arquitetura. A implementação foi organizada em torno de dois cenários principais: `LobbySample`, para entrada e configuração da partida, e `Quiz AI`, para o ciclo de jogo.

O ciclo de cada partida alterna entre perguntas, transição, corrida de obstáculos e preparação da ronda seguinte. A sincronização P2P mantém esse estado partilhado entre os jogadores; o anfitrião lê os temas, pede as perguntas ao modelo de IA e distribui o quiz gerado pelos restantes pares.

## 📚 O que foi consolidado

- Arquitetura multijogador P2P e chamadas remotas com PurrNet.
- Máquinas de estado para coordenar as fases de uma ronda.
- Integração de uma API de IA e validação de respostas JSON.
- Voz de proximidade como mecânica de jogo, integrada com a deteção do Vigia.
- Navegação de NPCs com NavMesh e interações físicas em primeira pessoa.



## ▶️ Executar o projeto

1. Clone o repositório e obtenha os assets LFS:

   ```bash
   git clone https://github.com/belarminooO/BackToSchool.git
   cd BackToSchool
   git lfs pull
   ```

2. Abra a pasta no **Unity Hub** com o Unity **6.0.4f1**.
3. Para geração dinâmica de quizzes, defina a variável de ambiente `API_KEY` com uma chave válida da API Gemini antes de iniciar o Unity.
4. Abra as cenas `LobbySample` e `Quiz AI` e inicie a partida pelo lobby.

## 🖥️ Builds

Os executáveis distribuídos com o projeto estão em [`builds/`](builds):

- [`BackToSchool_macOS.zip`](builds/BackToSchool_macOS.zip)
- [`BackToSchool_Windows.zip`](builds/BackToSchool_Windows.zip)

## 🎬 Vídeo de demonstração

O vídeo de gameplay está disponível abaixo (ou na [página de releases](https://github.com/belarminooO/BackToSchool/releases/tag/v1.0.0)).

https://github.com/belarminooO/BackToSchool/releases/download/v1.0.0/BackToSchoolVideo.mp4

## 📁 Documentação e anexos

| Conteúdo | Localização |
| --- | --- |
| Planeamento | [`anexos/00_Planeamento`](anexos/00_Planeamento) |
| Análise de requisitos | [`anexos/01_Analise`](anexos/01_Analise) |
| Game Design Document e diagramas | [`anexos/02_Desenho`](anexos/02_Desenho) |
| Scripts entregues | [`anexos/03_Implementacao`](anexos/03_Implementacao) |
| Avaliação de jogabilidade | [`anexos/04_Teste`](anexos/04_Teste) |
| Relatório final | [`anexos/_RELATORIO/Projeto Relatório.pdf`](anexos/_RELATORIO/Projeto%20Relat%C3%B3rio.pdf) |

## 👥 Autoria

Projeto académico da unidade curricular de Projeto — LEIM, ISEL/DEI (2025/2026).

- Belarmino Sacate
- Miguel Ferreira

As bibliotecas, recursos e *asset packs* de terceiros usados no projeto mantêm as respetivas licenças e atribuições presentes no código-fonte.
