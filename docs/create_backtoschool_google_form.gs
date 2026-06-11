function createBackToSchoolGoogleForm() {
  const form = FormApp.create('BackToSchool - Questionário de Avaliação da Jogabilidade');

  const geqColumns = [
    'Nada (0)',
    'Ligeiramente (1)',
    'Moderadamente (2)',
    'Bastante (3)',
    'Extremamente (4)',
  ];

  const agreeColumns = [
    'Discordo totalmente',
    'Discordo',
    'Nem concordo nem discordo',
    'Concordo',
    'Concordo totalmente',
  ];

  form.setDescription(
      'Caro/a participante,\n\n' +
      'Obrigado/a por participar neste estudo de jogabilidade do BackToSchool. ' +
      'Este questionário insere-se no projeto académico desenvolvido no âmbito ' +
      'da unidade curricular de Projeto (PRJ), da Licenciatura em Engenharia ' +
      'Informática e Multimédia do ISEL.\n\n' +
      'O principal objetivo é avaliar a experiência de jogo proporcionada pelo protótipo. ' +
      'A sua participação é voluntária e as respostas serão utilizadas apenas ' +
      'para fins académicos, de forma confidencial.\n\n' +
      'O preenchimento demora cerca de 10 a 15 minutos. Responda com base na ' +
      'sua experiência real durante a sessão de teste.\n\n' +
      'Obrigado pela colaboração.');
  form.setProgressBar(true);
  form.setShuffleQuestions(false);
  form.setLimitOneResponsePerUser(false);
  form.setConfirmationMessage(
      'Obrigado pela sua participação. As suas respostas foram registadas com sucesso.');

  const introConsent = form.addMultipleChoiceItem()
      .setTitle('Concorda em participar neste estudo de forma voluntária?')
      .setRequired(true);

  const instructionsPage = form.addPageBreakItem()
      .setTitle('Teste o jogo')
      .setHelpText(
          'Antes de responder ao questionário, jogue uma sessão do BackToSchool.\n\n' +
          'BackToSchool é um jogo multijogador em 3D do género party game, ' +
          'desenvolvido em Unity para Windows PC. Os participantes assumem o papel ' +
          'de alunos numa escola e competem em questionários sobre diversos temas, ' +
          'escolhidos pelos jogadores e gerados por inteligência artificial.\n\n' +
          'Durante a partida, os jogadores devem responder aos questionários sem ' +
          'que o Vigia escute conversas indevidas. Sempre que o tema muda, os ' +
          'jogadores deslocam-se para uma nova sala, enfrentando uma corrida de ' +
          'obstáculos pelos corredores.\n\n' +
          'Se possível, experimente pelo menos uma ronda com fase de quiz e fase ' +
          'de corrida/intervalo antes de preencher o questionário.');

  const profilePage = form.addPageBreakItem()
      .setTitle('Perfil e contexto')
      .setHelpText('Responda às perguntas seguintes com base no seu perfil e na sessão de teste realizada.');

  form.addTextItem()
      .setTitle('Idade')
      .setRequired(true);

  form.addMultipleChoiceItem()
      .setTitle('Género (opcional)')
      .setChoiceValues([
        'Feminino',
        'Masculino',
        'Não binário',
        'Prefiro não responder',
        'Outro',
      ])
      .setRequired(false);

  form.addMultipleChoiceItem()
      .setTitle('Com que frequência joga videojogos?')
      .setChoiceValues([
        'Todos os dias',
        'Várias vezes por semana',
        'Uma vez por semana',
        'Menos de uma vez por semana',
        'Raramente',
      ])
      .setRequired(true);

  form.addMultipleChoiceItem()
      .setTitle('Qual é a sua experiência com jogos multijogador competitivos/cooperativos?')
      .setChoiceValues([
        'Muito elevada',
        'Elevada',
        'Moderada',
        'Reduzida',
        'Nenhuma',
      ])
      .setRequired(true);

  form.addMultipleChoiceItem()
      .setTitle('Já conhecia o BackToSchool antes desta sessão?')
      .setChoiceValues(['Sim', 'Não'])
      .setRequired(true);

  form.addMultipleChoiceItem()
      .setTitle('Conseguiu experimentar pelo menos uma fase de quiz e uma fase de corrida/intervalo?')
      .setChoiceValues(['Sim', 'Não'])
      .setRequired(true);

  form.addMultipleChoiceItem()
      .setTitle('Usou comunicação por voz durante o teste?')
      .setChoiceValues(['Sim', 'Não'])
      .setRequired(true);

  const specificPage = form.addPageBreakItem()
      .setTitle('Elementos específicos do jogo')
      .setHelpText(
          'Indique o seu grau de concordância com as afirmações seguintes, tendo em conta a sua experiência no BackToSchool.');

  form.addGridItem()
      .setTitle('Avaliação de elementos específicos do BackToSchool')
      .setRows([
        'Compreendi rapidamente o objetivo geral do jogo.',
        'Foi fácil perceber como responder às perguntas durante a fase de quiz.',
        'Os controlos pareceram-me adequados ao tipo de jogo.',
        'O feedback visual e sonoro ajudou-me a perceber o que estava a acontecer.',
        'A alternância entre quiz e corrida tornou a experiência mais interessante.',
        'A presença do Vigia acrescentou tensão de forma positiva.',
        'As interações físicas entre jogadores e objetos enriqueceram a experiência.',
        'O equilíbrio entre conhecimento, caos e corrida pareceu-me adequado.',
        'O jogo conseguiu criar uma experiência diferente de um quiz tradicional.',
      ])
      .setColumns(agreeColumns)
      .setRequired(true);

  const geqCorePage = form.addPageBreakItem()
      .setTitle('GEQ - Experiência durante o jogo')
      .setHelpText(
          'Indique como se sentiu enquanto jogava, usando a escala seguinte: ' +
          'Nada (0), Ligeiramente (1), Moderadamente (2), Bastante (3), Extremamente (4).');

  form.addGridItem()
      .setTitle('GEQ - Bloco 1')
      .setRows([
        'Senti-me satisfeito/a.',
        'Senti-me habilidoso/a.',
        'Fiquei interessado/a na história do jogo.',
        'Achei divertido.',
        'Estive completamente absorvido/a pelo jogo.',
        'Senti-me feliz.',
        'O jogo pôs-me de mau humor.',
        'Pensei noutras coisas.',
        'Achei-o cansativo.',
        'Senti-me competente.',
        'Achei-o difícil.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  form.addGridItem()
      .setTitle('GEQ - Bloco 2')
      .setRows([
        'Achei-o esteticamente agradável.',
        'Esqueci-me de tudo o que me rodeava.',
        'Senti-me bem.',
        'Fui bom/boa a jogá-lo.',
        'Senti-me aborrecido/a.',
        'Senti-me bem-sucedido/a.',
        'Senti-me imaginativo/a.',
        'Senti que podia explorar coisas.',
        'Gostei da experiência.',
        'Fui rápido/a a atingir os objetivos do jogo.',
        'Senti-me incomodado/a.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  form.addGridItem()
      .setTitle('GEQ - Bloco 3')
      .setRows([
        'Senti-me pressionado/a.',
        'Senti-me irritado/a.',
        'Perdi a noção do tempo.',
        'Senti-me desafiado/a.',
        'Achei-o impressionante.',
        'Estive profundamente concentrado/a no jogo.',
        'Senti-me frustrado/a.',
        'Pareceu-me uma experiência rica.',
        'Perdi a ligação com o mundo exterior.',
        'Senti pressão do tempo.',
        'Tive de fazer muito esforço.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  const socialGatePage = form.addPageBreakItem()
      .setTitle('Presença social')
      .setHelpText('Indique se esta sessão foi jogada com outros participantes humanos.');

  const socialGateItem = form.addMultipleChoiceItem()
      .setTitle('Durante esta sessão, jogou com outros participantes humanos?')
      .setRequired(true);

  const socialPage = form.addPageBreakItem()
      .setTitle('GEQ - Presença social')
      .setHelpText(
          'As afirmações seguintes dizem respeito à forma como se sentiu em relação aos outros jogadores durante a sessão.');

  form.addGridItem()
      .setTitle('GEQ - Presença social - Bloco 1')
      .setRows([
        'Empatizei com os outros jogadores.',
        'As minhas ações dependeram das ações dos outros.',
        'As ações dos outros dependeram das minhas.',
        'Senti-me ligado/a aos outros.',
        'Os outros prestaram muita atenção a mim.',
        'Prestei muita atenção aos outros.',
        'Senti ciúmes dos outros.',
        'Gostei de estar com os outros.',
        'Quando eu estava feliz, os outros também estavam felizes.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  form.addGridItem()
      .setTitle('GEQ - Presença social - Bloco 2')
      .setRows([
        'Quando os outros estavam felizes, eu também estava feliz.',
        'Influenciei o estado de espírito dos outros.',
        'Fui influenciado/a pelo estado de espírito dos outros.',
        'Admirei os outros.',
        'O que os outros fizeram afetou o que eu fiz.',
        'O que eu fiz afetou o que os outros fizeram.',
        'Senti vontade de me vingar.',
        'Senti satisfação com o infortúnio dos outros.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  const postPage = form.addPageBreakItem()
      .setTitle('GEQ - Como se sentiu depois de jogar')
      .setHelpText(
          'Indique como se sentiu após terminar a sessão de jogo, usando a mesma escala do GEQ.');

  form.addGridItem()
      .setTitle('GEQ - Pós-jogo - Bloco 1')
      .setRows([
        'Senti-me revitalizado/a.',
        'Senti-me mal.',
        'Tive dificuldade em voltar à realidade.',
        'Senti-me culpado/a.',
        'Pareceu uma vitória.',
        'Pareceu-me uma perda de tempo.',
        'Senti-me cheio/a de energia.',
        'Senti-me satisfeito/a.',
        'Senti-me desorientado/a.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  form.addGridItem()
      .setTitle('GEQ - Pós-jogo - Bloco 2')
      .setRows([
        'Senti-me exausto/a.',
        'Senti que poderia ter feito coisas mais úteis.',
        'Senti-me poderoso/a.',
        'Senti-me cansado/a.',
        'Senti arrependimento.',
        'Senti vergonha.',
        'Senti orgulho.',
        'Tive a sensação de ter regressado de uma viagem.',
      ])
      .setColumns(geqColumns)
      .setRequired(true);

  const commentsPage = form.addPageBreakItem()
      .setTitle('Comentários finais')
      .setHelpText('Partilhe livremente a sua opinião sobre a experiência.');

  form.addParagraphTextItem()
      .setTitle('Que elemento do BackToSchool achou mais divertido ou mais conseguido?')
      .setRequired(true);

  form.addParagraphTextItem()
      .setTitle('Que parte do jogo lhe causou mais frustração, confusão ou dificuldade?')
      .setRequired(true);

  form.addParagraphTextItem()
      .setTitle('Se pudesse mudar apenas uma coisa no jogo, o que mudaria?')
      .setRequired(true);

  form.addParagraphTextItem()
      .setTitle('Comentários adicionais (opcional)')
      .setRequired(false);

  introConsent.setChoices([
    introConsent.createChoice('Sim, concordo', instructionsPage),
    introConsent.createChoice('Não, não concordo', FormApp.PageNavigationType.SUBMIT),
  ]);

  socialGateItem.setChoices([
    socialGateItem.createChoice('Sim', socialPage),
    socialGateItem.createChoice('Não', postPage),
  ]);

  const result = {
    editUrl: form.getEditUrl(),
    publishedUrl: form.getPublishedUrl(),
    id: form.getId(),
  };

  console.log(JSON.stringify(result, null, 2));
  return result;
}
